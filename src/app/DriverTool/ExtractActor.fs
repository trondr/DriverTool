namespace DriverTool

module ExtractActor =
    
    open DriverTool.Packaging    
    open DriverTool.Library.Messages
    open DriverTool.Library.Logging
    open DriverTool.Library.F    
    open DriverTool.Library.PathOperations
    open DriverTool.Library
    open DriverTool.Library.PackageXml
    open Akka.FSharp
    open Akka.Actor
    let logger = getLoggerByName "ExtractActor"
    let loggerName = LoggerName "ExtractActor"
    
    let getPrefix (value:int) =        
        value.ToString("D3")

    let extractUpdate packagingContext downloadedPackage =
        match(result{
            let extractUpdate' = DriverTool.Updates.extractUpdateFunc packagingContext.Manufacturer
            let prefix = getPrefix packagingContext.ExtractFolderPrefix
            let! driversPath = combine2Paths (FileSystem.pathValue packagingContext.PackageFolderPath, "Drivers")
            let! existingDriversPath = DirectoryOperations.ensureDirectoryExists true driversPath
            logger.Info( (sprintf "Extracting Update Package '%s'..." downloadedPackage.Package.Installer.Name))
            let! extractedPackage = extractUpdate' (existingDriversPath,(prefix,downloadedPackage))
            let! installScript = createInstallScript (extractedPackage,packagingContext.Manufacturer,packagingContext.LogDirectory)
            let! packageDefinitionFile = createPackageDefinitionFile (packagingContext.LogDirectory, extractedPackage, packagingContext.PackagePublisher)            
            return extractedPackage
        })with
        |Result.Error  ex -> 
            logger.Error(sprintf "Failed to extract downloaded package '%s'. Error: %s" downloadedPackage.Package.Installer.Name (getAccumulatedExceptionMessages ex))
            CreateDriverPackageMessage.PackageExtracted (None, downloadedPackage)
        |Result.Ok ep -> 
            CreateDriverPackageMessage.PackageExtracted (Some ep,downloadedPackage)

    let extractSccmPackage packagingContext (downloadedSccmPackage:DownloadedSccmPackageInfo) =        
        match(result{
            let sccmPackageFolderName = "005_Sccm_Package_" + downloadedSccmPackage.SccmPackage.Released.ToString("yyyy_MM_dd")
            let! driversPath = combine2Paths (FileSystem.pathValue packagingContext.PackageFolderPath, "Drivers")
            let! existingDriversPath = DirectoryOperations.ensureDirectoryExists true driversPath
            let! sccmPackageDestinationPath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue existingDriversPath, sccmPackageFolderName))
            let! existingSccmPackageDestinationPath = DirectoryOperations.ensureDirectoryExists true sccmPackageDestinationPath
            logger.Info( (sprintf "Extracting Sccm Package to folder '%A'..." existingSccmPackageDestinationPath))                
            let extractSccmPackage' = DriverTool.Updates.extractSccmPackageFunc (packagingContext.Manufacturer)                
            let! (extractedSccmPackagePath,_) = extractSccmPackage' (downloadedSccmPackage, sccmPackageDestinationPath)
            let! sccmPackageInstallScriptPath = createSccmPackageInstallScript extractedSccmPackagePath
            logger.Info((sprintf "Created sccm package install script: %A" sccmPackageInstallScriptPath))
            return sccmPackageDestinationPath
        })with
        |Result.Error ex -> 
            logger.Error(sprintf "Failed to extract downloaded sccm package '%s'. Error: %s" downloadedSccmPackage.SccmPackage.InstallerFile.FileName (getAccumulatedExceptionMessages ex))
            CreateDriverPackageMessage.SccmPackageExtracted (None, downloadedSccmPackage)
        |Result.Ok p -> 
            let extractedSccmPackage = {ExtractedDirectoryPath=FileSystem.pathValue p;DownloadedSccmPackage=downloadedSccmPackage}
            CreateDriverPackageMessage.SccmPackageExtracted (Some extractedSccmPackage,downloadedSccmPackage)

    let extractActor (ownerActor:IActorRef)  (mailbox:Actor<_>) =
        
        if(logger.IsDebugEnabled) then mailbox.Context.System.Scheduler.ScheduleTellRepeatedly(System.TimeSpan.FromSeconds(10.0),System.TimeSpan.FromSeconds(5.0),mailbox.Context.Self,(CreateDriverPackageMessage.Info "ExtractActor is alive"),mailbox.Context.Self)

        let rec loop () = 
            actor {                                                
                let! message = mailbox.Receive()
                let (sender, self) = (mailbox.Context.Sender, mailbox.Context.Self)
                match message with                
                |ExtractPackage (packagingContext,downloadedPackage) ->
                    logger.Info( (sprintf "Extracting downloaded package: %s" downloadedPackage.Package.Installer.Name))
                    self <! extractUpdate packagingContext downloadedPackage                
                |PackageExtracted (extractedPackage,downloadedPackage) ->
                    logger.Info( (sprintf "Finished extraction attempt of downloaded package: %s" downloadedPackage.Package.Installer.Name))
                    ownerActor <! PackageExtracted (extractedPackage,downloadedPackage)
                |ExtractSccmPackage (packagingContext,downloadedSccmPackage) ->
                      logger.Info( (sprintf "Extracting downloaded sccm package: %s" downloadedSccmPackage.SccmPackage.InstallerFile.FileName))
                      self <! extractSccmPackage packagingContext downloadedSccmPackage
                |SccmPackageExtracted (extractedSccmPackage,downloadedSccmPackage)->
                    logger.Info( (sprintf "Finished extraction attempt of downloaded sccm package: %s" downloadedSccmPackage.SccmPackage.InstallerFile.FileName))
                    ownerActor <! SccmPackageExtracted (extractedSccmPackage,downloadedSccmPackage)
                |CreateDriverPackageMessage.Info msg ->
                    logger.Info(msg)
                | _ ->                    
                    logger.Warn((sprintf "Message not handled: %A" message))
                    return! loop ()
                return! loop ()
            }
        loop ()

