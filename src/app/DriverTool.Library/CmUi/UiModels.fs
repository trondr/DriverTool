﻿namespace DriverTool.Library.CmUi
open DriverTool.Library

module UiModels =
    
    open System
    open Microsoft.FSharp.Reflection
    open DriverTool.Library.PackageXml
    open DriverTool.Library.PackageDefinition
    open DriverTool.Library.DriverPack
    open DriverTool.Library.Logging

    let getCacheFolderPath () =
        result{
            let! cacheFolderPath = FileSystem.path (DriverTool.Library.Configuration.getDownloadCacheDirectoryPath())
            let! existingCacheFolderPath = DirectoryOperations.ensureDirectoryExists true cacheFolderPath
            return existingCacheFolderPath
        }

    open DriverTool.Library.PackageDefinitionSms
    open DriverTool.Library.DriverPack

    /// Package CM drivers
    let packageSccmPackage (cacheFolderPath:FileSystem.Path) (reportProgress:reportProgressFunction) (driverPack:DriverPackInfo) : Result<DownloadedDriverPackInfo,Exception> =
        result{
            logger.Warn(sprintf "TODO: Packaging '%s' (%A)..." driverPack.Model driverPack)
            let! manufacturer = ManufacturerTypes.manufacturerStringToManufacturer(driverPack.Manufacturer,false)
            
            logger.Warn("Preparing package folder...")
            let! destinationRootFolderPath = FileSystem.path @"c:\temp\D"
            let osBuild = 
                match(driverPack.OsBuild)with
                |"*" -> "All"
                |_ -> driverPack.OsBuild
            let packageVersion = (driverPack.Released.ToString("yyyy-MM-dd"))
            let packageName = sprintf "%s %s %s CM Drivers %s" driverPack.Manufacturer (driverPack.ModelCodes.[0]) osBuild packageVersion
            let! packageFolderPath = 
                PathOperations.combinePaths2 destinationRootFolderPath packageName
                |>DirectoryOperations.ensureFolderPathExists' true (Some "Package folder does not exist.")
                |>DirectoryOperations.ensureFolderPathIsEmpty' (Some "Package folder is not empty.")
            let! packageScriptsFolderPath = 
                PathOperations.combinePaths2 packageFolderPath "Scripts"
                |>DirectoryOperations.ensureFolderPathExists' true (Some "Package scripts folder does not exist.")                
            let! packageDriversFolderPath = 
                PathOperations.combinePaths2 packageScriptsFolderPath "Drivers"
                |> DirectoryOperations.ensureFolderPathExists' true (Some "Package drivers folder does not exist.")
                        
            reportProgress (sprintf "Downloading CM Drivers for model '%s'..." driverPack.Model) String.Empty String.Empty None true None
            let downloadDriverPackInfo = DriverTool.Updates.downloadDriverPackInfoFunc manufacturer
            let! downloadedDriverPackInfo = downloadDriverPackInfo cacheFolderPath reportProgress driverPack
            
            reportProgress (sprintf "Extracting CM Drivers for model '%s'..." driverPack.Model) String.Empty String.Empty None true None
            let extractDriverPackInfo = DriverTool.Updates.extractDriverPackInfoFunc manufacturer
            
            let cmDriversFolderName = "005_CM_Package_" + downloadedDriverPackInfo.DriverPack.Released.ToString("yyyy_MM_dd")
            let! cmDriversFolderPath = 
                PathOperations.combinePaths2 packageDriversFolderPath cmDriversFolderName                
            let! extractedDriverPackInfoFolder = extractDriverPackInfo downloadedDriverPackInfo cmDriversFolderPath

            logger.Info("Create PackageDefinition-DISM.sms")            
            let! dismProgram = PackageDefinitionSms.createSmsProgram "INSTALL-OFFLINE-OS" ("DISM.exe /Image:%OSDisk%\\ /Add-Driver /Driver:.\\Drivers\\" + cmDriversFolderName + "\\ /Recurse") "" SmsCanRunWhen.AnyUserStatus true true false (Some SmsProgramMode.Hidden) "Install INF drivers into the offline operating system using DISM in the WinPE phase of the OSD."
            let! pnpUtilProgram = PackageDefinitionSms.createSmsProgram "INSTALL-ONLINE-OS" ("pnputil.exe /add-driver .\\Drivers\\" + cmDriversFolderName + "\\*.inf /install /subdirs") "" SmsCanRunWhen.AnyUserStatus true true false (Some SmsProgramMode.Hidden) "Install INF drivers into the online operating system using PnPUtil."
            let! packageDefinition = PackageDefinitionSms.createSmsPackageDefinition packageName (driverPack.Released.ToString("yyyy-MM-dd")) None driverPack.Manufacturer "EN" false "Install INF drivers." [|dismProgram;pnpUtilProgram|] driverPack.ManufacturerWmiQuery driverPack.ModelWmiQuery
            let! packageDefinitionSmsPath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue packageScriptsFolderPath,"PackageDefinition.sms"))
            let! packageDefintionWriteResult = packageDefinition |> writeToFile logger packageDefinitionSmsPath
            logger.Info(sprintf "Created PackageDefinition.sms: %A" packageDefintionWriteResult)

            reportProgress (sprintf "Finished packaging INF drivers for model %s" driverPack.Model) String.Empty String.Empty None true None
            return downloadedDriverPackInfo
        }

        
        
