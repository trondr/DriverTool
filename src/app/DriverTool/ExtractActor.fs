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
    let logger = getLoggerByName "ExtractActor"
    
    let getPrefix value =
        ((value+1)*10).ToString("D3")

    let extractUpdate packagingContext downloadedPackage =
        match(result{
            let extractUpdate' = DriverTool.Updates.extractUpdateFunc packagingContext.Manufacturer
            let prefix = getPrefix packagingContext.PackageExtracts.Value
            let! extractedPackage = extractUpdate' (packagingContext.PackageFolderPath,(prefix,downloadedPackage))
            let! installScript = createInstallScript (extractedPackage,packagingContext.Manufacturer,packagingContext.LogDirectory)
            let! packageDefinitionFile = createPackageDefinitionFile (packagingContext.LogDirectory, extractedPackage, packagingContext.PackagePublisher)            
            return extractedPackage
        })with
        |Result.Error  ex -> CreateDriverPackageMessage.Error ex
        |Result.Ok ep -> CreateDriverPackageMessage.PackageExtracted ep

    let extractSccmPackage packagingContext (downloadedSccmPackage:DownloadedSccmPackageInfo) =        
        match(result{
            let sccmPackageFolderName = "005_Sccm_Package_" + downloadedSccmPackage.SccmPackage.Released.ToString("yyyy_MM_dd")
            let! driversPath = combine2Paths (FileSystem.pathValue packagingContext.PackageFolderPath, "Drivers")
            let! existingDriversPath = DirectoryOperations.ensureDirectoryExists true driversPath
            let! sccmPackageDestinationPath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue existingDriversPath, sccmPackageFolderName))
            let! existingSccmPackageDestinationPath = DirectoryOperations.ensureDirectoryExists true sccmPackageDestinationPath
            logger.Info(msg (sprintf "Extracting Sccm Package to folder '%A'..." existingSccmPackageDestinationPath))                
            let extractSccmPackage' = DriverTool.Updates.extractSccmPackageFunc (packagingContext.Manufacturer)                
            let! (extractedSccmPackagePath,_) = extractSccmPackage' (downloadedSccmPackage, sccmPackageDestinationPath)
            let! sccmPackageInstallScriptPath = createSccmPackageInstallScript extractedSccmPackagePath
            logger.Info(sprintf "Created sccm package install script: %A" sccmPackageInstallScriptPath)
            return sccmPackageDestinationPath
        })with
        |Result.Error ex -> CreateDriverPackageMessage.Error ex
        |Result.Ok p -> CreateDriverPackageMessage.SccmPackageExtracted {ExtractedDirectoryPath=FileSystem.pathValue p;DownloadedSccmPackage=downloadedSccmPackage}


    let extractActor (mailbox:Actor<_>) =
        
        let rec loop () = 
            actor {                                                
                let! message = mailbox.Receive()
                let (sender, self) = (mailbox.Context.Sender, mailbox.Context.Self)
                match message with                
                |ExtractPackage (packagingContext,downloadedPackage) ->
                    sender <! CreateDriverPackageMessage.Info (sprintf "Extracting downloaded package: %A" downloadedPackage)
                    sender <! extractUpdate packagingContext downloadedPackage
                |ExtractSccmPackage (packagingContext,downloadedSccmPackage) ->
                      sender <! CreateDriverPackageMessage.Info (sprintf "Extracting downloaded scc package: %A" downloadedSccmPackage)
                      sender <! extractSccmPackage packagingContext downloadedSccmPackage                  
                | _ ->
                    sender <! CreateDriverPackageMessage.Warning (sprintf "Message not handled: %A" message)
                    logger.Warn(sprintf "Message not handled: %A" message)
                    return! loop ()
                return! loop ()
            }
        loop ()

