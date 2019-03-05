﻿namespace DriverTool

module LenovoUpdates =    
    open System
    open FSharp.Data
    open DriverTool.PackageXml
    open DriverTool.Configuration
    open System.Text.RegularExpressions
    open System.Linq
    open F
    open DriverTool.Web
    open DriverTool.Checksum
    open DriverTool.FileOperations
    open DriverTool.PackageXml    
    //open DriverTool.LenovoCatalog

    let loggerl = Logging.getLoggerByName("LenovoUpdates")
               
    let operatingSystemCode2DownloadableCode (operatingSystemCode: OperatingSystemCode) =
        operatingSystemCode.Value.Replace("X86","").Replace("x86","").Replace("X64","").Replace("x64","")
    
    let modelCode2DownloadableCode (modelCode: ModelCode) =
        modelCode.Value.Substring(0,4)
     
    let getModelInfoUri (modelCode: ModelCode) (operatingSystemCode: OperatingSystemCode) = 
        new Uri(sprintf "https://download.lenovo.com/catalog/%s_%s.xml" (modelCode2DownloadableCode modelCode) (operatingSystemCode2DownloadableCode operatingSystemCode))

    type PackagesXmlProvider = XmlProvider<"https://download.lenovo.com/catalog/20FA_Win7.xml">
    type PackageXmlProvider = XmlProvider<"https://download.lenovo.com/pccbbs/mobiles/n1cx802w_2_.xml">

    let getPackagesInfo (modelInfoXmlFilePath:FileSystem.Path) : Result<seq<PackageXmlInfo>,Exception>= 
        try
            let x = PackagesXmlProvider.Load(FileSystem.pathValue modelInfoXmlFilePath)
            x.Packages
            |> Seq.map (fun p -> { Location = p.Location; Category = p.Category; CheckSum = p.Checksum.Value})
            |> Result.Ok            
        with
        |ex -> Result.Error ex

    let getPackagesInfoR (modelInfoXmlFilePath:Result<FileSystem.Path,Exception>) : Result<seq<PackageXmlInfo>,Exception>= 
        try
            match modelInfoXmlFilePath with
            |Ok p -> 
                let x = PackagesXmlProvider.Load(FileSystem.pathValue p)
                x.Packages
                |> Seq.map (fun p -> { Location = p.Location; Category = p.Category; CheckSum = p.Checksum.Value})
                |> Result.Ok
            |Error ex -> Result.Error ex
        with
        |ex -> Result.Error ex
    
    let getXmlFileNameFromUri (uri: Uri) : Result<string,Exception>= 
        try
            let regExMatch = 
                Regex.Match(uri.OriginalString, @".+/(.+\.xml)$")
            match regExMatch.Success with
            | true -> Result.Ok (regExMatch.Groups.[1].Value)                                        
            | false -> Result.Error (new ArgumentException(sprintf "Uri '%s' does not represent a xml file." uri.OriginalString):> Exception)
        with
        | ex -> Result.Error ex

    let getTempXmlFilePathFromUri uri : Result<FileSystem.Path,Exception> = 
        let xmlFileName = getXmlFileNameFromUri uri
        match xmlFileName with
        |Result.Ok f -> 
            let tempXmlFilePathString = 
                getDownloadCacheFilePath f
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
    

    let verifyDownload (sourceUri:Uri, destinationFile, checksum, fileSize, verificationWarningOnly) =
        match (hasSameFileHash (destinationFile, checksum, fileSize)) with
        |true  -> Result.Ok destinationFile
        |false -> 
            let msg = sprintf "Destination file ('%s') hash does not match source file ('%s') hash." (FileSystem.pathValue destinationFile) sourceUri.OriginalString
            match verificationWarningOnly with
            |true ->
                Logging.getLoggerByName("verifyDownload").Warn(msg)
                Result.Ok destinationFile
            |false->Result.Error (new Exception(msg))

    let downloadPackageInfo (packageXmlInfo:PackageXmlInfo) = 
            result {
                let sourceUri = new Uri(packageXmlInfo.Location)
                let! destinationFilePath = getTempXmlFilePathFromUri sourceUri
                let downloadInfo = {SourceUri=sourceUri;SourceChecksum=packageXmlInfo.CheckSum;SourceFileSize=0L;DestinationFile=destinationFilePath;}                
                let! downloadInfo2 = downloadIfDifferent (downloadInfo, (ignoreVerificationErrors downloadInfo))
                let dpi = packageXmlInfo2downloadedPackageXmlInfo (packageXmlInfo, downloadInfo2.DestinationFile)
                return dpi
            }
    
    let getAllErrorMessages (results:seq<Result<'T,Exception>>) =         
        results
        |> Seq.filter (fun dpi -> 
                            match dpi with
                            |Error _ -> true
                            | _ -> false)
        |> Seq.map (fun dpi -> 
                        match dpi with
                        |Error ex -> getAccumulatedExceptionMessages ex
                        | _ -> String.Empty)

    let getAllSuccesses (results:seq<Result<'T,Exception>>) =
        results
        |> Seq.filter (fun dpi -> 
                                match dpi with
                                |Ok _ -> true
                                | _ -> false
                           )
            |> Seq.map (fun dpi -> 
                            match dpi with
                            |Ok pi -> pi
                            | _ -> failwith "Failed to get all successes due to a bug in the success filter.")

    let downloadPackageXmls packageXmlInfos : Result<seq<DownloadedPackageXmlInfo>,Exception> = 
        let downloadedPackageXmlInfos = 
            packageXmlInfos
            |> Seq.map (fun pi -> downloadPackageInfo pi)

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
     
    let downloadPackageXmlsR (packageXmlInfos: Result<seq<PackageXmlInfo>,Exception>) = 
        match packageXmlInfos with
        |Ok pis -> downloadPackageXmls pis
        |Error ex -> Result.Error ex     

    let getModelInfoXmlFilePath (modelCode: ModelCode) (operatingSystemCode: OperatingSystemCode) = 
        let fileName = sprintf "%s_%s.xml" modelCode.Value operatingSystemCode.Value
        let filePathString = getDownloadCacheFilePath fileName        
        FileSystem.path filePathString

    let getPackageInfo (downloadedPackageInfo : DownloadedPackageXmlInfo) =
        try
            Result.Ok (getPackageInfoUnsafe downloadedPackageInfo)
        with
        |ex -> Result.Error (new Exception(sprintf "Failed to get update info from '%s'." (FileSystem.pathValue downloadedPackageInfo.FilePath),ex))

    let parsePackageXmls (downloadedPackageXmls : seq<DownloadedPackageXmlInfo>) : seq<Result<PackageInfo,Exception>> = 
        downloadedPackageXmls
        |> Seq.map (fun pi -> (getPackageInfo pi))        

    let parsePackageXmlsR (downloadedPackageXmls: Result<seq<DownloadedPackageXmlInfo>,Exception>) : Result<seq<PackageInfo>,Exception> =
        let parsedUpdates = 
            match downloadedPackageXmls with
            |Ok pis -> (parsePackageXmls pis)
            |Error ex -> seq{yield Result.Error ex}
        
        let objectResults = 
                    parsedUpdates
                    //|> Seq.cast<Result<System.Object,Exception>>

        let allErrorMessages = getAllErrorMessages objectResults

        match allErrorMessages.Count() with
        | 0 ->  
                let allSuccesses = 
                    (getAllSuccesses objectResults)
                    |> Seq.cast<PackageInfo>                 
                Result.Ok allSuccesses
        | _ -> 
            let msg = sprintf "Failed to parse all package infos due to the following %i error messages:%s%s" (allErrorMessages.Count()) Environment.NewLine (String.Join(Environment.NewLine,allErrorMessages))
            Result.Error (new Exception(msg))
    
    let getRemoteUpdatesBase (modelCode: ModelCode, operatingSystemCode: OperatingSystemCode, overwrite,logDirectory:string) =
        result{
            let modelInfoUri = getModelInfoUri modelCode operatingSystemCode
            let! path = getModelInfoXmlFilePath modelCode operatingSystemCode
            let! modelInfoXmlFilePath = ensureFileDoesNotExist overwrite path
            let! downloadedFile = downloadFile (modelInfoUri, overwrite, modelInfoXmlFilePath)            
            let! packageXmlInfos = getPackagesInfo downloadedFile
            let! downloadedPackageXmls = downloadPackageXmls packageXmlInfos
            let! packageInfos = 
                (parsePackageXmls downloadedPackageXmls)
                |>toAccumulatedResult
            return packageInfos |> Seq.toArray
        }

    let getRemoteUpdates (modelCode: ModelCode, operatingSystemCode: OperatingSystemCode, overwrite,logDirectory:string) =
        Logging.genericLoggerResult Logging.LogLevel.Debug getRemoteUpdatesBase (modelCode, operatingSystemCode, overwrite, logDirectory)
    
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

    let updateFromRemote (remotePackageInfos:seq<PackageInfo>) (packageInfos:seq<PackageInfo>) =
        let updatedPacageInfos = 
            packageInfos
            //Filter local updates that do not have corresponding remote update
            |>Seq.filter(fun p -> 
                            let remotePackageInfo = 
                                remotePackageInfos
                                |> Seq.tryFind(fun rp -> rp.InstallerName = p.InstallerName)
                            match remotePackageInfo with
                            |Some _ -> true
                            |None -> 
                                loggerl.Warn(sprintf "Remote update not found for local update: %A" p)
                                false
                        )
            //For those local updates that have a corresponding remote update, transfer the BaseUrl and Category information from the remote update to the local update.
            |>Seq.map(fun p -> 
                        let remotePackageInfo = 
                            remotePackageInfos
                            |> Seq.tryFind(fun rp -> rp.InstallerName = p.InstallerName)
                        let updatedPackageInfo =
                            match remotePackageInfo with
                            |Some rp ->                         
                                {p with Category=rp.Category;BaseUrl=rp.BaseUrl} 
                            |None -> p
                        updatedPackageInfo
                        )
        updatedPacageInfos

    let getLocalUpdates (modelCode: ModelCode, operatingSystemCode: OperatingSystemCode, overwrite,logDirectory:string) =
        result{
            loggerl.Info("Checking if Lenovo System Update is installed...")
            let! lenovoSystemUpdateIsInstalled = DriverTool.LenovoSystemUpdateCheck.ensureLenovoSystemUpdateIsInstalled ()
            loggerl.Info(sprintf "Lenovo System Update is installed: %b" lenovoSystemUpdateIsInstalled)
            loggerl.Info("Getting locally installed updates...")
            let! packageInfos = DriverTool.LenovoSystemUpdate.getLocalUpdates()
            
            let! actualModelCode = ModelCode.create String.Empty true
            let! modelCodeIsValid = assertThatModelCodeIsValid modelCode actualModelCode
            loggerl.Info(sprintf "Model code '%s' is valid: %b" modelCode.Value modelCodeIsValid)
            let! actualOperatingSystemCode = OperatingSystemCode.create String.Empty true
            let! operatingSystemCodeIsValid = asserThatOperatingSystemCodeIsValid operatingSystemCode actualOperatingSystemCode
            loggerl.Info(sprintf "Operating system code '%s' is valid: %b" operatingSystemCode.Value operatingSystemCodeIsValid)

            let! remotePackageInfos = getRemoteUpdates (modelCode, operatingSystemCode, overwrite,logDirectory)
            let localUpdates = 
                packageInfos
                |> Seq.distinct
                |> updateFromRemote remotePackageInfos
                |>Seq.toArray
            loggerl.Info(sprintf "Local updates: %A" localUpdates)
            return localUpdates
        }
   
    let getLenovoSccmPackageDownloadInfo (uri:string) os osbuild =
        let content = DriverTool.WebParsing.getContentFromWebPage uri
        match content with
        |Ok downloadPageContent -> 
            let downloadLinks =             
                downloadPageContent
                |> DriverTool.LenovoCatalog.getDownloadLinksFromWebPageContent                
                |> Seq.sortBy (fun dl -> dl.Os, dl.OsBuild)
                |> Seq.toArray
                |> Array.rev
            let lenovoOs = (DriverTool.LenovoCatalog.osShortNameToLenovoOs os)
            let sccmPackages =
                downloadLinks
                |> Seq.filter (fun s -> (s.Os = lenovoOs && osbuild = osbuild))
                |> Seq.toArray
            match (sccmPackages.Length > 0) with
            |true -> Result.Ok sccmPackages.[0]
            |false -> 
                match osbuild with
                |"*" ->
                    let sccmPackages =
                        downloadLinks
                        |> Seq.filter (fun s -> (s.Os = lenovoOs))
                        |> Seq.toArray
                    match (sccmPackages.Length > 0) with
                    |true -> Result.Ok sccmPackages.[0]
                    | false ->
                        Result.Error (new Exception(sprintf "Sccm package not found for url '%s', OS=%s, OsBuild=%s." uri os osbuild))
                |_ ->
                    Result.Error (new Exception(sprintf "Sccm package not found for url '%s', OS=%s, OsBuild=%s." uri os osbuild))
        |Error ex -> Result.Error ex

    let findSccmPackageInfoByNameAndOsAndBuild name os osbuild (products:seq<DriverTool.LenovoCatalog.Product>) =
        let sccmPackageInfos = 
            products
            |> Seq.filter (fun p -> p.Name = name && p.Os = os && (p.OsBuild.Value = osbuild))
            |> Seq.toArray
        match sccmPackageInfos.Length > 0 with
        | true -> 
            sccmPackageInfos |> Seq.head
        | false -> 
            products
            |> Seq.filter (fun p -> p.Name = name && p.Os = os && (p.OsBuild.Value = "*"))
            |> Seq.head

    let getSccmDriverPackageInfo (modelCode:ModelCode, operatingSystemCode:OperatingSystemCode)  : Result<SccmPackageInfo,Exception> =
        result
            {
                let! products = DriverTool.LenovoCatalog.getSccmPackageInfos
                let product = DriverTool.LenovoCatalog.findSccmPackageInfoByModelCode4AndOsAndBuild (modelCode.Value.Substring(0,4)) (DriverTool.LenovoCatalog.osShortNameToLenovoOs operatingSystemCode.Value) OperatingSystem.getOsBuildForCurrentSystem products
                let osBuild = product.Value.OsBuild.Value
                let! sccmPackage = getLenovoSccmPackageDownloadInfo product.Value.SccmDriverPackUrl.Value operatingSystemCode.Value osBuild 
                return sccmPackage
            }

    let downloadSccmPackage (cacheDirectory, sccmPackage:SccmPackageInfo) =
        result{
            let! installerdestinationFilePath = FileSystem.path (System.IO.Path.Combine(cacheDirectory,sccmPackage.InstallerFileName))
            let! installerUri = toUri sccmPackage.InstallerUrl
            let installerDownloadInfo = { SourceUri = installerUri;SourceChecksum = sccmPackage.InstallerChecksum;SourceFileSize = 0L;DestinationFile = installerdestinationFilePath}
            let! installerInfo = Web.downloadIfDifferent (installerDownloadInfo,false)
            let installerPath = FileSystem.pathValue installerInfo.DestinationFile

            let! readmeDestinationFilePath = FileSystem.path (System.IO.Path.Combine(cacheDirectory,sccmPackage.ReadmeFile.FileName))
            let! readmeUri = toUri sccmPackage.ReadmeFile.Url
            let readmeDownloadInfo = { SourceUri = readmeUri;SourceChecksum = sccmPackage.ReadmeFile.Checksum;SourceFileSize = 0L;DestinationFile = readmeDestinationFilePath}
            let! readmeInfo = Web.downloadIfDifferent (readmeDownloadInfo,false)
            let readmePath = FileSystem.pathValue readmeInfo.DestinationFile

            return {
                InstallerPath = installerPath
                ReadmePath = readmePath
                SccmPackage = sccmPackage;
            }            
        }         
    
    let extractSccmPackage (downloadedSccmPackage:DownloadedSccmPackageInfo, destinationPath:FileSystem.Path) =
        loggerl.Info("Extract SccmPackage installer...")        
        let arguments = sprintf "/VERYSILENT /DIR=\"%s\" /EXTRACT=\"YES\"" (FileSystem.pathValue destinationPath)
        match (FileSystem.existingFilePath downloadedSccmPackage.InstallerPath) with
        |Ok fp -> 
            match DriverTool.ProcessOperations.startConsoleProcess (FileSystem.existingFilePathValueToPath fp, arguments, FileSystem.pathValue destinationPath, -1, null, null, false) with
            |Ok _ -> Result.Ok destinationPath
            |Error ex -> Result.Error (new Exception("Failed to extract Sccm package. " + ex.Message, ex))
        |Error ex -> Result.Error (new Exception("Sccm package installer not found. " + ex.Message, ex))
    
    let extractUpdate (rootDirectory:FileSystem.Path, (prefix,downloadedPackageInfo:DownloadedPackageInfo)) =
        result{
            let packageFolderName = getPackageFolderName downloadedPackageInfo.Package.Category downloadedPackageInfo.Package.ReleaseDate
            let! packageFolderPath = DriverTool.PathOperations.combine2Paths (FileSystem.pathValue rootDirectory, prefix + "_" + packageFolderName)
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
                | Error ex -> Result.Error ex
            return! res
        }