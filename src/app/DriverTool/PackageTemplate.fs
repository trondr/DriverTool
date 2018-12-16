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
        "DriverTool.PackageTemplate.Functions", System.IO.Path.Combine(destinationFolderPath.Value,"Functions");
        "DriverTool.PackageTemplate.Functions.Util", System.IO.Path.Combine(destinationFolderPath.Value,"Functions","Util");
        "DriverTool.PackageTemplate.Functions.Util._7Zip", System.IO.Path.Combine(destinationFolderPath.Value,"Functions","Util","7Zip");
        "DriverTool.PackageTemplate.Functions.Util.BitLocker", System.IO.Path.Combine(destinationFolderPath.Value,"Functions","Util","BitLocker");
        "DriverTool.PackageTemplate.Functions.Util.INIFileParser", System.IO.Path.Combine(destinationFolderPath.Value,"Functions","Util","INIFileParser");
        "DriverTool.PackageTemplate.Functions.Util.DriverTool.Util", System.IO.Path.Combine(destinationFolderPath.Value,"Functions","Util","DriverTool.Util");
        "DriverTool.PackageTemplate.Functions.Util.Log4Net", System.IO.Path.Combine(destinationFolderPath.Value,"Functions","Util","Log4Net");
        "DriverTool.PackageTemplate.Drivers", System.IO.Path.Combine(destinationFolderPath.Value,"Drivers");
        "DriverTool.PackageTemplate.Drivers_Example", System.IO.Path.Combine(destinationFolderPath.Value,"Drivers_Example");
        "DriverTool.PackageTemplate.Drivers_Example._020_Audio_Realtek_Audio_Driver_10_1_3_2017_08_23", System.IO.Path.Combine(destinationFolderPath.Value, "Drivers_Example", "020_Audio_Realtek_Audio_Driver_10_1_3_2017_08_23");
        "DriverTool.PackageTemplate.Drivers_Example._040_Camera_and_Card_Reader_Re_10_64_1_2_2018_03_29", System.IO.Path.Combine(destinationFolderPath.Value, "Drivers_Example", "040_Camera_and_Card_Reader_Re_10_64_1_2_2018_03_29");
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
