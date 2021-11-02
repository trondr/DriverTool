﻿namespace DriverTool.Library.CmUi
open DriverTool.Library

module UiModels =
    
    open System
    open Microsoft.FSharp.Reflection
    open DriverTool.Library.PackageXml
    open DriverTool.Library.PackageDefinition

    let getCacheFolderPath () =
        result{
            let! cacheFolderPath = FileSystem.path DriverTool.Library.Configuration.downloadCacheDirectoryPath
            let! existingCacheFolderPath = DirectoryOperations.ensureDirectoryExists true cacheFolderPath
            return existingCacheFolderPath
        }

    let loadSccmPackages (cacheFolderPath:FileSystem.Path) =
        result{            
            let manufacturers = FSharpType.GetUnionCases typeof<ManufacturerTypes.Manufacturer>
            let updateFunctions = manufacturers|> Array.map(fun m -> 
                                                                let manufacturer = FSharpValue.MakeUnion(m,[|(m.Name:>obj)|]):?> ManufacturerTypes.Manufacturer
                                                                let getFunc = DriverTool.Updates.getSccmPackagesFunc manufacturer
                                                                getFunc                                                            
                                                            )
            let! sccmPackagesArray = updateFunctions |> Array.map (fun f -> f(cacheFolderPath)) |> toAccumulatedResult
            let sccmpackages = sccmPackagesArray |> Seq.toArray |> Array.concat
            return sccmpackages
        }

    open DriverTool.Library.PackageDefinitionSms

    /// Package CM drivers
    let packageSccmPackage (cacheFolderPath:FileSystem.Path) (reportProgress:(bool->float option->string->unit)) (cmPackage:DriverPackInfo) : Result<DownloadedCmPackage,Exception> =
        result{
            logger.Warn(sprintf "TODO: Packaging '%s' (%A)..." cmPackage.Model cmPackage)
            let! manufacturer = ManufacturerTypes.manufacturerStringToManufacturer(cmPackage.Manufacturer,false)
            
            logger.Warn("Preparing package folder...")
            let! destinationRootFolderPath = FileSystem.path @"c:\temp\D"
            let osBuild = 
                match(cmPackage.OsBuild)with
                |"*" -> "All"
                |_ -> cmPackage.OsBuild
            let packageVersion = (cmPackage.Released.ToString("yyyy-MM-dd"))
            let packageName = sprintf "%s %s %s CM Drivers %s" cmPackage.Manufacturer (cmPackage.ModelCodes.[0]) osBuild packageVersion
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
                        
            reportProgress true None (sprintf "Downloading CM Drivers for model '%s'..." cmPackage.Model)
            let downloadCmPackage = DriverTool.Updates.downloadCmPackageFunc manufacturer
            let! downloadedCmPackage = downloadCmPackage cacheFolderPath reportProgress cmPackage
            
            reportProgress true None (sprintf "Extracting CM Drivers for model '%s'..." cmPackage.Model)
            let extractCmPackage = DriverTool.Updates.extractCmPackageFunc manufacturer
            
            let cmDriversFolderName = "005_CM_Package_" + downloadedCmPackage.CmPackage.Released.ToString("yyyy_MM_dd")
            let! cmDriversFolderPath = 
                PathOperations.combinePaths2 packageDriversFolderPath cmDriversFolderName                
            let! extractedCmPackageFolder = extractCmPackage downloadedCmPackage cmDriversFolderPath

            logger.Info("Create PackageDefinition-DISM.sms")            
            let! dismProgram = PackageDefinitionSms.createSmsProgram "INSTALL-OFFLINE-OS" ("DISM.exe /Image:%OSDisk%\\ /Add-Driver /Driver:.\\Drivers\\" + cmDriversFolderName + "\\ /Recurse") "" SmsCanRunWhen.AnyUserStatus true true false (Some SmsProgramMode.Hidden) "Install INF drivers into the offline operating system using DISM in the WinPE phase of the OSD."
            let! pnpUtilProgram = PackageDefinitionSms.createSmsProgram "INSTALL-ONLINE-OS" ("pnputil.exe /add-driver .\\Drivers\\" + cmDriversFolderName + "\\*.inf /install /subdirs") "" SmsCanRunWhen.AnyUserStatus true true false (Some SmsProgramMode.Hidden) "Install INF drivers into the online operating system using PnPUtil."
            let! packageDefinition = PackageDefinitionSms.createSmsPackageDefinition packageName (cmPackage.Released.ToString("yyyy-MM-dd")) None cmPackage.Manufacturer "EN" false "Install INF drivers." [|dismProgram;pnpUtilProgram|] cmPackage.ManufacturerWmiQuery cmPackage.ModelWmiQuery
            let! packageDefinitionSmsPath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue packageScriptsFolderPath,"PackageDefinition.sms"))
            let! packageDefintionWriteResult = packageDefinition |> writeToFile logger packageDefinitionSmsPath
            logger.Info(sprintf "Created PackageDefinition.sms: %A" packageDefintionWriteResult)

            reportProgress true None (sprintf "Finished packaging INF drivers for model %s" cmPackage.Model)            
            return downloadedCmPackage
        }

        
        
