﻿namespace DriverTool

module LenovoUpdates =    
    open System
    open DriverTool.Library.PackageXml
    open DriverTool.Library.Configuration
    open System.Text.RegularExpressions
    open System.Linq
    open System.Xml.Linq    
    open DriverTool.Library.Web    
    open DriverTool.Library.UpdatesContext
    open DriverTool.Library.F0
    open DriverTool.Library.Logging
    open DriverTool.Library.F
    open DriverTool.Library
    
    let logger = DriverTool.Library.Logging.getLoggerByName "LenovoUpdates"
               
    let operatingSystemCode2DownloadableCode (operatingSystemCode: OperatingSystemCode) =
        operatingSystemCode.Value.Replace("X86","").Replace("x86","").Replace("X64","").Replace("x64","")
    
    let modelCode2DownloadableCode (modelCode: ModelCode) =
        modelCode.Value.Substring(0,4)
     
    let getModelInfoUri (modelCode: ModelCode) (operatingSystemCode: OperatingSystemCode) = 
        new Uri(sprintf "https://download.lenovo.com/catalog/%s_%s.xml" (modelCode2DownloadableCode modelCode) (operatingSystemCode2DownloadableCode operatingSystemCode))
    
    let toPackages (packagesXElement:XElement) =
        result
            {
                let! packages = 
                    packagesXElement.Elements(XName.Get("package"))
                    |>Seq.map(fun p -> 
                            result{
                                let! location = XmlHelper.getElementValue p "location"
                                let! category = XmlHelper.getElementValue p "category"
                                let! checksumValue = XmlHelper.getElementValue p "checksum"
                                let! checksumType = XmlHelper.getRequiredAttribute (p.Element(XName.Get("checksum"))) "type"
                                return {
                                    Location = location
                                    Category = category
                                    CheckSum = checksumValue
                                }                            
                            }                            
                        )
                    |>toAccumulatedResult
                return packages                
            }

    let loadPackagesXml (xmlPath:FileSystem.Path) = 
        result{
            let! existingXmlPath = FileOperations.ensureFileExistsWithMessage (sprintf "Packages xml file '%A' not found." xmlPath) xmlPath
            let! xDocument = XmlHelper.loadXDocument existingXmlPath
            let! packages = toPackages xDocument.Root
            return packages
        }

    let getXmlFileNameFromUri (uri: Uri) : Result<string,Exception>= 
        try
            let regExMatch = 
                Regex.Match(uri.OriginalString, @".+/(.+\.xml)$")
            match regExMatch.Success with
            | true -> Result.Ok (regExMatch.Groups.[1].Value)                                        
            | false -> Result.Error (new ArgumentException(sprintf "Uri '%s' does not represent a xml file." uri.OriginalString):> Exception)
        with
        | ex -> Result.Error ex

    let getTempXmlFilePathFromUri cacheFolderPath uri : Result<FileSystem.Path,Exception> = 
        let xmlFileName = getXmlFileNameFromUri uri
        match xmlFileName with
        |Result.Ok f -> 
            let tempXmlFilePathString = 
                getDownloadCacheFilePath (FileSystem.pathValue cacheFolderPath) f
            FileSystem.path tempXmlFilePathString
        |Result.Error ex -> Result.Error ex


    let getBaseUrl locationUrl =
        let uri = new Uri(locationUrl)
        uri.AbsoluteUri.Remove(uri.AbsoluteUri.Length - uri.Segments.Last().Length).Trim('/');
    
    let packageXmlInfo2downloadedPackageXmlInfo (packageXmlInfo:PackageXmlInfo, filePath) =
        {
            Location = packageXmlInfo.Location;
            Category = packageXmlInfo.Category;
            FilePath = filePath;
            BaseUrl = getBaseUrl packageXmlInfo.Location;
            CheckSum = packageXmlInfo.CheckSum;
        }
    
    let downloadPackageInfo cacheFolderPath (packageXmlInfo:PackageXmlInfo) = 
            result {
                let sourceUri = new Uri(packageXmlInfo.Location)
                let! destinationFilePath = getTempXmlFilePathFromUri cacheFolderPath sourceUri
                let downloadInfo = {SourceUri=sourceUri;SourceChecksum=packageXmlInfo.CheckSum;SourceFileSize=0L;DestinationFile=destinationFilePath;}                
                let! downloadInfo2 = downloadIfDifferent (logger, downloadInfo, (ignoreVerificationErrors downloadInfo))
                let dpi = packageXmlInfo2downloadedPackageXmlInfo (packageXmlInfo, downloadInfo2.DestinationFile)
                return dpi
            }

    let downloadExternalFiles cacheFolderPath (packageInfo:PackageInfo) =
        let e:DownloadInfo array = [||]        
        match packageInfo.ExternalFiles with
        |None -> Result.Ok e
        |Some files -> 
            result {
                let! res = 
                    files                    
                    |> Array.map(fun f ->                 
                        match f.Url with
                        |None -> Result.Error (toException (sprintf "Source url for external file '%s' has not been defined." f.Name) None)
                        |Some sourceUri->                                
                            result{
                                let fileName = WebDownload.getFileNameFromUri sourceUri
                                let! destinationFilePath = FileSystem.path (getDownloadCacheFilePath (FileSystem.pathValue cacheFolderPath) fileName)
                                let downloadInfo = {SourceUri=sourceUri;SourceChecksum=f.Checksum;SourceFileSize=f.Size;DestinationFile=destinationFilePath;}
                                let! downloadInfo2 = downloadIfDifferent (logger, downloadInfo, (ignoreVerificationErrors downloadInfo))
                                return downloadInfo2
                            }
                        )                    
                    |>toAccumulatedResult
                return (res |> Seq.toArray)
            }
    
    let downloadPackageXmls cacheFolderPath packageXmlInfos : Result<seq<DownloadedPackageXmlInfo>,Exception> = 
        let downloadedPackageXmlInfos = 
            packageXmlInfos
            |> Seq.map (fun pi -> downloadPackageInfo cacheFolderPath pi)

        let objectResults = 
                    downloadedPackageXmlInfos
                    //|> Seq.cast<Result<System.Object,Exception>>

        let allErrorMessages = 
            getAllErrorMessages objectResults

        match allErrorMessages.Count() with
        | 0 ->  
                let allSuccesses = 
                    (getAllSuccesses objectResults)
                    |> Seq.cast<DownloadedPackageXmlInfo>                 
                Result.Ok allSuccesses
        | _ -> 
            let msg = sprintf "Failed to download all package infos due to the following %i error messages:%s%s" (allErrorMessages.Count()) Environment.NewLine (String.Join(Environment.NewLine,allErrorMessages))
            Result.Error (new Exception(msg))
     
    let getModelInfoXmlFilePath cacheFolderPath (modelCode: ModelCode) (operatingSystemCode: OperatingSystemCode) = 
        let fileName = sprintf "%s_%s.xml" modelCode.Value operatingSystemCode.Value
        let filePathString = DriverTool.Library.PathOperations.combinePaths2 cacheFolderPath fileName
        filePathString

    let getPackageInfo (downloadedPackageInfo : DownloadedPackageXmlInfo) =
        try
            Result.Ok (getPackageInfoUnsafe downloadedPackageInfo)
        with
        |ex -> Result.Error (new Exception(sprintf "Failed to get update info from '%s'." (FileSystem.pathValue downloadedPackageInfo.FilePath),ex))

    let parsePackageXmls (downloadedPackageXmls : seq<DownloadedPackageXmlInfo>) : seq<Result<PackageInfo,Exception>> = 
        downloadedPackageXmls
        |> Seq.map (fun pi -> (getPackageInfo pi))        

    let filterUpdates context packageInfo =
        (not (RegExp.matchAny context.ExcludeUpdateRegexPatterns packageInfo.Category)) 
        &&                             
        (not (RegExp.matchAny context.ExcludeUpdateRegexPatterns packageInfo.Title))

    let getRemoteUpdatesBase (logger, cacheFolderPath, (context:UpdatesRetrievalContext)) =
        result{
            let modelInfoUri = getModelInfoUri context.Model context.OperatingSystem
            let! modelInfoXmlFilePath = getModelInfoXmlFilePath cacheFolderPath context.Model context.OperatingSystem            
            let downloadInfo = DriverTool.Library.Web.toDownloadInfo modelInfoUri String.Empty 0L modelInfoXmlFilePath                
            let! downloadedInfo = DriverTool.Library.Web.downloadIfDifferent (logger, downloadInfo,true)
            let! packageXmlInfos = loadPackagesXml downloadedInfo.DestinationFile
            let! downloadedPackageXmls = downloadPackageXmls cacheFolderPath packageXmlInfos
            let! packageInfos = 
                (parsePackageXmls downloadedPackageXmls)
                |>toAccumulatedResult
            let! downloadResult = 
                    packageInfos
                    |>Seq.toArray
                    |> Array.map (downloadExternalFiles cacheFolderPath)                    
                    |> toAccumulatedResult
            
            return 
                packageInfos 
                |>Seq.filter (filterUpdates context)
                |>Seq.toArray
        }

    ///Get all updates for the current model from Lenovo remote web site
    let getRemoteUpdates logger cacheFolderPath (context:UpdatesRetrievalContext) =
        DriverTool.Library.Logging.genericLoggerResult LogLevel.Debug getRemoteUpdatesBase (logger, cacheFolderPath, context)
    
    let assertThatModelCodeIsValid (model:ModelCode) (actualModel:ModelCode) =
        if(actualModel.Value.StartsWith(model.Value)) then
            Result.Ok true
        else
            Result.Error (new Exception(sprintf "Given model '%s' and actual model '%s' is not equal." model.Value actualModel.Value))
    
    let asserThatOperatingSystemCodeIsValid (operatingSystemCode:OperatingSystemCode) (actualOperatingSystemCode:OperatingSystemCode) =
        if(operatingSystemCode.Value = actualOperatingSystemCode.Value) then
            Result.Ok true
        else
            Result.Error (new Exception(sprintf "Given operating system code '%s' and actual operating system code '%s' are not equal." operatingSystemCode.Value actualOperatingSystemCode.Value))

    let updateIsInstalled logger cacheFolderPath systemInformation lsuPackages (packageInfo:PackageInfo) =
        match(result{
            let workingFolder = FileSystem.pathValue cacheFolderPath
            let! lsuPackageFilePath = DriverTool.Library.PathOperations.combinePaths2 cacheFolderPath packageInfo.PackageXmlName
            let! lsuPackage = LsupEval.Lsup.loadLsuPackageFromFile (FileSystem.pathValue lsuPackageFilePath)
            let isInstalled =
                let isDependent = 
                    match lsuPackage.Dependencies with                
                    |Some d ->                    
                        let detectionRule = LsupEval.Lsup.lsupXmlToApplicabilityRules logger d
                        let isMatch = LsupEval.Rules.evaluateApplicabilityRule logger systemInformation workingFolder (Some lsuPackages) detectionRule 
                        logger.Info(sprintf "Evaluating dependencies: '%s' (%s) '%s'. Return: %b" packageInfo.Title packageInfo.Version packageInfo.PackageXmlName isMatch)
                        isMatch
                    |None -> false

                let isDetectedInstalled =
                    match lsuPackage.DetectInstall with                
                    |Some d ->                    
                        let detectionRule = LsupEval.Lsup.lsupXmlToApplicabilityRules logger d
                        let isMatch = LsupEval.Rules.evaluateApplicabilityRule logger systemInformation workingFolder (Some lsuPackages) detectionRule 
                        logger.Info(sprintf "Evaluating detect install: '%s' (%s) '%s'. Return: %b" packageInfo.Title packageInfo.Version packageInfo.PackageXmlName isMatch)
                        isMatch
                    |None -> false
                (isDependent && isDetectedInstalled)
                //isDetectedInstalled
            return isInstalled
        }) with
        |Result.Ok b -> b
        |Result.Error ex -> 
            logger.Info(sprintf "Failed to evaluate if '%s' is installed. Return: false" packageInfo.PackageXmlName)
            false

    let getLsuPackages cacheFolderPath (packages:PackageInfo[]) =
        packages
        |>Array.map(fun p ->
                result{
                    let! lsuPackageFilePath = DriverTool.Library.PathOperations.combinePaths2 cacheFolderPath p.PackageXmlName
                    let! lsuPackage = LsupEval.Lsup.loadLsuPackageFromFile (FileSystem.pathValue lsuPackageFilePath)
                    return lsuPackage
                }
            )
        |> toAccumulatedResult

    /// Get locally installed updates.
    let getLocalUpdates (logger:Common.Logging.ILog) cacheFolderPath (context:UpdatesRetrievalContext) =
        result{
            
            let! actualModelCode = ModelCode.create String.Empty true
            let! modelCodeIsValid = assertThatModelCodeIsValid context.Model actualModelCode
            logger.Info(sprintf "Model code '%s' is valid: %b" context.Model.Value modelCodeIsValid)
            let! actualOperatingSystemCode = OperatingSystemCode.create String.Empty true
            let! operatingSystemCodeIsValid = asserThatOperatingSystemCodeIsValid context.OperatingSystem actualOperatingSystemCode
            logger.Info(sprintf "Operating system code '%s' is valid: %b" context.OperatingSystem.Value operatingSystemCodeIsValid)
            let! systemInformation =  LsupEval.Rules.getCurrentSystemInformation'()

            let! remotePackageInfos = getRemoteUpdates logger cacheFolderPath context
            let! lsuPackages = getLsuPackages cacheFolderPath remotePackageInfos
            let lsuPackageArray = lsuPackages|>Seq.toArray
            let localUpdates = 
                remotePackageInfos
                |> Seq.distinct
                |> Seq.filter( fun p -> updateIsInstalled logger cacheFolderPath systemInformation lsuPackageArray p)
                |>Seq.filter (filterUpdates context)
                |>Seq.toArray
            logger.Info(sprintf "Local updates: %A" localUpdates)
            return localUpdates
        }

    let getSccmDriverPackageInfo (modelCode:ModelCode, operatingSystemCode:OperatingSystemCode, cacheFolderPath)  : Result<SccmPackageInfo,Exception> =
        result
            {
                let! products = DriverTool.LenovoCatalog.getSccmPackageInfosv2 cacheFolderPath
                let modeCode4 = (modelCode.Value.Substring(0,4))
                let os = (DriverTool.LenovoCatalog.osShortNameToLenovoOs operatingSystemCode.Value)
                let osBuild = OperatingSystem.getOsBuildForCurrentSystem
                let product = DriverTool.LenovoCatalog.findSccmPackageInfoByModelCode4AndOsAndBuild logger modeCode4 os osBuild products
                logger.Info(sprintf "Sccm product info: %A " product)                
                let! sccmPackage = 
                        match product with
                        |None -> Result.Error (new Exception("No sccm package found."))
                        |Some p ->
                            match p.OsBuild with
                            |None -> Result.Error (new Exception(sprintf "No os build for sccm package '%A' found." p))
                            |Some osBuild ->
                                let readmeUrl =  p.SccmDriverPackUrl.Value.Replace(".exe",".txt")
                                let installerUrl = p.SccmDriverPackUrl.Value
                                Result.Ok 
                                    {
                                        ReadmeFile =                                         
                                            {
                                            Url = readmeUrl;
                                            Checksum = "";
                                            FileName = getFileNameFromUrl readmeUrl;
                                            Size=0L;
                                            }
                                        InstallerFile=
                                            {
                                                Url=installerUrl;
                                                Checksum=""
                                                FileName=getFileNameFromUrl installerUrl
                                                Size=0L
                                            }
                                        Released=(DriverTool.LenovoCatalog.getReleaseDateFromUrl installerUrl);
                                        Os= (DriverTool.LenovoCatalog.osShortNameToLenovoOs p.Os);
                                        OsBuild=osBuild
                                    }
                return sccmPackage
            }
    
    let downloadSccmPackage (cacheDirectory, sccmPackage:SccmPackageInfo) =
        result{
            let! installerdestinationFilePath = PathOperations.combinePaths2 cacheDirectory sccmPackage.InstallerFile.FileName
            let! installerUri = toUri sccmPackage.InstallerFile.Url
            let installerDownloadInfo = { SourceUri = installerUri;SourceChecksum = sccmPackage.InstallerFile.Checksum;SourceFileSize = 0L;DestinationFile = installerdestinationFilePath}
            let! installerInfo = Web.downloadIfDifferent (logger,installerDownloadInfo,false)
            let installerPath = FileSystem.pathValue installerInfo.DestinationFile

            let! readmeDestinationFilePath = PathOperations.combinePaths2 cacheDirectory sccmPackage.ReadmeFile.FileName
            let! readmeUri = toUri sccmPackage.ReadmeFile.Url
            let readmeDownloadInfo = { SourceUri = readmeUri;SourceChecksum = sccmPackage.ReadmeFile.Checksum;SourceFileSize = 0L;DestinationFile = readmeDestinationFilePath}
            let! readmeInfo = Web.downloadIfDifferent (logger,readmeDownloadInfo,false)
            let readmePath = FileSystem.pathValue readmeInfo.DestinationFile

            return {
                InstallerPath = installerPath
                ReadmePath = readmePath
                SccmPackage = sccmPackage;
            }            
        }         
    
    let extractSccmPackage (downloadedSccmPackage:DownloadedSccmPackageInfo, destinationPath:FileSystem.Path) =
        result{
            logger.Info("Extract SccmPackage installer...")        
            let arguments = sprintf "/VERYSILENT /DIR=\"%s\"" (FileSystem.pathValue destinationPath)
            let! nonExistingDestinationPath = DirectoryOperations.ensureDirectoryNotExistsWithMessage "Sccm package destination path should exist before extracting the downloaded sccm package." destinationPath
            let! installerExtractedFolder =
                match (FileSystem.existingFilePathString downloadedSccmPackage.InstallerPath) with
                |Ok fp -> 
                    match DriverTool.Library.ProcessOperations.startConsoleProcess (fp, arguments, null, -1, null, null, false) with
                    |Ok _ -> Result.Ok destinationPath
                    |Result.Error ex -> Result.Error (new Exception("Failed to extract Sccm package. " + ex.Message, ex))
                |Result.Error ex -> Result.Error (new Exception("Sccm package installer not found. " + ex.Message, ex))
            let! copiedReadmeFilePath = 
                FileOperations.copyFileIfExists downloadedSccmPackage.ReadmePath destinationPath
            return (installerExtractedFolder,copiedReadmeFilePath)
        }
        
    let toReleaseId downloadedPackageInfo =
        sprintf "%s-%s" (downloadedPackageInfo.Package.Installer.Name.Replace(".exe","")) downloadedPackageInfo.Package.ReleaseDate
    
    let extractUpdate (rootDirectory:FileSystem.Path, (prefix,downloadedPackageInfo:DownloadedPackageInfo)) =
        result{
            let packageFolderName = getPackageFolderName downloadedPackageInfo.Package.Category (toReleaseId downloadedPackageInfo)
            let! packageFolderPath = DriverTool.Library.PathOperations.combine2Paths (FileSystem.pathValue rootDirectory, prefix + "_" + packageFolderName)
            let! existingPackageFolderPath = DirectoryOperations.ensureDirectoryExistsAndIsEmpty (packageFolderPath, true)
            let extractReadmeResult = extractReadme (downloadedPackageInfo, existingPackageFolderPath)
            let extractPackageXmlResult = extractPackageXml (downloadedPackageInfo, existingPackageFolderPath)
            let extractInstallerResult = extractInstaller (downloadedPackageInfo, existingPackageFolderPath)
            let result = 
                [|extractReadmeResult;extractPackageXmlResult;extractInstallerResult|]
                |> toAccumulatedResult
            let res = 
                match result with 
                | Ok r -> extractInstallerResult
                | Result.Error ex -> Result.Error ex
            return! res
        }

    let updateDownloadedPackageInfo downloadedUpdates =
        result
            {
                return (downloadedUpdates |> Array.sortBy (fun dp -> packageInfoSortKey dp.Package))
            }