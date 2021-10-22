namespace DriverTool.Library

module PackageTemplate =
    open DriverTool.Library.EmbeddedResource
    let logger = DriverTool.Library.Logging.getLoggerByName "PackageTemplate"
    open DriverTool.Library.F
    open DriverTool.Library
        
    let packageTempateResourceName = "DriverTool.Library.PackageTemplate"

    let isDriverPackageEmbeddedResourceName (resourceName:string) =
        resourceName.StartsWith(packageTempateResourceName)

    let getPackageTemplateEmbeddedResourceNames () =
        let embeddedResourceNames =                 
                getAllEmbeddedResourceNames resourceAssembly
                |> Seq.filter (fun x -> (isDriverPackageEmbeddedResourceName x))
        embeddedResourceNames
    
    let resourceNameToDirectoryDictionary (destinationFolderPath:FileSystem.Path) = 
        dict[
        packageTempateResourceName, FileSystem.pathValue destinationFolderPath;        
        packageTempateResourceName + ".Drivers", (System.IO.Path.Combine(FileSystem.pathValue destinationFolderPath,"Drivers"));
        ]

    let getParentDirectory (path:FileSystem.Path) = 
        FileSystem.pathUnSafe ((new System.IO.FileInfo(FileSystem.pathValue path))).Directory.FullName
    
    let getFileName (filePath:FileSystem.Path) =
        ((new System.IO.FileInfo(FileSystem.pathValue filePath)).Name)

    let extractIfNotExists (filePath:FileSystem.Path) = 
        result
            {
                let filePathValue = FileSystem.pathValue filePath
                let! extractedFilepath =
                    match System.IO.File.Exists(filePathValue) with
                    |false ->
                        result{                            
                            let fileName = getFileName filePath
                            let parentDirectory = getParentDirectory filePath
                            let! extractedFilePath = extractEmbeddedResourceByFileNameBase (fileName, parentDirectory, fileName, resourceAssembly)
                            return extractedFilePath
                        }
                    |true -> Result.Ok filePath
                return extractedFilepath
            }

    let toResult toValue fromValue  =
        match fromValue with
        |Ok _ -> Result.Ok toValue
        |Result.Error ex -> Result.Error ex
        
    let copyDriverToolToDriverPackage (destinationFolderPath:FileSystem.Path) =
        result{
            logger.Info("Copy DriverTool.exe to driver package so that it can handle install and uninstall of the driver package.")
            let! driverToolFolderPath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue destinationFolderPath,"DriverTool"))
            let! existingDriverToolDirectoryPath = DriverTool.Library.DirectoryOperations.ensureDirectoryExists true driverToolFolderPath            
            let! exeFileDirectoryPath = FileSystem.path ((new System.IO.FileInfo(resourceAssembly.Location)).Directory.FullName)
            let! copyResult = Robocopy.roboCopy (exeFileDirectoryPath,existingDriverToolDirectoryPath,"*.* /MIR")
            return copyResult
        }

    let extractPackageTemplate (destinationFolderPath:FileSystem.Path) =
        result {             
            let! emptyDestinationFolderPath = DriverTool.Library.DirectoryOperations.ensureDirectoryExistsAndIsEmpty (destinationFolderPath, true)
            let resourceNamesVsDestinationFilesMap = mapResourceNamesToFileNames (emptyDestinationFolderPath,getPackageTemplateEmbeddedResourceNames(),resourceNameToDirectoryDictionary)
            let! extractedFiles =
                resourceNamesVsDestinationFilesMap
                |> Seq.map (fun (resourceName, fileName) ->
                        extractEmbededResouceToFile (resourceAssembly,resourceName, fileName)
                    )
                |> toAccumulatedResult
            let! copyExitCode = copyDriverToolToDriverPackage destinationFolderPath
            logger.Info(sprintf "Copy of DriverTool.exe to driver update package returned %d:" copyExitCode)
            return extractedFiles
        }
