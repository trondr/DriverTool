namespace DriverTool
open Microsoft.FSharp.Collections
open F

module CreateDriverPackage =
    let logger = Logging.getLoggerByName("CreateDriverPackage")
    
    open System
    open DriverTool
    open DriverTool.PackageXml
    open FSharp.Collections.ParallelSeq
    open Checksum
    open DriverTool.ManufacturerTypes
            
    let getUniqueUpdatesByInstallerName packageInfos = 
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
    
    let downloadUpdateBase (downloadInfo:DownloadInfo, ignoreVerificationErrors) =
        downloadIfDifferent (downloadInfo, ignoreVerificationErrors)         

    let downloadUpdate (downloadJob, ignoreVerificationErrors) =
        Logging.genericLoggerResult Logging.LogLevel.Debug downloadUpdateBase (downloadJob, ignoreVerificationErrors)

    let toFileName (filePath:Path) =
        System.IO.Path.GetFileName(filePath.Value)

    let packageInfosToDownloadedPackageInfos destinationDirectory (packageInfos:seq<PackageInfo>) (downloadJobs:seq<DownloadInfo>) =
        packageInfos
        //Remove packages with no download jobs (download job for the package failed typically)
        |> Seq.filter(fun p ->
                        let downloadJob = downloadJobs|>Seq.tryFind(fun dj -> 
                                                let djFileName = toFileName dj.DestinationFile
                                                p.InstallerName = djFileName
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
    
    let resultToOption (result : Result<_,Exception>) =
        match result with
        |Ok s -> Some s
        |Error ex -> 
            logger.Error(ex.Message)
            None

    let downloadUpdates destinationDirectory packageInfos = 
        let downloadJobs = 
            packageInfos             
            |> packageInfosToDownloadJobs destinationDirectory            
            |> PSeq.map (fun dj -> resultToOption (downloadUpdate (dj,ignoreVerificationErrors dj)))
            |> PSeq.toArray
            |> Seq.choose id //Remove all failed downloads            
            |> Seq.toArray            
        packageInfosToDownloadedPackageInfos destinationDirectory packageInfos downloadJobs

    open System.Linq
               
    let getPrefixes count =
        Array.init count (fun index -> ((index+1)*10).ToString("D3"))
    
    let extractUpdates (rootDirectory,manufacturer:Manufacturer2, downloadedPackageInfos:seq<DownloadedPackageInfo>) = 
        let downloadedPackageInfosList = downloadedPackageInfos.ToList()
        let prefixes = getPrefixes downloadedPackageInfosList.Count
        let extractUpdate = DriverTool.Updates.extractUpdateFunc manufacturer
        downloadedPackageInfosList
        |> Seq.zip prefixes
        |> PSeq.map (fun dp -> extractUpdate (rootDirectory, dp))
        |> PSeq.toArray
        |> Seq.ofArray
        |> toAccumulatedResult
     
    open System.Text.RegularExpressions
    open System.Text
        
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

    let createUnInstallScriptFileContent () =
        let sw = StringBuilder()
        sw.AppendLine("Set ExitCode=0")|>ignore
        sw.AppendLine("pushd \"%~dp0\"")|>ignore
        sw.AppendLine("@Echo Uninstall is not supported")|>ignore
        sw.AppendLine("popd")|>ignore
        sw.AppendLine("EXIT /B %ExitCode%")|>ignore
        sw.ToString()

    let writeTextToFileUnsafe (filePath:Path, text:string) =
        use sw = new System.IO.StreamWriter(filePath.Value)
        sw.Write(text)
        filePath
    
    let writeTextToFile (filePath:Path) (text:string) =
        tryCatch writeTextToFileUnsafe (filePath, text)

    let createInstallScriptFileContent (packageIsUsingDpInst:bool, installCommandLine:string,manufacturer:Manufacturer2,logDirectory:string) =
        let sb = new StringBuilder()
        sb.AppendLine("Set ExitCode=0")|>ignore
        sb.AppendLine("pushd \"%~dp0\"")|>ignore
        sb.AppendLine("")|>ignore
        sb.AppendFormat("IF NOT EXIST \"{0}\" md \"{0}\"",logDirectory).AppendLine(String.Empty)|>ignore
        sb.AppendLine("REM " + installCommandLine)|>ignore
        if (packageIsUsingDpInst) then
            sb.AppendLine("")|>ignore
            sb.AppendLine("Set DpInstExitCode=%errorlevel%")|>ignore
            sb.AppendLine("%~dp0..\\DpInstExitCode2ExitCode.exe %DpInstExitCode%")|>ignore
        else
            sb.AppendLine("")|>ignore
            sb.AppendLine("REM Set DpInstExitCode=%errorlevel%")|>ignore
            sb.AppendLine("REM %~dp0..\\DpInstExitCode2ExitCode.exe %DpInstExitCode%")|>ignore
        match manufacturer with
        |Manufacturer2.Dell _ ->
            sb.AppendLine("")|>ignore
            sb.AppendLine("Set DupExitCode=%errorlevel%")|>ignore
            sb.AppendLine("%~dp0..\\DriverTool.DupExitCode2ExitCode.exe %DupExitCode%")|>ignore
        |Manufacturer2.Lenovo _ -> 
            sb.AppendLine("")|>ignore
        |Manufacturer2.HP _ -> 
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

    let createInstallScript (extractedUpdate:ExtractedPackageInfo,manufacturer:Manufacturer2,logDirectory:string) =
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

    let createInstallScripts (extractedUpdates:seq<ExtractedPackageInfo>,manufacturer:Manufacturer2,logDirectory:string) =
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
                    RegistryValue="";
                    RegistryValueIs64Bit="";
                }
            let writeTextToFileResult = writeTextToFile packageDefinitonSmsPath (getPackageDefinitionContent packageDefinition)                
            return! writeTextToFileResult
        }
    
    let createPackageDefinitionFiles (extractedUpdates:seq<ExtractedPackageInfo>, logDirectory:string) =
        extractedUpdates
        |> PSeq.map (fun u -> (createPackageDefinitionFile (logDirectory, u)))
        |> PSeq.toArray
        |> Seq.ofArray
        |> toAccumulatedResult
    
    
    let createSccmPackageInstallScript (extractedSccmPackagePath:Path) =
        result{
            let! installScriptPath = 
                Path.create (System.IO.Path.Combine(extractedSccmPackagePath.Value,"DT-Install-Package.cmd"))
            let installCommandLine = "pnputil.exe /add-driver *.inf /install /subdirs"                      
            
            let createSccmInstallScriptFileContent = createSccmInstallScriptFileContent installCommandLine
            let! installScript = writeTextToFile installScriptPath createSccmInstallScriptFileContent
            let! unInstallScriptPath = 
                Path.create (System.IO.Path.Combine(extractedSccmPackagePath.Value,"DT-UnInstall-Package.cmd"))
            let! unInstallScriptResult = writeTextToFile unInstallScriptPath (createUnInstallScriptFileContent())
            return installScript
        }        
    
    open DriverTool.Requirements

    let assertDriverPackageCreateRequirements =
        result{
                let! isAdministrator = assertIsAdministrator "Administrative privileges are required. Please run driver package create from an elevated command prompt."
                logger.Info("Installation is running with admin privileges: " + isAdministrator.ToString())                
                return isAdministrator
        }

    open DriverTool.PackageTemplate
        
    let createDriverPackageBase (packagePublisher:string,manufacturer:Manufacturer2,systemFamily:SystemFamily,model: ModelCode, operatingSystem:OperatingSystemCode, destinationFolderPath: Path, baseOnLocallyInstalledUpdates, logDirectory) =             
            result {
                let! requirementsAreFullfilled = assertDriverPackageCreateRequirements
                logger.Info("All create package requirements are fullfilled: " + requirementsAreFullfilled.ToString())
                                
                let getUpdates = DriverTool.Updates.getUpdates (manufacturer,baseOnLocallyInstalledUpdates) 

                logger.Info("Getting update infos...")
                let! packageInfos = getUpdates (model, operatingSystem, true, logDirectory)
                let uniquePackageInfos = packageInfos |> Seq.distinct
                let uniqueUpdates = uniquePackageInfos |> getUniqueUpdatesByInstallerName
                
                logger.Info("Downloading software and drivers...")
                let updates = downloadUpdates (DriverTool.Configuration.getDownloadCacheDirectoryPath) uniqueUpdates
                let latestRelaseDate = getLastestReleaseDate updates
                
                logger.Info("Getting SCCM package info...")
                let getSccmPackage = DriverTool.Updates.getSccmPackageFunc manufacturer
                let! sccmPackage = getSccmPackage (model,operatingSystem)
                logger.Info(sprintf "Sccm packge: %A" sccmPackage)
                
                logger.Info("Downloading SCCM package...")
                let downloadSccmPackage = DriverTool.Updates.downloadSccmPackageFunc manufacturer
                let! downloadedSccmPackage = downloadSccmPackage ((DriverTool.Configuration.getDownloadCacheDirectoryPath), sccmPackage)
                
                let releaseDate= (max latestRelaseDate (downloadedSccmPackage.SccmPackage.Released.ToString("yyyy-MM-dd")))
                let packageName = String.Format("{0} {1} {2} {3} {4} Drivers {5}", packagePublisher, (manufacturerToName manufacturer), systemFamily.Value, model.Value, operatingSystem.Value, releaseDate)
                let packageFolderName = String.Format("{0} {1} {2} {3} {4} Drivers", packagePublisher, (manufacturerToName manufacturer), systemFamily.Value, model.Value, operatingSystem.Value)
                let! versionedPackagePath = combine3Paths (destinationFolderPath.Value, packageFolderName, releaseDate)

                logger.InfoFormat("Extracting package template to '{0}'",versionedPackagePath.Value)
                let! extractedPackagePaths = extractPackageTemplate versionedPackagePath
                logger.InfoFormat("Package template was extracted successfully from embedded resource. Number of files extracted: {0}", extractedPackagePaths.Count())

                let! driversPath = combine2Paths (versionedPackagePath.Value, "Drivers")
                logger.InfoFormat("Extracting drivers to folder '{0}'...", driversPath.Value)
                let! existingDriversPath = DirectoryOperations.ensureDirectoryExists (driversPath, true)
                let! extractedUpdates = extractUpdates (existingDriversPath,manufacturer, updates)
                let installScriptResults = createInstallScripts (extractedUpdates,manufacturer,logDirectory)
                let packageSmsResults = createPackageDefinitionFiles (extractedUpdates, logDirectory)

                let! sccmPackageDestinationPath = Path.create (System.IO.Path.Combine(existingDriversPath.Value,"005_Sccm_Package_" + downloadedSccmPackage.SccmPackage.Released.ToString("yyyy_MM_dd")))
                let! existingSccmPackageDestinationPath = DirectoryOperations.ensureDirectoryExists (sccmPackageDestinationPath, true)
                logger.InfoFormat("Extracting Sccm Package to folder '{0}'...", existingSccmPackageDestinationPath.Value)
                
                let extractSccmPackage = DriverTool.Updates.extractSccmPackageFunc (manufacturer)                
                let! extractedSccmPackagePath = extractSccmPackage (downloadedSccmPackage, sccmPackageDestinationPath)
                let! sccmPackageInstallScriptResult = createSccmPackageInstallScript extractedSccmPackagePath

                let! installXmlPath = Path.create (System.IO.Path.Combine(versionedPackagePath.Value,"Install.xml"))
                let! existingInstallXmlPath = FileOperations.ensureFileExists installXmlPath
                let! installConfiguration = DriverTool.InstallXml.loadInstallXml existingInstallXmlPath
                
                let updatedInstallConfiguration = 
                    { installConfiguration with 
                        LogDirectory = (DriverTool.Environment.unExpandEnironmentVariables logDirectory);
                        LogFileName = toValidDirectoryName (String.Format("{0}.log", packageName));
                        PackageName = packageName;
                        PackageVersion = "1.0"
                        PackageRevision = "000"
                        ComputerModel = model.Value;
                        ComputerSystemFamiliy = systemFamily.Value;
                        ComputerVendor = DriverTool.ManufacturerTypes.manufacturerToName manufacturer;
                        OsShortName = operatingSystem.Value;
                        Publisher = packagePublisher
                    }
                let savedInstallConfiguration = DriverTool.InstallXml.saveInstallXml (existingInstallXmlPath, updatedInstallConfiguration)
                logger.InfoFormat("Saved install configuration to '{0}'. Value:", existingInstallXmlPath.Value, (Logging.valueToString savedInstallConfiguration))
                logger.Info("Create PackageDefinition.sms")
                let! packageDefinitionSmsPath = Path.create (System.IO.Path.Combine(versionedPackagePath.Value,"PackageDefinition.sms"))                
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
    
    let createDriverPackage (packagePublisher:string,manufacturer:Manufacturer2,systemFamily:SystemFamily,modelCode: ModelCode, operatingSystem:OperatingSystemCode, destinationFolderPath: Path,baseOnLocallyInstalledUpdates:bool, logDirectory) =
        Logging.genericLoggerResult Logging.LogLevel.Debug createDriverPackageBase (packagePublisher,manufacturer,systemFamily,modelCode, operatingSystem, destinationFolderPath,baseOnLocallyInstalledUpdates, logDirectory)

        