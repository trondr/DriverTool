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

    let getDriverToolFiles () =
        result
            {
                let! exeFileDirectoryPath = FileSystem.path ((new System.IO.FileInfo(resourceAssembly.Location)).Directory.FullName)
                let! exeFilePath = PathOperations.combinePaths2 exeFileDirectoryPath "DriverTool.exe"
                let! exeFileConfigPath = FileSystem.path (FileSystem.pathValue exeFilePath + ".config")
                let! dllFilePath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue exeFileDirectoryPath,"DriverTool.Library.dll"))
                let! extractedDllFilePath = extractIfNotExists dllFilePath
                let! fsharpCoreDllPath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue exeFileDirectoryPath,"FSharp.Core.dll"))
                let! extractedFSharpCoreDllPath = extractIfNotExists fsharpCoreDllPath
                let! commonLoggingDllPath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue exeFileDirectoryPath,"Common.Logging.dll"))                
                let! extractedCommonLoggingDllPath = extractIfNotExists commonLoggingDllPath
                let driverToolFiles =
                    [|
                        if(FileOperations.fileExists exeFilePath) then yield exeFilePath
                        if(FileOperations.fileExists exeFileConfigPath) then yield exeFileConfigPath
                        yield extractedDllFilePath
                        yield extractedFSharpCoreDllPath
                        yield extractedCommonLoggingDllPath
                    |]
                return driverToolFiles 
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
            let! driverToolFiles = getDriverToolFiles ()
            let! driverToolFilesCopied = 
                driverToolFiles
                |> (DriverTool.Library.FileOperations.copyFilePaths existingDriverToolDirectoryPath)                            
            return driverToolFilesCopied
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
            let! copiedFiles = copyDriverToolToDriverPackage destinationFolderPath
            let files = Seq.append extractedFiles copiedFiles
            return files
        }
