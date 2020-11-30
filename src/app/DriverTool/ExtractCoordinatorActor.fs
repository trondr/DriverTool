namespace DriverTool

module ExtractCoordinatorActor =
    open DriverTool.Library.PackageXml    
    open DriverTool.Library.Logging
    let logger = getLoggerByName "ExtractCoordinatorActor"

    type ExtractCoordinatorContext =
        {
            Index : int;
            Packages: PackageInfo list
        }

    let updateExtractCoordinatorContext (extractCoordinatorContext:ExtractCoordinatorContext) (package:PackageInfo) index =
        let packageExists = List.exists (fun p -> p = package) extractCoordinatorContext.Packages
        if( not packageExists) then
            {extractCoordinatorContext with Packages=(extractCoordinatorContext.Packages @ [package]);Index=index}
        else
            extractCoordinatorContext

    let removePackageFromExtractCoordinatorContext (extractCoordinatorContext:ExtractCoordinatorContext) (package:PackageInfo) =
        let packageExists = List.exists (fun p -> p = package) extractCoordinatorContext.Packages
        if(packageExists) then            
            let updatedExtractCoordinatorContext = {extractCoordinatorContext with Packages=List.filter (fun p-> p <> package) extractCoordinatorContext.Packages}
            if(logger.IsDebugEnabled) then logger.Debug(sprintf "Extract coordinator context: %A" updatedExtractCoordinatorContext )
            updatedExtractCoordinatorContext
        else
            extractCoordinatorContext
