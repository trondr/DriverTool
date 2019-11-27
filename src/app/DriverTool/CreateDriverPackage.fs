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
    open DriverTool.PathOperations
    open PackageDefinition
    open DriverTool.Requirements
    open DriverTool.PackageTemplate    
    open FileSystem
    open DriverTool.Logging
    
    let logger = Logging.getLoggerByName("CreateDriverPackage")

    let getUniqueUpdatesByInstallerName packageInfos = 
        let uniqueUpdates = 
            packageInfos
            |> Array.groupBy (fun p -> p.Installer.Name)
            |> Array.map (fun (k,v) -> v |>Array.head)
        uniqueUpdates

    let downloadUpdateBase (downloadInfo:DownloadInfo, ignoreVerificationErrors) =
        downloadIfDifferent (logger,downloadInfo, ignoreVerificationErrors)         

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
        |>Seq.toArray
    
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
        logger.Info(msg (sprintf "Creating install script for %i packages..." extractedUpdatesList.Count))
        let installScripts = 
            extractedUpdatesList 
            |> PSeq.map (fun u -> (createInstallScript (u, manufacturer,logDirectory)) )       
            |> PSeq.toArray
            |> Seq.ofArray
            |> toAccumulatedResult
        installScripts
    
    let getLastestReleaseDate (updates:seq<DownloadedPackageInfo>) =
        match (Seq.isEmpty updates) with
        |true -> 
            String.Empty
        |false -> 
            updates
            |> Seq.map (fun p -> p.Package.ReleaseDate)
            |> Seq.max

    let createPackageDefinitionFile (logDirectory, extractedUpdate:ExtractedPackageInfo, packagePublisher) = 
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
                    Publisher = packagePublisher;
                    InstallCommandLine = dtInstallPackageCmd + sprintf " > \"%s\"" installLogFile;
                    UnInstallCommandLine = dtUnInstallPackageCmd + sprintf " > \"%s\"" unInstallLogFile;
                    RegistryValue="";
                    RegistryValueIs64Bit="";
                }
            let writeTextToFileResult = writeTextToFile packageDefinitonSmsPath (getPackageDefinitionContent packageDefinition)                
            return! writeTextToFileResult
        }
    
    let createPackageDefinitionFiles (extractedUpdates:seq<ExtractedPackageInfo>, logDirectory, packagePublisher) =
        extractedUpdates
        |> PSeq.map (fun u -> (createPackageDefinitionFile (logDirectory, u, packagePublisher)))
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
                logger.Info(msg (sprintf "Installation is running with admin privileges: %b" isAdministrator))
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
            DoNotDownloadSccmPackage:bool
            SccmPackageInstaller:string
            SccmPackageReadme:string
            SccmPackageReleased:DateTime
        }

    let toDriverPackageCreationContext packagePublisher manufacturer systemFamily modelCode operatingSystemCode destinationFolderPath baseOnLocallyInstalledUpdates logDirectory excludeUpdateRegexPatterns packageTypeName excludeSccmPackage doNotDownloadSccmPackage sccmPackageInstaller sccmPackageReadme sccmPackageReleased =
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
            DoNotDownloadSccmPackage = doNotDownloadSccmPackage
            SccmPackageInstaller = sccmPackageInstaller
            SccmPackageReadme = sccmPackageReadme
            SccmPackageReleased = sccmPackageReleased
        }

    open DriverTool.UpdatesContext

    let toDownloadedSccmPackageInfo cacheFolderPath intallerName readmeName releasedDate =
        result{
            let! installerPath = PathOperations.combinePaths2 cacheFolderPath intallerName
            let! existingInstallerPath = FileOperations.ensureFileExists installerPath
            let! readmePath = PathOperations.combinePaths2 cacheFolderPath readmeName
            let! existingReadmePath = FileOperations.ensureFileExists readmePath
            let downloadedSccmPackageInfo =
                {
                    InstallerPath=FileSystem.pathValue existingInstallerPath; 
                    ReadmePath=FileSystem.pathValue existingReadmePath;                     
                    SccmPackage=
                        {
                            ReadmeFile=
                                {
                                    Url=String.Empty;
                                    Checksum=String.Empty; 
                                    FileName=readmeName;
                                    Size=0L
                                }
                            InstallerUrl=String.Empty;
                            InstallerChecksum=String.Empty;
                            InstallerFileName=intallerName;
                            Released=releasedDate;
                            Os=String.Empty;
                            OsBuild=String.Empty;
                        }                
                }
            return downloadedSccmPackageInfo
        }
        

        
    let createDriverPackageBase (dpcc:DriverPackageCreationContext) =             
            result {
                let! requirementsAreFullfilled = assertDriverPackageCreateRequirements
                logger.Info(msg (sprintf "All create package requirements are fullfilled: %b" requirementsAreFullfilled))
                
                let! cacheFolderPath = FileSystem.path DriverTool.Configuration.downloadCacheDirectoryPath
                let! existingCacheFolderPath = DirectoryOperations.ensureDirectoryExists true cacheFolderPath

                let getUpdates = DriverTool.Updates.getUpdatesFunc (logger, dpcc.Manufacturer,dpcc.BaseOnLocallyInstalledUpdates) 

                logger.Info("Getting update infos...")
                let updatesRetrievalContext = toUpdatesRetrievalContext dpcc.Model dpcc.OperatingSystem true dpcc.LogDirectory dpcc.ExcludeUpdateRegexPatterns                
                let! packageInfos = getUpdates cacheFolderPath updatesRetrievalContext
                let uniquePackageInfos = packageInfos |> Array.distinct
                let uniqueUpdates = uniquePackageInfos |> getUniqueUpdatesByInstallerName
                
                logger.Info("Downloading software and drivers...")                
                let downloadedUpdates = downloadUpdates existingCacheFolderPath uniqueUpdates
                let latestRelaseDate = getLastestReleaseDate downloadedUpdates

                logger.Info("Update package info based from downloaded files (such as content in readme file)")
                let updatePackageInfo = DriverTool.Updates.updateDownloadedPackageInfoFunc (dpcc.Manufacturer)
                let! updatedInfoDownloadedUpdates = updatePackageInfo downloadedUpdates

                logger.Info("Find and download SCCM package...")
                let! downloadedSccmPackage =
                    if(not dpcc.DoNotDownloadSccmPackage) then
                        result{
                            logger.Info("Getting SCCM package info...")
                            let getSccmPackage = DriverTool.Updates.getSccmPackageFunc dpcc.Manufacturer                
                            let! sccmPackage = getSccmPackage (dpcc.Model,dpcc.OperatingSystem,existingCacheFolderPath)
                            logger.Info(msg (sprintf "Sccm packge: %A" sccmPackage))
                
                            logger.Info("Downloading SCCM package...")
                            let downloadSccmPackage = DriverTool.Updates.downloadSccmPackageFunc dpcc.Manufacturer
                            let! downloadedSccmPackage = downloadSccmPackage (existingCacheFolderPath, sccmPackage)
                            return downloadedSccmPackage
                        }
                    else
                        logger.Info("Attempting to use manually downloaded sccm package...")
                        toDownloadedSccmPackageInfo cacheFolderPath dpcc.SccmPackageInstaller dpcc.SccmPackageReadme dpcc.SccmPackageReleased
                    
                let releaseDate= (max latestRelaseDate (downloadedSccmPackage.SccmPackage.Released.ToString("yyyy-MM-dd")))
                let manufacturerName = manufacturerToName dpcc.Manufacturer
                let systemFamilyName = dpcc.SystemFamily.Value.Replace(manufacturerName,"").Trim()                
                let osBuild = OperatingSystem.getOsBuildForCurrentSystem
                let packageName = sprintf "%s %s %s %s %s %s %s" manufacturerName systemFamilyName dpcc.Model.Value dpcc.OperatingSystem.Value osBuild dpcc.PackageTypeName releaseDate
                let! versionedPackagePath = combine4Paths (FileSystem.pathValue dpcc.DestinationFolderPath, dpcc.Model.Value, releaseDate + "-1.0", "Script")

                logger.Info(msg (sprintf "Extracting package template to '%A'" versionedPackagePath))
                let! extractedPackagePaths = extractPackageTemplate versionedPackagePath
                logger.Info(msg (sprintf "Package template was extracted successfully from embedded resource. Number of files extracted: %i" (extractedPackagePaths.Count())))

                let! driversPath = combine2Paths (FileSystem.pathValue versionedPackagePath, "Drivers")
                logger.Info(msg (sprintf "Extracting drivers to folder '%A'..." driversPath))
                let! existingDriversPath = DirectoryOperations.ensureDirectoryExists true driversPath
                let! extractedUpdates = extractUpdates (existingDriversPath,dpcc.Manufacturer, updatedInfoDownloadedUpdates)
                let installScriptResults = createInstallScripts (extractedUpdates,dpcc.Manufacturer,dpcc.LogDirectory)
                let packageSmsResults = createPackageDefinitionFiles (extractedUpdates, dpcc.LogDirectory, dpcc.PackagePublisher)

                let sccmPackageFolderName = "005_Sccm_Package_" + downloadedSccmPackage.SccmPackage.Released.ToString("yyyy_MM_dd")
                let! sccmPackageDestinationPath =
                    if (not dpcc.ExcludeSccmPackage) then               
                        result{
                            let! sccmPackageDestinationPath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue existingDriversPath, sccmPackageFolderName))
                            let! existingSccmPackageDestinationPath = DirectoryOperations.ensureDirectoryExists true sccmPackageDestinationPath
                            logger.Info(msg (sprintf "Extracting Sccm Package to folder '%A'..." existingSccmPackageDestinationPath))                
                            let extractSccmPackage = DriverTool.Updates.extractSccmPackageFunc (dpcc.Manufacturer)                
                            let! (extractedSccmPackagePath,_) = extractSccmPackage (downloadedSccmPackage, sccmPackageDestinationPath)
                            let! sccmPackageInstallScriptPath = createSccmPackageInstallScript extractedSccmPackagePath
                            logger.Info(sprintf "Created sccm package install script: %A" sccmPackageInstallScriptPath)
                            return sccmPackageDestinationPath
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
                logger.Info(msg (sprintf  "Saved install configuration to '%s'. Value: %A" (FileSystem.pathValue existingInstallXmlPath) savedInstallConfiguration))
                logger.Info("Create PackageDefinition.sms")
                let! packageDefinitionSmsPath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue versionedPackagePath,"PackageDefinition.sms"))                
                let packageDefinition = getPackageDefinitionFromInstallConfiguration updatedInstallConfiguration
                let! packageDefintionWriteResult = 
                    packageDefinition
                    |> writePackageDefinitionToFile packageDefinitionSmsPath
                logger.Info(sprintf "Created PackageDefinition.sms: %A" packageDefintionWriteResult)

                logger.Info("Create PackageDefinition-DISM.sms")
                let! packageDefinitionDimsSmsPath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue versionedPackagePath,"PackageDefinition-DISM.sms"))
                let packageDefintionDism = {packageDefinition with InstallCommandLine = "DISM.exe /Image:%OSDisk%\\ /Add-Driver /Driver:.\\Drivers\\" + sccmPackageFolderName + "\\ /Recurse"; }
                let! packageDefintionDismWriteResult = 
                    packageDefintionDism
                    |> writePackageDefinitionDismToFile packageDefinitionDimsSmsPath
                                        
                logger.Info(sprintf "Created PackageDefinition-DISM.sms: %A" packageDefintionDismWriteResult)

                let res = 
                    match ([|installScriptResults;packageSmsResults|] |> toAccumulatedResult) with
                    |Ok _ -> Result.Ok ()
                    |Result.Error ex -> Result.Error ex  
                return! res
            }
    
    let createDriverPackage driverPackageCreationContext =
        Logging.genericLoggerResult Logging.LogLevel.Debug createDriverPackageBase driverPackageCreationContext

        