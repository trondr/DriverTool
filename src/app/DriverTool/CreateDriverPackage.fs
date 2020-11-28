namespace DriverTool

module CreateDriverPackage =
        
    open System
    open System.Linq
    open DriverTool.Packaging
    open Microsoft.FSharp.Collections
    open DriverTool
    open DriverTool.Library.PackageXml
    open FSharp.Collections.ParallelSeq    
    open DriverTool.Library.ManufacturerTypes
    open DriverTool.Library.Web    
    open DriverTool.Library.PathOperations
    open DriverTool.Library.PackageDefinition
    open DriverTool.Library.Requirements
    open DriverTool.PackageTemplate        
    open DriverTool.Library.Logging
    open DriverTool.Library.F
    open DriverTool.Library
    open DriverTool.Library.UpdatesContext
    open DriverTool.Library.Environment
    open DriverTool.Library.HostMessages
    open DriverTool.Library.Messages
    open Akka.FSharp
    open DriverTool.Actors
    open DriverTool.DownloadActor    
    open DriverTool.ExtractActor
    open DriverTool.CreateDriverPackageActor

    let logger = DriverTool.Library.Logging.getLoggerByName("CreateDriverPackage")
    
    let downloadUpdates destinationDirectory packageInfos = 
        let downloadJobs = 
            packageInfos             
            |> packageInfosToDownloadJobs destinationDirectory            
            |> PSeq.map (fun dj -> resultToOption logger (DriverTool.DownloadActor.downloadUpdate (dj,ignoreVerificationErrors dj)))
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

    
    
    let createPackageDefinitionFiles (extractedUpdates:seq<ExtractedPackageInfo>, logDirectory, packagePublisher) =
        extractedUpdates
        |> PSeq.map (fun u -> (createPackageDefinitionFile (logDirectory, u, packagePublisher)))
        |> PSeq.toArray
        |> Seq.ofArray
        |> toAccumulatedResult
    
    
    
    let assertDriverPackageCreateRequirements =
        result{
                let! isAdministrator = assertIsAdministrator "Administrative privileges are required. Please run driver package create from an elevated command prompt."
                logger.Info(msg (sprintf "Installation is running with admin privileges: %b" isAdministrator))
                return isAdministrator
        }
    
    

    let createDriverPackageBase (dpcc:DriverPackageCreationContext) =             
            result {
                let! requirementsAreFullfilled = assertDriverPackageCreateRequirements
                logger.Info(msg (sprintf "All create package requirements are fullfilled: %b" requirementsAreFullfilled))

                let getUpdates = DriverTool.Updates.getUpdatesFunc (logger, dpcc.Manufacturer,dpcc.BaseOnLocallyInstalledUpdates) 

                logger.Info("Getting update infos...")
                let updatesRetrievalContext = toUpdatesRetrievalContext dpcc.Manufacturer dpcc.Model dpcc.OperatingSystem true dpcc.LogDirectory dpcc.CacheFolderPath dpcc.BaseOnLocallyInstalledUpdates dpcc.ExcludeUpdateRegexPatterns                
                let! packageInfos = getUpdates dpcc.CacheFolderPath updatesRetrievalContext
                let uniquePackageInfos = packageInfos |> Array.distinct
                let uniqueUpdates = uniquePackageInfos |> getUniqueUpdatesByInstallerName
                
                logger.Info("Downloading software and drivers...")                
                let downloadedUpdates = downloadUpdates dpcc.CacheFolderPath uniqueUpdates
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
                            let! sccmPackage = getSccmPackage (dpcc.Model,dpcc.OperatingSystem,dpcc.CacheFolderPath)
                            logger.Info(msg (sprintf "Sccm packge: %A" sccmPackage))
                
                            logger.Info("Downloading SCCM package...")
                            let downloadSccmPackage = DriverTool.Updates.downloadSccmPackageFunc dpcc.Manufacturer
                            let! downloadedSccmPackage = downloadSccmPackage (dpcc.CacheFolderPath, sccmPackage)
                            return downloadedSccmPackage
                        }
                    else
                        logger.Info("Attempting to use manually downloaded sccm package...")
                        toDownloadedSccmPackageInfo dpcc.CacheFolderPath dpcc.SccmPackageInstaller dpcc.SccmPackageReadme dpcc.SccmPackageReleased
                    
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
                let! installConfiguration = DriverTool.Library.InstallXml.loadInstallXml existingInstallXmlPath
                
                let updatedInstallConfiguration = 
                    { installConfiguration with 
                        LogDirectory = (unExpandEnironmentVariables (FileSystem.pathValue dpcc.LogDirectory));
                        LogFileName = toValidDirectoryName (sprintf "%s.log" packageName);
                        PackageName = packageName;
                        PackageVersion = "1.0"
                        PackageRevision = "000"
                        ComputerModel = dpcc.Model.Value;
                        ComputerSystemFamiliy = dpcc.SystemFamily.Value;
                        ComputerVendor = DriverTool.Library.ManufacturerTypes.manufacturerToName dpcc.Manufacturer;
                        OsShortName = dpcc.OperatingSystem.Value;
                        Publisher = dpcc.PackagePublisher
                    }
                let! savedInstallConfiguration = DriverTool.Library.InstallXml.saveInstallXml (existingInstallXmlPath, updatedInstallConfiguration)
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
        DriverTool.Library.Logging.genericLoggerResult LogLevel.Debug createDriverPackageBase driverPackageCreationContext

    let createDriverPackageBase2 (dpcc:DriverPackageCreationContext) = 
        result{
            let! requirementsAreFullfilled = assertDriverPackageCreateRequirements
            logger.Info(msg (sprintf "All create package requirements are fullfilled: %b" requirementsAreFullfilled))
            logger.Info("Starting x86 client actor system and x86 host actor system.")
            let (clientActorSystem, clientActorRef) = DriverTool.ActorSystem.startClientActorSystem()
            clientActorRef <! (new ConfirmStartHostMessage())
            logger.Info("Starting CreateDriverPackage actor.")
            let createDriverPackageActorRef = spawn clientActorSystem "CreateDriverPackageActor" (createDriverPackageActor dpcc clientActorRef)
            logger.Info(sprintf "Initializing CreateDriverPackage actor with driver package creation context: %A" dpcc)            
            createDriverPackageActorRef <! CreateDriverPackageMessage.Start
            clientActorSystem.WhenTerminated.Wait()            
            clientActorSystem.Dispose()
            logger.Info("Client actor system and host has been terminated.")
            return ()
        }

    let createDriverPackage2 driverPackageCreationContext =
        DriverTool.Library.Logging.genericLoggerResult LogLevel.Debug createDriverPackageBase2 driverPackageCreationContext