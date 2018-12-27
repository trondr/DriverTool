﻿namespace DriverTool
open Microsoft.FSharp.Collections
open F

module CreateDriverPackage =
    let logger = Logging.getLoggerByName("CreateDriverPackage")
    
    open System
    open DriverTool
    open DriverTool.PackageXml
    open FSharp.Collections.ParallelSeq
    open Checksum
            
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

    open ExistingPath
    open System.Linq  

    open DriverTool.SystemInfo

    let extractUpdateFunc (manufacturer:Manufacturer) = 
        match manufacturer.Value with
        |ManufacturerName.Dell ->        
            DellUpdates.extractUpdate
        |ManufacturerName.Lenovo ->        
            LenovoUpdates.extractUpdate
        |_ -> raise(new Exception("Manufacturer not supported: " + manufacturer.Value.ToString()))

    let getPrefixes count =
        Array.init count (fun index -> ((index+1)*10).ToString("D3"))
    
    let extractUpdates (rootDirectory,manufacturer:Manufacturer, downloadedPackageInfos:seq<DownloadedPackageInfo>) = 
        let downloadedPackageInfosList = downloadedPackageInfos.ToList()
        let prefixes = getPrefixes downloadedPackageInfosList.Count
        let extractUpdate = extractUpdateFunc manufacturer
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

    let createInstallScriptFile (installScriptPath: Path, installCommandLine:string,manufacturer:Manufacturer,logDirectory:string) =
        try
            use sw = new System.IO.StreamWriter(installScriptPath.Value)
            sw.WriteLine("Set ExitCode=0")
            sw.WriteLine("pushd \"%~dp0\"")
            sw.WriteLine("")
            sw.WriteLine("IF NOT EXIST \"{0}\" md \"{0}\"", logDirectory)
            sw.WriteLine(installCommandLine)
            if (packageIsUsingDpInstDuringInstall (installScriptPath, installCommandLine)) then
                sw.WriteLine("")
                sw.WriteLine("Set DpInstExitCode=%errorlevel%")
                sw.WriteLine("%~dp0..\\DpInstExitCode2ExitCode.exe %DpInstExitCode%")
            else
                sw.WriteLine("")
                sw.WriteLine("REM Set DpInstExitCode=%errorlevel%")
                sw.WriteLine("REM %~dp0..\\DpInstExitCode2ExitCode.exe %DpInstExitCode%")
            if(manufacturer.Value = ManufacturerName.Dell) then
                sw.WriteLine("")
                sw.WriteLine("Set DupExitCode=%errorlevel%")
                sw.WriteLine("%~dp0..\\DriverTool.DupExitCode2ExitCode.exe %DupExitCode%")
            sw.WriteLine("")
            sw.WriteLine("Set ExitCode=%errorlevel%")
            sw.WriteLine("popd")
            sw.WriteLine("EXIT /B %ExitCode%")
            Result.Ok installScriptPath
        with
        | _ as ex -> Result.Error ex
    
    let createSccmInstallScriptFile (installScriptPath: Path, installCommandLine:string) =
        try
            use sw = new System.IO.StreamWriter(installScriptPath.Value)
            sw.WriteLine("Set ExitCode=0")
            sw.WriteLine("pushd \"%~dp0\"")
            sw.WriteLine("")
            sw.WriteLine(installCommandLine)            
            sw.WriteLine("")
            sw.WriteLine("Set ExitCode=%errorlevel%")
            sw.WriteLine("popd")
            sw.WriteLine("EXIT /B %ExitCode%")
            Result.Ok installScriptPath
        with
        | _ as ex -> Result.Error ex

    let dtInstallPackageCmd = "DT-Install-Package.cmd"
    let dtUnInstallPackageCmd = "DT-UnInstall-Package.cmd"

    let createInstallScript (extractedUpdate:ExtractedPackageInfo,manufacturer:Manufacturer,logDirectory:string) =
        result{
            let! installScriptPath = 
                Path.create (System.IO.Path.Combine(extractedUpdate.ExtractedDirectoryPath,dtInstallPackageCmd))
            let installCommandLine = 
                extractedUpdate.DownloadedPackage.Package.InstallCommandLine.Replace("%PACKAGEPATH%\\","")            
            let installScriptResult = (createInstallScriptFile (installScriptPath,installCommandLine,manufacturer,logDirectory))
            let! unInstallScriptPath = 
                Path.create (System.IO.Path.Combine(extractedUpdate.ExtractedDirectoryPath,dtUnInstallPackageCmd))
            let unInstallScriptResult = (createUnInstallScriptFile (unInstallScriptPath))
            
            let createInstallScriptResult = 
                match ([|installScriptResult;unInstallScriptResult|]|> toAccumulatedResult) with
                |Error ex -> Result.Error ex
                |Ok _ -> installScriptResult

            return! createInstallScriptResult
        }

    let createInstallScripts (extractedUpdates:seq<ExtractedPackageInfo>,manufacturer:Manufacturer,logDirectory:string) =
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
                    RegistryValue="";
                    RegistryValueIs64Bit="";
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
    
    
    let createSccmPackageInstallScript (extractedSccmPackagePath:Path) =
        result{
            let! installScriptPath = 
                Path.create (System.IO.Path.Combine(extractedSccmPackagePath.Value,"DT-Install-Package.cmd"))
            let installCommandLine = "pnputil.exe /add-driver *.inf /install /subdirs"                      
            let installScriptResult = (createSccmInstallScriptFile (installScriptPath,installCommandLine))
            let! unInstallScriptPath = 
                Path.create (System.IO.Path.Combine(extractedSccmPackagePath.Value,"DT-UnInstall-Package.cmd"))
            let unInstallScriptResult = (createUnInstallScriptFile (unInstallScriptPath))            
            let createInstallScriptResult = 
                match ([|installScriptResult;unInstallScriptResult|]|> toAccumulatedResult) with
                |Error ex -> Result.Error ex
                |Ok _ -> installScriptResult
            return! createInstallScriptResult
        }        
    
    open DriverTool.Requirements

    let assertDriverPackageCreateRequirements =
        result{
                let! isAdministrator = assertIsAdministrator "Administrative privileges are required. Please run driver package create from an elevated command prompt."
                logger.Info("Installation is running with admin privileges: " + isAdministrator.ToString())                
                return isAdministrator
        }

    open DriverTool.PackageTemplate
        
    let getUpdatesFunc (manufacturer:Manufacturer,baseOnLocallyInstalledUpdates:bool) = 
        match manufacturer.Value with
        |ManufacturerName.Dell ->        
            match baseOnLocallyInstalledUpdates with
            |true -> DellUpdates.getLocalUpdates
            |false -> DellUpdates.getRemoteUpdates
        |ManufacturerName.Lenovo ->        
            match baseOnLocallyInstalledUpdates with
            |true -> LenovoUpdates.getLocalUpdates
            |false -> LenovoUpdates.getRemoteUpdates
        |_ -> raise(new Exception("Manufacturer not supported: " + manufacturer.Value.ToString()))

    let getSccmPackageFunc (manufacturer:Manufacturer) = 
        match manufacturer.Value with
        |ManufacturerName.Dell ->        
            DellUpdates.getSccmDriverPackageInfo
        |ManufacturerName.Lenovo ->        
            LenovoUpdates.getSccmDriverPackageInfo
        |_ -> raise(new Exception("Manufacturer not supported: " + manufacturer.Value.ToString()))
    
    let downloadSccmPackageFunc (manufacturer:Manufacturer) = 
        match manufacturer.Value with
        |ManufacturerName.Dell ->        
            DellUpdates.downloadSccmPackage
        |ManufacturerName.Lenovo ->        
            LenovoUpdates.downloadSccmPackage
        |_ -> raise(new Exception("Manufacturer not supported: " + manufacturer.Value.ToString()))

    let extractSccmPackageFunc (manufacturer:Manufacturer) = 
        match manufacturer.Value with
        |ManufacturerName.Dell ->        
            DellUpdates.extractSccmPackage
        |ManufacturerName.Lenovo ->        
            LenovoUpdates.extractSccmPackage
        |_ -> raise(new Exception("Manufacturer not supported: " + manufacturer.Value.ToString()))
    
    let createDriverPackageBase (packagePublisher:string,manufacturer:Manufacturer,systemFamily:SystemFamily,model: ModelCode, operatingSystem:OperatingSystemCode, destinationFolderPath: Path, baseOnLocallyInstalledUpdates, logDirectory) =             
            result {
                let! requirementsAreFullfilled = assertDriverPackageCreateRequirements
                logger.Info("All create package requirements are fullfilled: " + requirementsAreFullfilled.ToString())
                
                let getUpdates = getUpdatesFunc (manufacturer,baseOnLocallyInstalledUpdates) 

                let! packageInfos = getUpdates (model, operatingSystem, true, logDirectory)
                let uniquePackageInfos = packageInfos |> Seq.distinct
                let uniqueUpdates = uniquePackageInfos |> getUniqueUpdatesByInstallerName
                
                logger.Info("Downloading software and drivers...")
                let! updates = downloadUpdates (DriverTool.Configuration.getDownloadCacheDirectoryPath) uniqueUpdates
                let latestRelaseDate = getLastestReleaseDate updates
                                
                let getSccmPackage = getSccmPackageFunc manufacturer
                let! sccmPackage = getSccmPackage (model,operatingSystem)
                sccmPackage |> Logging.logToConsole |> ignore
                
                let downloadSccmPackage = downloadSccmPackageFunc manufacturer
                let! downloadedSccmPackage = downloadSccmPackage ((DriverTool.Configuration.getDownloadCacheDirectoryPath), sccmPackage)
                
                let releaseDate= (max latestRelaseDate (downloadedSccmPackage.SccmPackage.Released.ToString("yyyy-MM-dd")))
                let! versionedPackagePath = combine2Paths (destinationFolderPath.Value, releaseDate)

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
                
                let extractSccmPackage = extractSccmPackageFunc (manufacturer)                
                let! extractedSccmPackagePath = extractSccmPackage (downloadedSccmPackage, sccmPackageDestinationPath)
                let! sccmPackageInstallScriptResult = createSccmPackageInstallScript extractedSccmPackagePath

                let! installXmlPath = Path.create (System.IO.Path.Combine(versionedPackagePath.Value,"Install.xml"))
                let! existingInstallXmlPath = FileOperations.ensureFileExists installXmlPath
                let! installConfiguration = DriverTool.InstallXml.loadInstallXml existingInstallXmlPath
                let packageName = String.Format("{0} {1} {2} {3} {4} Drivers And Software {5}", packagePublisher, manufacturer.Value, systemFamily.Value, model.Value, operatingSystem.Value, releaseDate)
                let updatedInstallConfiguration = 
                    { installConfiguration with 
                        LogDirectory = (DriverTool.Environment.unExpandEnironmentVariables logDirectory);
                        LogFileName = toValidDirectoryName (String.Format("{0}.log", packageName));
                        PackageName = packageName;
                        PackageVersion = "1.0"
                        PackageRevision = "000"
                        ComputerModel = model.Value;
                        ComputerSystemFamiliy = systemFamily.Value;
                        ComputerVendor = manufacturer.Value.ToString();
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
    
    let createDriverPackage (packagePublisher:string,manufacturer:Manufacturer,systemFamily:SystemFamily,modelCode: ModelCode, operatingSystem:OperatingSystemCode, destinationFolderPath: Path,baseOnLocallyInstalledUpdates:bool, logDirectory) =
        Logging.genericLoggerResult Logging.LogLevel.Debug createDriverPackageBase (packagePublisher,manufacturer,systemFamily,modelCode, operatingSystem, destinationFolderPath,baseOnLocallyInstalledUpdates, logDirectory)

        