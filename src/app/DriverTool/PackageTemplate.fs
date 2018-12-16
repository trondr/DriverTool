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

    let copyDriverToolToDriverPackage (destinationFolderPath:Path) =
        result{
            let! driverToolFolderPath = Path.create (System.IO.Path.Combine(destinationFolderPath.Value,"DriverTool"))
            let! existingDriverToolDirectoryPath = DriverTool.DirectoryOperations.ensureDirectoryExists (driverToolFolderPath, true)
            let! copyResult = 
                getDriverToolFiles
                |> (DriverTool.FileOperations.copyFiles existingDriverToolDirectoryPath)            
            return copyResult
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
