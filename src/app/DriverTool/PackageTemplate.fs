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

    let extractPackageTemplate (destinationFolderPath:Path) =
        result {
            let! emptyDestinationFolderPath = DriverTool.DirectoryOperations.ensureDirectoryExistsAndIsEmpty (destinationFolderPath, true)
            let resourceNamesVsDestinationFilesMap = mapResourceNamesToFileNames (emptyDestinationFolderPath,getPackageTemplateEmbeddedResourceNames,resourceNameToDirectoryDictionary)
            let extractResult =
                resourceNamesVsDestinationFilesMap
                |> Seq.map (fun (resourceName, fileName) ->
                        extractEmbededResouceToFile (resourceName, fileName)
                    )
            return! (extractResult |> toAccumulatedResult)
        }
