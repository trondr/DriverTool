namespace DriverTool.Library.CmUi
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
    open DriverTool.Library.DriverUpdates

    /// Package CM drivers
    let packageSccmPackage (cacheFolderPath:FileSystem.Path) (reportProgress:reportProgressFunction) (driverPack:DriverPackInfo) : Result<FileSystem.Path,Exception> =
        result{
            
            reportProgress (sprintf "Downloading and Packaging '%s' (%A)...\n" driverPack.Model driverPack) String.Empty String.Empty None true None            
            let! manufacturer = ManufacturerTypes.manufacturerStringToManufacturer(driverPack.Manufacturer,false)
                        
            reportProgress (sprintf "Preparing package folder...\n") String.Empty String.Empty None true None
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
                        
            reportProgress (sprintf "Downloading CM Drivers for model '%s'...\n" driverPack.Model) String.Empty String.Empty None true None
            let downloadDriverPackInfo = DriverTool.Updates.downloadDriverPackInfoFunc manufacturer
            let! downloadedDriverPackInfo = downloadDriverPackInfo cacheFolderPath reportProgress driverPack
            
            reportProgress (sprintf "Extracting CM Drivers for model '%s'...\n" driverPack.Model) String.Empty String.Empty None true None
            let extractDriverPackInfo = DriverTool.Updates.extractDriverPackInfoFunc manufacturer
            
            let cmDriversFolderName = "005_CM_Package_" + downloadedDriverPackInfo.DriverPack.Released.ToString("yyyy_MM_dd")
            let! cmDriversFolderPath = 
                PathOperations.combinePaths2 packageDriversFolderPath cmDriversFolderName                
            let! extractedDriverPackInfoFolder = extractDriverPackInfo downloadedDriverPackInfo cmDriversFolderPath
            
            reportProgress (sprintf "Creating PackageDefinition.sms...\n") String.Empty String.Empty None true None
            let! dismProgram = PackageDefinitionSms.createSmsProgram "INSTALL-OFFLINE-OS" ("DISM.exe /Image:%OSDisk%\\ /Add-Driver /Driver:.\\Drivers\\" + cmDriversFolderName + "\\ /Recurse") "" SmsCanRunWhen.AnyUserStatus true true false (Some SmsProgramMode.Hidden) "Install INF drivers into the offline operating system using DISM in the WinPE phase of the OSD."
            let! pnpUtilProgram = PackageDefinitionSms.createSmsProgram "INSTALL-ONLINE-OS" ("pnputil.exe /add-driver .\\Drivers\\" + cmDriversFolderName + "\\*.inf /install /subdirs") "" SmsCanRunWhen.AnyUserStatus true true false (Some SmsProgramMode.Hidden) "Install INF drivers into the online operating system using PnPUtil."
            let! packageDefinition = PackageDefinitionSms.createSmsPackageDefinition packageName (driverPack.Released.ToString("yyyy-MM-dd")) None driverPack.Manufacturer "EN" false "Install INF drivers." [|dismProgram;pnpUtilProgram|] driverPack.ManufacturerWmiQuery driverPack.ModelWmiQuery
            let! packageDefinitionSmsPath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue packageScriptsFolderPath,"PackageDefinition.sms"))
            let! packageDefintionWriteResult = packageDefinition |> writeToFile logger packageDefinitionSmsPath
            reportProgress (sprintf "Created PackageDefinition.sms: %A\n" packageDefintionWriteResult) String.Empty String.Empty None true None            

            reportProgress (sprintf "Finished packaging INF drivers for model %s\n" driverPack.Model) String.Empty String.Empty None true None
            return packageDefintionWriteResult
        }

    open DriverTool.Library.PathOperations
    open DriverTool.Library.PackageTemplate
    
    let packageDriverUpdates (cacheFolderPath:FileSystem.Path) (reportProgress:reportProgressFunction) (modelInfo:ModelInfo) (packagePublisher:string) (packageInstallLogDirectory:string) : Result<FileSystem.Path,Exception> =
        result{            
            reportProgress (sprintf "Downloading and packaging driver updates for '%s' (%s)...\n" modelInfo.Name modelInfo.ModelCode) String.Empty String.Empty None true None
            let! manufacturer = ManufacturerTypes.manufacturerStringToManufacturer(modelInfo.Manufacturer,false)
            
            reportProgress (sprintf "Downloading driver updates...\n") String.Empty String.Empty None true None
            let downloadedDriverUpdates = DriverTool.CreateDriverPackage.downloadUpdates cacheFolderPath modelInfo.DriverUpdates
            let latestRelaseDate = DriverTool.CreateDriverPackage.getLastestReleaseDate downloadedDriverUpdates

            reportProgress (sprintf "Update package info based from downloaded files (such as content in readme file)...\n") String.Empty String.Empty None true None            
            let updatePackageInfo = DriverTool.Updates.updateDownloadedPackageInfoFunc (manufacturer)
            let! updatedInfoDownloadedUpdates = updatePackageInfo downloadedDriverUpdates

            reportProgress (sprintf "Preparing package folder...\n") String.Empty String.Empty None true None
            let! destinationRootFolderPath = FileSystem.path @"c:\temp\DU"
            let packageName = sprintf "%s %s %s %s %s %s" modelInfo.Manufacturer modelInfo.Name modelInfo.ModelCode modelInfo.OperatingSystem modelInfo.OsBuild latestRelaseDate
            let! versionedPackagePath = combine4Paths (FileSystem.pathValue destinationRootFolderPath, modelInfo.ModelCode, latestRelaseDate + "-1.0", "Script")
            let! extractedPackagePaths = extractPackageTemplate versionedPackagePath

            let! driversPath = combine2Paths (FileSystem.pathValue versionedPackagePath, "Drivers")
            reportProgress (sprintf "Extracting driver updates to folder '%A'...\n" driversPath) String.Empty String.Empty None true None
            let! driversPath = combine2Paths (FileSystem.pathValue versionedPackagePath, "Drivers")            
            let! existingDriversPath = DirectoryOperations.ensureDirectoryExists true driversPath
            let! extractedUpdates = DriverTool.CreateDriverPackage.extractUpdates (existingDriversPath, manufacturer, updatedInfoDownloadedUpdates)
            let! logDirectory = FileSystem.path packageInstallLogDirectory
            let installScriptResults = DriverTool.CreateDriverPackage.createInstallScripts (extractedUpdates,manufacturer,logDirectory)
            let packageSmsResults = DriverTool.CreateDriverPackage.createPackageDefinitionFiles (extractedUpdates, logDirectory, packagePublisher)

            let! installXmlPath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue versionedPackagePath,"Install.xml"))
            let! existingInstallXmlPath = FileOperations.ensureFileExists installXmlPath
            let! installConfiguration = DriverTool.Library.InstallXml.loadInstallXml existingInstallXmlPath
            let updatedInstallConfiguration = 
                { installConfiguration with 
                    LogDirectory = (DriverTool.Library.Environment.unExpandEnironmentVariables (FileSystem.pathValue logDirectory));
                    LogFileName = toValidDirectoryName (sprintf "%s.log" packageName);
                    PackageName = packageName;
                    PackageVersion = "1.0"
                    PackageRevision = "000"
                    ComputerModel = modelInfo.ModelCode;
                    ComputerSystemFamiliy = modelInfo.Name;
                    ComputerVendor = modelInfo.Manufacturer;
                    OsShortName = modelInfo.OperatingSystem;
                    Publisher = packagePublisher
                }
            let! savedInstallConfiguration = DriverTool.Library.InstallXml.saveInstallXml (existingInstallXmlPath, updatedInstallConfiguration)
            reportProgress (sprintf  "Saved install configuration to '%s'. Value: %A\n" (FileSystem.pathValue existingInstallXmlPath) savedInstallConfiguration) String.Empty String.Empty None true None
            reportProgress ("Creating PackageDefinition.sms...\n") String.Empty String.Empty None true None            
            let! packageDefinitionSmsPath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue versionedPackagePath,"PackageDefinition.sms"))                
            let packageDefinition = getPackageDefinitionFromInstallConfiguration updatedInstallConfiguration
            let! packageDefintionSms = 
                packageDefinition
                |> writePackageDefinitionToFile packageDefinitionSmsPath
            reportProgress (sprintf "Created PackageDefinition.sms: %A" packageDefintionSms) String.Empty String.Empty None true None
            return packageDefintionSms
        }

        
        
