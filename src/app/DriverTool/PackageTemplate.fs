namespace DriverTool

module PackageTemplate =
    open DriverTool.EmbeddedResource
        
    let isDriverPackageEmbeddedResourceName (resourceName:string) =
        resourceName.StartsWith("DriverTool.PackageTemplate")

    let getPackageTemplateEmbeddedResourceNames () =
        let embeddedResourceNames = 
                getAllEmbeddedResourceNames
                |> Seq.filter (fun x -> (isDriverPackageEmbeddedResourceName x))
        embeddedResourceNames
    
    let resourceNameToDirectoryDictionary (destinationFolderPath:FileSystem.Path) = 
        dict[
        "DriverTool.PackageTemplate", FileSystem.pathValue destinationFolderPath;        
        "DriverTool.PackageTemplate.Drivers", (System.IO.Path.Combine(FileSystem.pathValue destinationFolderPath,"Drivers"));        
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
                            let assembly = typeof<ThisAssembly>.Assembly
                            let fileName = getFileName filePath
                            let parentDirectory = getParentDirectory filePath
                            let! extractedFilePath = extractEmbeddedResourceByFileNameBase (fileName, parentDirectory, fileName, assembly)
                            return extractedFilePath
                        }
                    |true -> Result.Ok filePath
                return extractedFilepath
            }

    let getDriverToolFiles () =
        result
            {
                let assembly = typeof<ThisAssembly>.Assembly
                let! exeFilePath = FileSystem.path assembly.Location
                let! exeFileConfigPath = FileSystem.path (assembly.Location + ".config")
                let! exeFileDirectoryPath = FileSystem.path ((new System.IO.FileInfo(FileSystem.pathValue exeFilePath))).Directory.FullName
                let! fsharpCoreDllPath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue exeFileDirectoryPath,"FSharp.Core.dll"))
                let! extractedFSharpCoreDllPath = extractIfNotExists fsharpCoreDllPath
                let! commonLoggingDllPath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue exeFileDirectoryPath,"Common.Logging.dll"))                
                let! extractedCommonLoggingDllPath = extractIfNotExists commonLoggingDllPath
                let driverToolFiles =
                    [|
                        yield exeFilePath
                        yield exeFileConfigPath
                        yield extractedFSharpCoreDllPath
                        yield extractedCommonLoggingDllPath
                    |]
                return driverToolFiles 
            }
        
    let toResult toValue fromValue  =
        match fromValue with
        |Ok _ -> Result.Ok toValue
        |Error ex -> Result.Error ex
        

    let copyDriverToolToDriverPackage (destinationFolderPath:FileSystem.Path) =
        result{
            logger.Info("Copy DriverTool.exe to driver package so that it can handle install and uninstall of the driver package.")
            let! driverToolFolderPath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue destinationFolderPath,"DriverTool"))
            let! existingDriverToolDirectoryPath = DriverTool.DirectoryOperations.ensureDirectoryExists true driverToolFolderPath
            let! driverToolFiles = getDriverToolFiles ()
            let! driverToolFilesCopied = 
                driverToolFiles
                |> (DriverTool.FileOperations.copyFilePaths existingDriverToolDirectoryPath)
            logger.Info("Adjust corflags on the copied DriverTool.exe so that the version of DriverTool.exe in the driver package will prefer to run in 64 bit process on a 64 bit operating system. This is required when installing and uninstalling the driver package.")
            let! adjustCorFlagResult =
                driverToolFilesCopied
                |>Seq.filter(fun p -> (FileSystem.pathValue p).EndsWith(".exe"))
                |>Seq.map(fun p -> 
                            (CorFlags.prefer32BitClear p)
                            |>toResult p                    
                    )
                |>toAccumulatedResult                
            return adjustCorFlagResult
        }

    let extractPackageTemplate (destinationFolderPath:FileSystem.Path) =
        result {
            let! emptyDestinationFolderPath = DriverTool.DirectoryOperations.ensureDirectoryExistsAndIsEmpty (destinationFolderPath, true)
            let resourceNamesVsDestinationFilesMap = mapResourceNamesToFileNames (emptyDestinationFolderPath,getPackageTemplateEmbeddedResourceNames(),resourceNameToDirectoryDictionary)
            let! extractedFiles =
                resourceNamesVsDestinationFilesMap
                |> Seq.map (fun (resourceName, fileName) ->
                        extractEmbededResouceToFile (resourceName, fileName)
                    )
                |> toAccumulatedResult
            let! copiedFiles = copyDriverToolToDriverPackage destinationFolderPath
            let files = Seq.append extractedFiles copiedFiles
            return files
        }
