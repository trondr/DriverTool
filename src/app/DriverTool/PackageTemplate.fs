namespace DriverTool

module PackageTemplate =
    open DriverTool.EmbeddedResouce
    
    let isDriverPackageEmbeddedResourceName (resourceName:string) =
        resourceName.StartsWith("DriverTool.PackageTemplate")

    let getPackageTemplateEmbeddedResourceNames =
        let embeddedResourceNames = 
                getAllEmbeddedResourceNames
                |> Seq.filter (fun x -> (isDriverPackageEmbeddedResourceName x))
        embeddedResourceNames
    
    let resourceNameToDirectoryDictionary (destinationFolderPath:Path) = 
        dict[
        "DriverTool.PackageTemplate", destinationFolderPath.Value;        
        "DriverTool.PackageTemplate.Drivers", System.IO.Path.Combine(destinationFolderPath.Value,"Drivers");        
        ]

    let getDriverToolFiles =
        let exeFileName = System.Reflection.Assembly.GetExecutingAssembly().Location
        let exeFileDirectoryName = (new System.IO.FileInfo(exeFileName)).Directory.FullName
        seq{
            yield exeFileName
            yield exeFileName + ".config"
            yield System.IO.Path.Combine(exeFileDirectoryName,"FSharp.Core.dll")
        }

    let toResult toValue fromValue  =
        match fromValue with
        |Ok _ -> Result.Ok toValue
        |Error ex -> Result.Error ex
        

    let copyDriverToolToDriverPackage (destinationFolderPath:Path) =
        result{
            logger.Info("Copy DriverTool.exe to driver package so that it can handle install and uninstall of the driver package.")
            let! driverToolFolderPath = Path.create (System.IO.Path.Combine(destinationFolderPath.Value,"DriverTool"))
            let! existingDriverToolDirectoryPath = DriverTool.DirectoryOperations.ensureDirectoryExists (driverToolFolderPath, true)
            let! driverToolFiles = 
                getDriverToolFiles
                |> (DriverTool.FileOperations.copyFiles existingDriverToolDirectoryPath)
            logger.Info("Adjust corflags on the copied DriverTool.exe so that the version of DriverTool.exe in the driver package will prefer to run in 64 bit process on a 64 bit operating system. This is required when installing and uninstalling the driver package.")
            let! adjustCorFlagResult =
                driverToolFiles
                |>Seq.filter(fun p -> p.Value.EndsWith(".exe"))
                |>Seq.map(fun p -> 
                            (CorFlags.prefer32BitClear p)
                            |>toResult p                    
                    )
                |>toAccumulatedResult                
            return adjustCorFlagResult
        }

    let extractPackageTemplate (destinationFolderPath:Path) =
        result {
            let! emptyDestinationFolderPath = DriverTool.DirectoryOperations.ensureDirectoryExistsAndIsEmpty (destinationFolderPath, true)
            let resourceNamesVsDestinationFilesMap = mapResourceNamesToFileNames (emptyDestinationFolderPath,getPackageTemplateEmbeddedResourceNames,resourceNameToDirectoryDictionary)
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
