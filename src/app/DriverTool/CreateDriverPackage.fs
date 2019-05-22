namespace DriverTool
open Microsoft.FSharp.Collections
open F

module CreateDriverPackage =
        
    open System
    open System.Linq
    open System.Text.RegularExpressions
    open System.Text
    open DriverTool
    open DriverTool.PackageXml
    open FSharp.Collections.ParallelSeq    
    open DriverTool.ManufacturerTypes
    open DriverTool.Web
    open EmbeddedResouce
    open DriverTool.PathOperations
    open PackageDefinition
    open DriverTool.Requirements
    open DriverTool.PackageTemplate    
    open FileSystem
    
    let logger = Logging.getLoggerByName("CreateDriverPackage")

    let getUniqueUpdatesByInstallerName packageInfos = 
        let uniqueUpdates = 
            packageInfos
            |> Seq.groupBy (fun p -> p.Installer.Name)
            |> Seq.map (fun (k,v) -> v |>Seq.head)
        uniqueUpdates

    let verifyDownload downloadJob verificationWarningOnly =
        match (hasSameFileHash downloadJob) with
        |true  -> Result.Ok downloadJob
        |false -> 
            let msg = sprintf "Destination file ('%s') hash does not match source file ('%s') hash." (FileSystem.pathValue downloadJob.DestinationFile) downloadJob.SourceUri.OriginalString
            match verificationWarningOnly with
            |true ->
                logger.Warn(msg)
                Result.Ok downloadJob
            |false->Result.Error (new Exception(msg))
    
    let downloadUpdateBase (downloadInfo:DownloadInfo, ignoreVerificationErrors) =
        downloadIfDifferent (downloadInfo, ignoreVerificationErrors)         

    let downloadUpdate (downloadJob, ignoreVerificationErrors) =
        Logging.genericLoggerResult Logging.LogLevel.Debug downloadUpdateBase (downloadJob, ignoreVerificationErrors)

    let toFileName (filePath:FileSystem.Path) =
        System.IO.Path.GetFileName(FileSystem.pathValue filePath)

    let packageInfosToDownloadedPackageInfos destinationDirectory (packageInfos:seq<PackageInfo>) (downloadJobs:seq<DownloadInfo>) =
        packageInfos
        //Remove packages with no download jobs (download job for the package failed typically)
        |> Seq.filter(fun p ->
                        let downloadJob = downloadJobs|>Seq.tryFind(fun dj -> 
                                                let djFileName = toFileName dj.DestinationFile
                                                p.Installer.Name = djFileName
                                            )
                        optionToBoolean downloadJob
                    )
        //Create downloaded package info
        |> Seq.map (fun p -> 
                        {
                            InstallerPath = getDestinationInstallerPath destinationDirectory p;
                            ReadmePath = getDestinationReadmePath destinationDirectory p;
                            PackageXmlPath = getDestinationPackageXmlPath destinationDirectory p;
                            Package = p;
                        }
                    )
    
    let downloadUpdates destinationDirectory packageInfos = 
        let downloadJobs = 
            packageInfos             
            |> packageInfosToDownloadJobs destinationDirectory            
            |> PSeq.map (fun dj -> resultToOption logger (downloadUpdate (dj,ignoreVerificationErrors dj)))
            |> PSeq.toArray
            |> Seq.choose id //Remove all failed downloads            
            |> Seq.toArray            
        packageInfosToDownloadedPackageInfos destinationDirectory packageInfos downloadJobs
               
    let getPrefixes count =
        Array.init count (fun index -> ((index+1)*10).ToString("D3"))
    
    let extractUpdates (rootDirectory,manufacturer:Manufacturer, downloadedPackageInfos:seq<DownloadedPackageInfo>) = 
        let downloadedPackageInfosList = downloadedPackageInfos.ToList()
        let prefixes = getPrefixes downloadedPackageInfosList.Count
        let extractUpdate = DriverTool.Updates.extractUpdateFunc manufacturer
        downloadedPackageInfosList
        |> Seq.zip prefixes
        |> PSeq.map (fun dp -> extractUpdate (rootDirectory, dp))
        |> PSeq.toArray
        |> Seq.ofArray
        |> toAccumulatedResult
     
    let directoryContainsDpInst (directoryPath:string) =
        let dpinstFilesCount = 
            System.IO.Directory.GetFiles(directoryPath,"*.*")
            |> Seq.filter (fun fp -> System.Text.RegularExpressions.Regex.Match((new System.IO.FileInfo(fp)).Name,"dpinst",RegexOptions.IgnoreCase).Success)
            |> Seq.toList
            |> Seq.length            
        (dpinstFilesCount > 0)

    let packageIsUsingDpInstDuringInstall (installScriptPath:FileSystem.Path, installCommandLine:string) = 
        match (installCommandLine.StartsWith("dpinst.exe", true, System.Globalization.CultureInfo.InvariantCulture)) with
        | true -> 
            true
        | false -> 
            directoryContainsDpInst ((new System.IO.FileInfo(FileSystem.pathValue installScriptPath)).Directory.FullName)

    let createUnInstallScriptFileContent () =
        let sw = StringBuilder()
        sw.AppendLine("Set ExitCode=0")|>ignore
        sw.AppendLine("pushd \"%~dp0\"")|>ignore
        sw.AppendLine("@Echo Uninstall is not supported")|>ignore
        sw.AppendLine("popd")|>ignore
        sw.AppendLine("EXIT /B %ExitCode%")|>ignore
        sw.ToString()

    let writeTextToFileUnsafe (filePath:FileSystem.Path, text:string) =
        use sw = new System.IO.StreamWriter(FileSystem.pathValue filePath)
        sw.Write(text)
        filePath
    
    let writeTextToFile (filePath:FileSystem.Path) (text:string) =
        tryCatch writeTextToFileUnsafe (filePath, text)

    let createInstallScriptFileContent (packageIsUsingDpInst:bool, installCommandLine:string,manufacturer:Manufacturer, logDirectory:FileSystem.Path) =
        let sb = new StringBuilder()
        sb.AppendLine("Set ExitCode=0")|>ignore
        sb.AppendLine("pushd \"%~dp0\"")|>ignore
        sb.AppendLine("")|>ignore
        let logDirectoryLine = sprintf "IF NOT EXIST \"%s\" md \"%s\"" (FileSystem.pathValue logDirectory) (FileSystem.pathValue logDirectory)
        sb.Append(logDirectoryLine).AppendLine(String.Empty)|>ignore
        sb.AppendLine(installCommandLine)|>ignore
        if (packageIsUsingDpInst) then
            sb.AppendLine("")|>ignore
            sb.AppendLine("Set DpInstExitCode=%errorlevel%")|>ignore
            sb.AppendLine("\"%~dp0..\\DpInstExitCode2ExitCode.exe\" %DpInstExitCode%")|>ignore
        else
            sb.AppendLine("")|>ignore
            sb.AppendLine("REM Set DpInstExitCode=%errorlevel%")|>ignore
            sb.AppendLine("REM \"%~dp0..\\DpInstExitCode2ExitCode.exe\" %DpInstExitCode%")|>ignore
        match manufacturer with
        |Manufacturer.Dell _ ->
            sb.AppendLine("")|>ignore
            sb.AppendLine("Set DupExitCode=%errorlevel%")|>ignore
            sb.AppendLine("\"%~dp0..\\DriverTool.DupExitCode2ExitCode.exe\" %DupExitCode%")|>ignore
        |Manufacturer.Lenovo _ -> 
            sb.AppendLine("")|>ignore
        |Manufacturer.HP _ -> 
            sb.AppendLine("")|>ignore
        sb.AppendLine("")|>ignore
        sb.AppendLine("Set ExitCode=%errorlevel%")|>ignore
        sb.AppendLine("popd")|>ignore
        sb.AppendLine("EXIT /B %ExitCode%")|>ignore
        sb.ToString()
    
    let createSccmInstallScriptFileContent (installCommandLine:string) =
        let sb = new StringBuilder()
        sb.AppendLine("Set ExitCode=0")|>ignore
        sb.AppendLine("pushd \"%~dp0\"")|>ignore
        sb.AppendLine("")|>ignore
        sb.AppendLine(installCommandLine)|>ignore            
        sb.AppendLine("")|>ignore
        sb.AppendLine("Set ExitCode=%errorlevel%")|>ignore
        sb.AppendLine("popd")|>ignore
        sb.AppendLine("EXIT /B %ExitCode%")|>ignore
        sb.ToString()

    let dtInstallPackageCmd = "DT-Install-Package.cmd"
    let dtUnInstallPackageCmd = "DT-UnInstall-Package.cmd"

    let createInstallScript (extractedUpdate:ExtractedPackageInfo,manufacturer:Manufacturer,logDirectory:FileSystem.Path) =
        result{
            let! installScriptPath = PathOperations.combine2Paths(extractedUpdate.ExtractedDirectoryPath,dtInstallPackageCmd)
            let installCommandLine = extractedUpdate.DownloadedPackage.Package.InstallCommandLine.Replace("%PACKAGEPATH%\\","")            
            let packageIsUsingDpInst = packageIsUsingDpInstDuringInstall (installScriptPath, installCommandLine)

            let installScriptContent = (createInstallScriptFileContent (packageIsUsingDpInst,installCommandLine,manufacturer,logDirectory))
            let! installScript = writeTextToFile installScriptPath installScriptContent
            
            let! unInstallScriptPath = PathOperations.combine2Paths(extractedUpdate.ExtractedDirectoryPath,dtUnInstallPackageCmd)
            let! unInstallScript = writeTextToFile unInstallScriptPath (createUnInstallScriptFileContent())
            
            return installScript
        }

    let createInstallScripts (extractedUpdates:seq<ExtractedPackageInfo>,manufacturer:Manufacturer,logDirectory:FileSystem.Path) =
        let extractedUpdatesList = 
            extractedUpdates.ToList()
        logger.InfoFormat("Creating install script for {0} packages...",extractedUpdatesList.Count)
        let installScripts = 
            extractedUpdatesList 
            |> PSeq.map (fun u -> (createInstallScript (u, manufacturer,logDirectory)) )       
            |> PSeq.toArray
            |> Seq.ofArray
            |> toAccumulatedResult
        installScripts
    
    let extractDpInstExitCodeToExitCodeExe (toolsPath:FileSystem.Path) =
        let exeResult = 
            extractEmbeddedResource ("DriverTool.Tools.DpInstExitCode2ExitCode.exe", toolsPath,"DpInstExitCode2ExitCode.exe")
        let configResult =
            extractEmbeddedResource ("DriverTool.Tools.DpInstExitCode2ExitCode.exe.config",toolsPath,"DpInstExitCode2ExitCode.exe.config")
        [|exeResult;configResult|]
        |> toAccumulatedResult
    
    let getLastestReleaseDate (updates:seq<DownloadedPackageInfo>) =
        match (Seq.isEmpty updates) with
        |true -> 
            String.Empty
        |false -> 
            updates
            |> Seq.map (fun p -> p.Package.ReleaseDate)
            |> Seq.max

    let createPackageDefinitionFile (logDirectory, extractedUpdate:ExtractedPackageInfo) = 
        result{
            let! packageDefinitonSmsPath = combine2Paths (extractedUpdate.ExtractedDirectoryPath,"PackageDefinition.sms")   
            let installLogFileName = toValidDirectoryName ("Install_" + extractedUpdate.DownloadedPackage.Package.Title + "_" + extractedUpdate.DownloadedPackage.Package.Version + ".log")
            let unInstallLogFileName = toValidDirectoryName ("UnInstall_" + extractedUpdate.DownloadedPackage.Package.Title + "_" + extractedUpdate.DownloadedPackage.Package.Version + ".log")
            let installLogFile = System.IO.Path.Combine(FileSystem.pathValue logDirectory,installLogFileName)
            let unInstallLogFile = System.IO.Path.Combine(FileSystem.pathValue logDirectory,unInstallLogFileName)
            let packageDefinition =
                {
                    Name = extractedUpdate.DownloadedPackage.Package.Title;
                    Version = extractedUpdate.DownloadedPackage.Package.Version;
                    Language = "EN";
                    Publisher = "LENOVO";
                    InstallCommandLine = dtInstallPackageCmd + sprintf " > \"%s\"" installLogFile;
                    UnInstallCommandLine = dtUnInstallPackageCmd + sprintf " > \"%s\"" unInstallLogFile;
                    RegistryValue="";
                    RegistryValueIs64Bit="";
                }
            let writeTextToFileResult = writeTextToFile packageDefinitonSmsPath (getPackageDefinitionContent packageDefinition)                
            return! writeTextToFileResult
        }
    
    let createPackageDefinitionFiles (extractedUpdates:seq<ExtractedPackageInfo>, logDirectory) =
        extractedUpdates
        |> PSeq.map (fun u -> (createPackageDefinitionFile (logDirectory, u)))
        |> PSeq.toArray
        |> Seq.ofArray
        |> toAccumulatedResult
    
    let createSccmPackageInstallScript (extractedSccmPackagePath:FileSystem.Path) =
        result{
            let! installScriptPath = 
                FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue extractedSccmPackagePath,"DT-Install-Package.cmd"))
            let installCommandLine = "pnputil.exe /add-driver *.inf /install /subdirs"                      
            
            let createSccmInstallScriptFileContent = createSccmInstallScriptFileContent installCommandLine
            let! installScript = writeTextToFile installScriptPath createSccmInstallScriptFileContent
            let! unInstallScriptPath = 
                FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue extractedSccmPackagePath,"DT-UnInstall-Package.cmd"))
            let! unInstallScriptResult = writeTextToFile unInstallScriptPath (createUnInstallScriptFileContent())
            return installScript
        }        
    
    let assertDriverPackageCreateRequirements =
        result{
                let! isAdministrator = assertIsAdministrator "Administrative privileges are required. Please run driver package create from an elevated command prompt."
                logger.Info(sprintf "Installation is running with admin privileges: %b" isAdministrator)                
                return isAdministrator
        }
    
    type DriverPackageCreationContext =
        {
            PackagePublisher:string
            Manufacturer:Manufacturer
            SystemFamily:SystemFamily
            Model:ModelCode
            OperatingSystem:OperatingSystemCode
            DestinationFolderPath:Path
            BaseOnLocallyInstalledUpdates:bool
            LogDirectory:Path
            ExcludeUpdateRegexPatterns: System.Text.RegularExpressions.Regex[]
            PackageTypeName:string
            ExcludeSccmPackage:bool
        }

    let toDriverPackageCreationContext packagePublisher manufacturer systemFamily modelCode operatingSystemCode destinationFolderPath baseOnLocallyInstalledUpdates logDirectory excludeUpdateRegexPatterns packageTypeName excludeSccmPackage =
        {
            PackagePublisher=packagePublisher
            Manufacturer=manufacturer
            SystemFamily=systemFamily
            Model=modelCode
            OperatingSystem=operatingSystemCode
            DestinationFolderPath=destinationFolderPath
            BaseOnLocallyInstalledUpdates=baseOnLocallyInstalledUpdates
            LogDirectory=logDirectory
            ExcludeUpdateRegexPatterns=excludeUpdateRegexPatterns
            PackageTypeName=packageTypeName
            ExcludeSccmPackage=excludeSccmPackage
        }

    open DriverTool.UpdatesContext
        
    let createDriverPackageBase (dpcc:DriverPackageCreationContext) =             
            result {
                let! requirementsAreFullfilled = assertDriverPackageCreateRequirements
                logger.Info(sprintf "All create package requirements are fullfilled: %b" requirementsAreFullfilled)
                                
                let getUpdates = DriverTool.Updates.getUpdatesFunc (dpcc.Manufacturer,dpcc.BaseOnLocallyInstalledUpdates) 

                logger.Info("Getting update infos...")
                let updatesRetrievalContext = toUpdatesRetrievalContext dpcc.Model dpcc.OperatingSystem true dpcc.LogDirectory dpcc.ExcludeUpdateRegexPatterns
                
                let! packageInfos = getUpdates updatesRetrievalContext
                let uniquePackageInfos = packageInfos |> Seq.distinct
                let uniqueUpdates = uniquePackageInfos |> getUniqueUpdatesByInstallerName
                
                logger.Info("Downloading software and drivers...")
                let downloadedUpdates = downloadUpdates (DriverTool.Configuration.downloadCacheDirectoryPath) uniqueUpdates
                let latestRelaseDate = getLastestReleaseDate downloadedUpdates

                logger.Info("Update package info based from downloaded files (such as content in readme file)")
                let updatePackageInfo = DriverTool.Updates.updateDownloadedPackageInfoFunc (dpcc.Manufacturer)
                let! updatedInfoDownloadedUpdates = updatePackageInfo downloadedUpdates

                logger.Info("Getting SCCM package info...")
                let getSccmPackage = DriverTool.Updates.getSccmPackageFunc dpcc.Manufacturer
                let! sccmPackage = getSccmPackage (dpcc.Model,dpcc.OperatingSystem)
                logger.Info(sprintf "Sccm packge: %A" sccmPackage)
                
                logger.Info("Downloading SCCM package...")
                let downloadSccmPackage = DriverTool.Updates.downloadSccmPackageFunc dpcc.Manufacturer
                let! downloadedSccmPackage = downloadSccmPackage ((DriverTool.Configuration.downloadCacheDirectoryPath), sccmPackage)
                
                let releaseDate= (max latestRelaseDate (downloadedSccmPackage.SccmPackage.Released.ToString("yyyy-MM-dd")))
                let manufacturerName = manufacturerToName dpcc.Manufacturer
                let systemFamilyName = dpcc.SystemFamily.Value.Replace(manufacturerName,"").Trim()                
                let packageName = sprintf "%s %s %s %s %s %s %s" dpcc.PackagePublisher manufacturerName systemFamilyName dpcc.Model.Value dpcc.OperatingSystem.Value dpcc.PackageTypeName releaseDate
                let! versionedPackagePath = combine3Paths (FileSystem.pathValue dpcc.DestinationFolderPath, dpcc.Model.Value + "-" + dpcc.PackageTypeName, releaseDate)

                logger.InfoFormat("Extracting package template to '{0}'",versionedPackagePath)
                let! extractedPackagePaths = extractPackageTemplate versionedPackagePath
                logger.InfoFormat("Package template was extracted successfully from embedded resource. Number of files extracted: {0}", extractedPackagePaths.Count())

                let! driversPath = combine2Paths (FileSystem.pathValue versionedPackagePath, "Drivers")
                logger.InfoFormat("Extracting drivers to folder '{0}'...", driversPath)
                let! existingDriversPath = DirectoryOperations.ensureDirectoryExists true driversPath
                let! extractedUpdates = extractUpdates (existingDriversPath,dpcc.Manufacturer, updatedInfoDownloadedUpdates)
                let installScriptResults = createInstallScripts (extractedUpdates,dpcc.Manufacturer,dpcc.LogDirectory)
                let packageSmsResults = createPackageDefinitionFiles (extractedUpdates, dpcc.LogDirectory)

                let! sccmPackageExtractResult =
                    if (not dpcc.ExcludeSccmPackage) then               
                        result{
                            let! sccmPackageDestinationPath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue existingDriversPath,"005_Sccm_Package_" + downloadedSccmPackage.SccmPackage.Released.ToString("yyyy_MM_dd")))
                            let! existingSccmPackageDestinationPath = DirectoryOperations.ensureDirectoryExists true sccmPackageDestinationPath
                            logger.InfoFormat("Extracting Sccm Package to folder '{0}'...", existingSccmPackageDestinationPath)                
                            let extractSccmPackage = DriverTool.Updates.extractSccmPackageFunc (dpcc.Manufacturer)                
                            let! extractedSccmPackagePath = extractSccmPackage (downloadedSccmPackage, sccmPackageDestinationPath)
                            let! sccmPackageInstallScriptResult = createSccmPackageInstallScript extractedSccmPackagePath
                            return sccmPackageInstallScriptResult                    
                        }
                    else
                        Result.Ok existingDriversPath

                let! installXmlPath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue versionedPackagePath,"Install.xml"))
                let! existingInstallXmlPath = FileOperations.ensureFileExists installXmlPath
                let! installConfiguration = DriverTool.InstallXml.loadInstallXml existingInstallXmlPath
                
                let updatedInstallConfiguration = 
                    { installConfiguration with 
                        LogDirectory = (DriverTool.Environment.unExpandEnironmentVariables (FileSystem.pathValue dpcc.LogDirectory));
                        LogFileName = toValidDirectoryName (sprintf "%s.log" packageName);
                        PackageName = packageName;
                        PackageVersion = "1.0"
                        PackageRevision = "000"
                        ComputerModel = dpcc.Model.Value;
                        ComputerSystemFamiliy = dpcc.SystemFamily.Value;
                        ComputerVendor = DriverTool.ManufacturerTypes.manufacturerToName dpcc.Manufacturer;
                        OsShortName = dpcc.OperatingSystem.Value;
                        Publisher = dpcc.PackagePublisher
                    }
                let! savedInstallConfiguration = DriverTool.InstallXml.saveInstallXml (existingInstallXmlPath, updatedInstallConfiguration)
                logger.Info(sprintf "Saved install configuration to '%s'. Value: %A" (FileSystem.pathValue existingInstallXmlPath) savedInstallConfiguration)
                logger.Info("Create PackageDefinition.sms")
                let! packageDefinitionSmsPath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue versionedPackagePath,"PackageDefinition.sms"))                
                let! packageDefintionWriteResult = 
                    getPackageDefinitionFromInstallConfiguration updatedInstallConfiguration
                    |> writePackageDefinitionToFile packageDefinitionSmsPath
                logger.Info("Created PackageDefinition.sms")
                let res = 
                    match ([|installScriptResults;packageSmsResults|] |> toAccumulatedResult) with
                    |Ok _ -> Result.Ok ()
                    |Error ex -> Result.Error ex  
                return! res
            }
    
    let createDriverPackage driverPackageCreationContext =
        Logging.genericLoggerResult Logging.LogLevel.Debug createDriverPackageBase driverPackageCreationContext

        