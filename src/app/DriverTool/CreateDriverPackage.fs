namespace DriverTool
open Microsoft.FSharp.Collections
open F

module CreateDriverPackage =
    let logger = Logging.getLoggerByName("CreateDriverPackage")
    
    open System
    open DriverTool
    open FSharp.Collections.ParallelSeq
    open Checksum

    let getUniqueUpdates packageInfos = 
        let uniqueUpdates = 
            packageInfos
            |> Seq.groupBy (fun p -> p.InstallerName)
            |> Seq.map (fun (k,v) -> v |>Seq.head)
        uniqueUpdates

    let verifyDownload downloadJob verificationWarningOnly =
        match (hasSameFileHash (downloadJob.DestinationFile, downloadJob.Checksum, downloadJob.Size)) with
        |true  -> Result.Ok downloadJob
        |false -> 
            let msg = String.Format("Destination file ('{0}') hash does not match source file ('{1}') hash.",downloadJob.DestinationFile,downloadJob.SourceUri.OriginalString)
            match verificationWarningOnly with
            |true ->
                logger.Warn(msg)
                Result.Ok downloadJob
            |false->Result.Error (new Exception(msg))
 
    open DriverTool.Web
    
    let downloadUpdatePlain (downloadInfo:DownloadInfo, ignoreVerificationErrors) =
        downloadIfDifferent (downloadInfo, ignoreVerificationErrors)         

    let downloadUpdate (downloadJob, ignoreVerificationErrors) =
        Logging.debugLoggerResult downloadUpdatePlain (downloadJob, ignoreVerificationErrors)

    let packageInfosToDownloadedPackageInfos destinationDirectory packageInfos =
        packageInfos
        |> Seq.map (fun p -> 
                        {
                            InstallerPath = getDestinationInstallerPath destinationDirectory p;
                            ReadmePath = getDestinationReadmePath destinationDirectory p;
                            PackageXmlPath = getDestinationPackageXmlPath destinationDirectory p;
                            Package = p;
                        }
                    )
    
    let (|TextFile|_|) (input:string) = if input.ToLower().EndsWith(".txt") then Some(input) else None

    let ignoreVerificationErrors downloadInfo =
        match downloadInfo.DestinationFile.Value with
        | TextFile x -> true
        | _ -> false
    
    let downloadUpdates destinationDirectory packageInfos = 
        let downloadJobs = 
            packageInfos 
            |> packageInfosToDownloadJobs destinationDirectory
            |> PSeq.map (fun dj -> downloadUpdate (dj,ignoreVerificationErrors dj))
            |> PSeq.toArray
            |> Seq.ofArray
            |> toAccumulatedResult
        match downloadJobs with
        |Ok _ -> 
            Result.Ok (packageInfosToDownloadedPackageInfos destinationDirectory packageInfos)
        |Error ex -> 
            Result.Error ex

    let toTitlePostFix (title:string) (version:string) (releaseDate:string) = 
        nullOrWhiteSpaceGuard title "title"
        let parts = title.Split('-');
        let titlePostfix = 
            match parts.Length with
            | 0 -> String.Empty
            | _ -> parts.[parts.Length - 1]
        toValidDirectoryName (String.Format("{0}_{1}_{2}",titlePostfix,version,releaseDate))
    
    open System.Linq
    open ExistingPath
    
    let toTitlePrefix (title:string) (category:string) (postFixLength: int) = 
        nullOrWhiteSpaceGuard title "title"
        nullGuard category "category"
        let parts = title.Split('-');
        let partsString =
            (parts.[0]).AsEnumerable().Take(57 - postFixLength - category.Length).ToArray()
        let titlePrefix = 
            category + "_" + new String(partsString);
        toValidDirectoryName titlePrefix    

    let getPackageFolderName (packageInfo:PackageInfo) =
        let validDirectoryName = 
            toValidDirectoryName packageInfo.Title
        let postfix = 
            toTitlePostFix validDirectoryName packageInfo.Version packageInfo.ReleaseDate
        let prefix = 
            toTitlePrefix validDirectoryName (packageInfo.Category |? String.Empty) postfix.Length
        let packageFolderName = 
            String.Format("{0}_{1}",prefix,postfix).Replace("__", "_").Replace("__", "_");
        packageFolderName

    let downloadedPackageInfoToExtractedPackageInfo (packageFolderPath:Path,downloadedPackageInfo) =
        {
            ExtractedDirectoryPath = packageFolderPath.Value;
            DownloadedPackage = downloadedPackageInfo;
        }

    let copyFile (sourceFilePath, destinationFilePath) =
        try
            System.IO.File.Copy(sourceFilePath, destinationFilePath, true)
            Result.Ok destinationFilePath
        with
        | ex -> Result.Error (new Exception(String.Format("Failed to copy file '{0}'->'{1}'.", sourceFilePath, destinationFilePath), ex))
    
    let extractPackageXml (downloadedPackageInfo, packageFolderPath:Path)  =
        let destinationFilePath = System.IO.Path.Combine(packageFolderPath.Value,downloadedPackageInfo.Package.PackageXmlName)
        match ExistingFilePath.New downloadedPackageInfo.PackageXmlPath with
        |Ok filePath -> 
            match (copyFile (filePath.Value, destinationFilePath)) with
            |Ok _ -> 
                Result.Ok (downloadedPackageInfoToExtractedPackageInfo (packageFolderPath,downloadedPackageInfo))
            |Error ex -> Result.Error ex
        |Error ex -> Result.Error ex

    let extractReadme (downloadedPackageInfo, packageFolderPath:Path)  =
        let destinationReadmeFilePath = System.IO.Path.Combine(packageFolderPath.Value,downloadedPackageInfo.Package.ReadmeName)
        match ExistingFilePath.New downloadedPackageInfo.ReadmePath with
        |Ok readmeFilePath -> 
            match (copyFile (readmeFilePath.Value, destinationReadmeFilePath)) with
            |Ok _ -> 
                Result.Ok (downloadedPackageInfoToExtractedPackageInfo (packageFolderPath,downloadedPackageInfo))
            |Error ex -> Result.Error ex
        |Error ex -> Result.Error ex

    let getFileNameFromCommandLine (commandLine:string) = 
        let fileName = commandLine.Split(' ').[0];
        fileName;

    let extractInstaller (downloadedPackageInfo, packageFolderPath:Path) =
        if(String.IsNullOrWhiteSpace(downloadedPackageInfo.Package.ExtractCommandLine)) then
           logger.Info("Installer does not support extraction, copy the installer directly to package folder...")
           let destinationInstallerFilePath = System.IO.Path.Combine(packageFolderPath.Value,downloadedPackageInfo.Package.InstallerName)
           match ExistingFilePath.New downloadedPackageInfo.InstallerPath with
           |Ok installerPath -> 
                match copyFile (installerPath.Value, destinationInstallerFilePath) with
                |Ok _ -> 
                    Result.Ok (downloadedPackageInfoToExtractedPackageInfo (packageFolderPath,downloadedPackageInfo))
                |Error ex -> Result.Error ex
           |Error ex -> 
                Result.Error ex
        else
            logger.Info("Installer supports extraction, extract installer...")
            let extractCommandLine = downloadedPackageInfo.Package.ExtractCommandLine.Replace("%PACKAGEPATH%",String.Format("\"{0}\"",packageFolderPath.Value))
            let fileName = getFileNameFromCommandLine extractCommandLine
            let arguments = extractCommandLine.Replace(fileName,"")
            match (ExistingFilePath.New downloadedPackageInfo.InstallerPath) with
            |Ok fp -> 
                match DriverTool.ProcessOperations.startProcess (fp.Value, arguments) with
                |Ok _ -> Result.Ok (downloadedPackageInfoToExtractedPackageInfo (packageFolderPath,downloadedPackageInfo))
                |Error ex -> Result.Error ex
            |Error ex -> Result.Error ex

    let extractUpdate (rootDirectory:Path, (prefix,downloadedPackageInfo:DownloadedPackageInfo)) =
        result{
            let packageFolderName = getPackageFolderName downloadedPackageInfo.Package
            let! packageFolderPath = DriverTool.PathOperations.combine2Paths (rootDirectory.Value, prefix + "_" + packageFolderName)
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

    let getPrefixes count =
        Array.init count (fun index -> ((index+1)*10).ToString("D3"))
    
    let extractUpdates (rootDirectory, downloadedPackageInfos:seq<DownloadedPackageInfo>) = 
        let downloadedPackageInfosList = downloadedPackageInfos.ToList()
        let prefixes = getPrefixes downloadedPackageInfosList.Count
        downloadedPackageInfosList
        |> Seq.zip prefixes
        |> PSeq.map (fun dp -> extractUpdate (rootDirectory, dp))
        |> PSeq.toArray
        |> Seq.ofArray
        |> toAccumulatedResult
     
    open System.Text.RegularExpressions
        
    let directoryContainsDpInst (directoryPath:string) =
        let dpinstFilesCount = 
            System.IO.Directory.GetFiles(directoryPath,"*.*")
            |> Seq.filter (fun fp -> System.Text.RegularExpressions.Regex.Match((new System.IO.FileInfo(fp)).Name,"dpinst",RegexOptions.IgnoreCase).Success)
            |> Seq.toList
            |> Seq.length            
        (dpinstFilesCount > 0)

    let packageIsUsingDpInstDuringInstall (installScriptPath:Path, installCommandLine:string) = 
        match (installCommandLine.StartsWith("dpinst.exe", true, System.Globalization.CultureInfo.InvariantCulture)) with
        | true -> 
            true
        | false -> 
            directoryContainsDpInst ((new System.IO.FileInfo(installScriptPath.Value)).Directory.FullName)

    let createUnInstallScriptFile (installScriptPath: Path) =
        try
            use sw = new System.IO.StreamWriter(installScriptPath.Value)
            sw.WriteLine("Set ExitCode=0")
            sw.WriteLine("pushd \"%~dp0\"")
            sw.WriteLine("@Echo Uninstall is not supported")
            sw.WriteLine("popd")
            sw.WriteLine("EXIT /B %ExitCode%")
            Result.Ok installScriptPath
        with
        | _ as ex -> Result.Error ex

    let createInstallScriptFile (installScriptPath: Path, installCommandLine:string) =
        try
            use sw = new System.IO.StreamWriter(installScriptPath.Value)
            sw.WriteLine("Set ExitCode=0")
            sw.WriteLine("pushd \"%~dp0\"")
            sw.WriteLine("")
            sw.WriteLine(installCommandLine)
            if (packageIsUsingDpInstDuringInstall (installScriptPath, installCommandLine)) then
                sw.WriteLine("")
                sw.WriteLine("Set DpInstExitCode=%errorlevel%")
                sw.WriteLine("%~dp0..\\DpInstExitCode2ExitCode.exe %DpInstExitCode%")
            else
                sw.WriteLine("")
                sw.WriteLine("REM Set DpInstExitCode=%errorlevel%")
                sw.WriteLine("REM %~dp0..\\DpInstExitCode2ExitCode.exe %DpInstExitCode%")
            sw.WriteLine("")
            sw.WriteLine("Set ExitCode=%errorlevel%")
            sw.WriteLine("popd")
            sw.WriteLine("EXIT /B %ExitCode%")
            Result.Ok installScriptPath
        with
        | _ as ex -> Result.Error ex
    
    let dtInstallPackageCmd = "DT-Install-Package.cmd"
    let dtUnInstallPackageCmd = "DT-UnInstall-Package.cmd"

    let createInstallScript (extractedUpdate:ExtractedPackageInfo) =
        result{
            let! installScriptPath = 
                Path.create (System.IO.Path.Combine(extractedUpdate.ExtractedDirectoryPath,dtInstallPackageCmd))
            let installCommandLine = 
                extractedUpdate.DownloadedPackage.Package.InstallCommandLine.Replace("%PACKAGEPATH%\\","")            
            let installScriptResult = (createInstallScriptFile (installScriptPath,installCommandLine))
            let! unInstallScriptPath = 
                Path.create (System.IO.Path.Combine(extractedUpdate.ExtractedDirectoryPath,dtUnInstallPackageCmd))
            let unInstallScriptResult = (createUnInstallScriptFile (unInstallScriptPath))
            
            let createInstallScriptResult = 
                match ([|installScriptResult;unInstallScriptResult|]|> toAccumulatedResult) with
                |Error ex -> Result.Error ex
                |Ok _ -> installScriptResult

            return! createInstallScriptResult
        }

    let createInstallScripts (extractedUpdates:seq<ExtractedPackageInfo>) =
        let extractedUpdatesList = 
            extractedUpdates.ToList()
        logger.InfoFormat("Creating install script for {0} packages...",extractedUpdatesList.Count)
        let installScripts = 
            extractedUpdatesList 
            |> PSeq.map (fun u -> (createInstallScript u) )       
            |> PSeq.toArray
            |> Seq.ofArray
            |> toAccumulatedResult
        installScripts
    
    open EmbeddedResouce

    let extractDpInstExitCodeToExitCodeExe (toolsPath:Path) =
        let exeResult = 
            extractEmbeddedResource ("DriverTool.Tools.DpInstExitCode2ExitCode.exe", toolsPath,"DpInstExitCode2ExitCode.exe")
        let configResult =
            extractEmbeddedResource ("DriverTool.Tools.DpInstExitCode2ExitCode.exe.config",toolsPath,"DpInstExitCode2ExitCode.exe.config")
        [|exeResult;configResult|]
        |> toAccumulatedResult
    
    open DriverTool.PathOperations

    let getLastestReleaseDate (updates:seq<DownloadedPackageInfo>) =
        updates
        |> Seq.map (fun p -> p.Package.ReleaseDate)
        |> Seq.max
    
    let writeTextToFile (text:string, filePath:Path) =
        try
            use sw = new System.IO.StreamWriter(filePath.Value)
            sw.Write(text)
            Result.Ok filePath
        with
        | ex -> Result.Error (new Exception(String.Format("Failed to write text to file '{0}'.", filePath.Value), ex))

    open PackageDefinition
    open LenovoCatalog

    let createPackageDefinitionFile (logDirectory, extractedUpdate:ExtractedPackageInfo) = 
        result{
            let! packageDefinitonSmsPath = combine2Paths (extractedUpdate.ExtractedDirectoryPath,"PackageDefinition.sms")   
            let installLogFileName = toValidDirectoryName ("Install_" + extractedUpdate.DownloadedPackage.Package.Title + "_" + extractedUpdate.DownloadedPackage.Package.Version + ".log")
            let unInstallLogFileName = toValidDirectoryName ("UnInstall_" + extractedUpdate.DownloadedPackage.Package.Title + "_" + extractedUpdate.DownloadedPackage.Package.Version + ".log")
            let installLogFile = System.IO.Path.Combine(logDirectory,installLogFileName)
            let unInstallLogFile = System.IO.Path.Combine(logDirectory,unInstallLogFileName)
            let packageDefinition =
                {
                    Name = extractedUpdate.DownloadedPackage.Package.Title;
                    Version = extractedUpdate.DownloadedPackage.Package.Version;
                    Language = "EN";
                    Publisher = "LENOVO";
                    InstallCommandLine = String.Format(dtInstallPackageCmd + " > \"{0}\"",installLogFile);
                    UnInstallCommandLine = String.Format(dtUnInstallPackageCmd + " > \"{0}\"",unInstallLogFile);
                }
            let writeTextToFileResult = writeTextToFile ((getPackageDefinitionContent packageDefinition), packageDefinitonSmsPath)                
            return! writeTextToFileResult
        }
    
    let createPackageDefinitionFiles (extractedUpdates:seq<ExtractedPackageInfo>, logDirectory:string) =
        extractedUpdates
        |> PSeq.map (fun u -> (createPackageDefinitionFile (logDirectory, u)))
        |> PSeq.toArray
        |> Seq.ofArray
        |> toAccumulatedResult
    
    let updateInstallXml (packagePublisher:string,manufacturer:Manufacturer,systemFamily:SystemFamily,model: ModelCode, operatingSystem:OperatingSystemCode, destinationFolderPath: Path, logDirectory) =
        ignore
    
    type DownloadedSccmPackageInfo = { InstallerPath:string; ReadmePath:string; SccmPackage:SccmPackageInfo}

    let downloadSccmPackage cacheDirectory (sccmPackage:SccmPackageInfo) =
        result{
            let! installerdestinationFilePath = Path.create (System.IO.Path.Combine(cacheDirectory,sccmPackage.InstallerFileName))
            let installerDownloadInfo = { SourceUri = new Uri(sccmPackage.InstallerUrl);SourceChecksum = sccmPackage.InstallerChecksum;SourceFileSize = 0L;DestinationFile = installerdestinationFilePath}
            let! readmeDestinationFilePath = Path.create (System.IO.Path.Combine(cacheDirectory,sccmPackage.ReadmeFileName))
            let readmeDownloadInfo = { SourceUri = new Uri(sccmPackage.ReadmeUrl);SourceChecksum = sccmPackage.ReadmeChecksum;SourceFileSize = 0L;DestinationFile = readmeDestinationFilePath}

            let! installerInfo = Web.downloadIfDifferent (installerDownloadInfo,false)
            let! readmeInfo = Web.downloadIfDifferent (readmeDownloadInfo,false)            
            return {
                InstallerPath = installerInfo.DestinationFile.Value
                ReadmePath = readmeInfo.DestinationFile.Value
                SccmPackage = sccmPackage;
            }            
        } 
        
    let extractSccmPackage (downloadedSccmPackage:DownloadedSccmPackageInfo, destinationPath:Path) =
        logger.Info("Extract SccmPackage installer...")        
        let arguments = String.Format("/VERYSILENT /DIR=\"{0}\" /EXTRACT=\"YES\"",destinationPath.Value)
        match (ExistingFilePath.New downloadedSccmPackage.InstallerPath) with
        |Ok fp -> 
            match DriverTool.ProcessOperations.startProcess (fp.Value, arguments) with
            |Ok _ -> Result.Ok destinationPath
            |Error ex -> Result.Error (new Exception("Failed to extract Sccm package. " + ex.Message, ex))
        |Error ex -> Result.Error (new Exception("Sccm package installer not found. " + ex.Message, ex))

    let createSccmPackageInstallScript (extractedSccmPackagePath:Path) =
        result{
            let! installScriptPath = 
                Path.create (System.IO.Path.Combine(extractedSccmPackagePath.Value,"DT-Install-Package.cmd"))
            let installCommandLine = "pnputil.exe /add-driver *.inf /install /subdirs"                      
            let installScriptResult = (createInstallScriptFile (installScriptPath,installCommandLine))
            let! unInstallScriptPath = 
                Path.create (System.IO.Path.Combine(extractedSccmPackagePath.Value,"DT-UnInstall-Package.cmd"))
            let unInstallScriptResult = (createUnInstallScriptFile (unInstallScriptPath))            
            let createInstallScriptResult = 
                match ([|installScriptResult;unInstallScriptResult|]|> toAccumulatedResult) with
                |Error ex -> Result.Error ex
                |Ok _ -> installScriptResult
            return! createInstallScriptResult
        }        
    
    open DriverTool.PackageTemplate

    let createDriverPackageSimple (packagePublisher:string,manufacturer:Manufacturer,systemFamily:SystemFamily,model: ModelCode, operatingSystem:OperatingSystemCode, destinationFolderPath: Path, logDirectory) =             
            result {
                let! packageInfos = ExportRemoteUpdates.getRemoteUpdates (model, operatingSystem, true)
                let uniquePackageInfos = packageInfos |> Seq.distinct
                let uniqueUpdates = uniquePackageInfos |> getUniqueUpdates
                
                logger.Info("Downloading software and drivers...")
                let! updates = downloadUpdates (DriverTool.Configuration.getDownloadCacheDirectoryPath) uniqueUpdates
                let latestRelaseDate = getLastestReleaseDate updates
                                
                let! products = getSccmPackageInfos
                let product = findSccmPackageInfoByModelCode4AndOsAndBuild (model.Value.Substring(0,4)) (osShortNameToLenovoOs operatingSystem.Value) getOsBuild products
                let osBuild = product.Value.OsBuild.Value
                let! sccmPackage = getLenovoSccmPackageDownloadInfo product.Value.SccmDriverPackUrl.Value operatingSystem.Value osBuild
                let! downloadedSccmPackage = downloadSccmPackage (DriverTool.Configuration.getDownloadCacheDirectoryPath) sccmPackage
                
                let releaseDate= (max latestRelaseDate (downloadedSccmPackage.SccmPackage.Released.ToString("yyyy-MM-dd")))
                let! versionedPackagePath = combine2Paths (destinationFolderPath.Value, releaseDate)

                logger.InfoFormat("Extracting package template to '{0}'",versionedPackagePath.Value)
                let! extractedPackagePaths = extractPackageTemplate versionedPackagePath
                logger.InfoFormat("Package template was extracted successfully from embedded resource. Number of files extracted: {0}", extractedPackagePaths.Count())

                let! driversPath = combine2Paths (versionedPackagePath.Value, "Drivers")
                logger.InfoFormat("Extracting drivers to folder '{0}'...", driversPath.Value)
                let! existingDriversPath = DirectoryOperations.ensureDirectoryExists (driversPath, true)
                let! extractedUpdates = extractUpdates (existingDriversPath, updates)
                let installScriptResults = createInstallScripts (extractedUpdates)
                let packageSmsResults = createPackageDefinitionFiles (extractedUpdates, logDirectory)

                let! sccmPackageDestinationPath = Path.create (System.IO.Path.Combine(existingDriversPath.Value,"005_Sccm_Package_" + downloadedSccmPackage.SccmPackage.Released.ToString("yyyy_MM_dd")))
                let! existingSccmPackageDestinationPath = DirectoryOperations.ensureDirectoryExists (sccmPackageDestinationPath, true)
                logger.InfoFormat("Extracting Sccm Package to folder '{0}'...", existingSccmPackageDestinationPath.Value)
                let! extractedSccmPackagePath = extractSccmPackage (downloadedSccmPackage, sccmPackageDestinationPath)
                let! sccmPackageInstallScriptResult = createSccmPackageInstallScript extractedSccmPackagePath

                let! installXmlPath = Path.create (System.IO.Path.Combine(versionedPackagePath.Value,"Install.xml"))
                let! existingInstallXmlPath = FileOperations.ensureFileExists installXmlPath
                let! installConfiguration = DriverTool.InstallXml.loadInstallXml existingInstallXmlPath
                let packageName = String.Format("{0} {1} {2} {3} {4} Drv & Sw", packagePublisher, manufacturer.Value, systemFamily.Value, model.Value, operatingSystem.Value)
                let updatedInstallConfiguration = 
                    { installConfiguration with 
                        LogDirectory = (DriverTool.Environment.unExpandEnironmentVariables logDirectory);
                        LogFileName = toValidDirectoryName (String.Format("{0}.log", packageName));
                        PackageName = packageName;
                        PackageVersion = "1.0"
                        ComputerModel = model.Value;
                        ComputerSystemFamiliy = systemFamily.Value;
                        ComputerVendor = manufacturer.Value;
                        OsShortName = operatingSystem.Value;
                        Publisher = packagePublisher
                    }
                let savedInstallConfiguration = DriverTool.InstallXml.saveInstallXml (existingInstallXmlPath, updatedInstallConfiguration)
                logger.InfoFormat("Saved install configuration to '{0}'. Value:", existingInstallXmlPath.Value, (Logging.valueToString savedInstallConfiguration))
                
                let res = 
                    match ([|installScriptResults;packageSmsResults|] |> toAccumulatedResult) with
                    |Ok _ -> Result.Ok ()
                    |Error ex -> Result.Error ex  
                return! res
            }
    
    let createDriverPackage (packagePublisher:string,manufacturer:Manufacturer,systemFamily:SystemFamily,modelCode: ModelCode, operatingSystem:OperatingSystemCode, destinationFolderPath: Path, logDirectory) =
        Logging.debugLoggerResult createDriverPackageSimple (packagePublisher,manufacturer,systemFamily,modelCode, operatingSystem, destinationFolderPath, logDirectory)

        