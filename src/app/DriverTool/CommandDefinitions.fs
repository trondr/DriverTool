namespace DriverTool

module Commands =

    open NCmdLiner.Attributes

    [<Commands()>]
    type CommandDefinitions =        
        
        [<Command(Description = "Export remote update information for specified model to csv file. The update information is extracted from xml files located on the respective vendor web sites. Example for Lenovo: https://download.lenovo.com/catalog. Example for model 20EQ (Lenovo Thinkpad P50) and Win10: https://download.lenovo.com/catalog/20EQ_Win10.xml. The remote update information typically contains all available updates also including those allready installed.", Summary = "Export remote update information for specified model to csv file.")>]
        static member ExportRemoteUdateInfo (
                                            [<RequiredCommandParameter(Description = "Path to csv file.", ExampleValue = @"c:\temp\updates.csv", AlternativeName = "f")>] 
                                            csvFileName: string,
                                            [<OptionalCommandParameter(Description = "Overwrite csv file if it allready exists.", ExampleValue = false,DefaultValue = false, AlternativeName = "o")>] 
                                            overWrite : bool,
                                            [<OptionalCommandParameter(Description = "Manufacturer as specified by the Manufacturer property of the Win32_ComputerSystem wmi class instance. Powershell script to extract the manufacturer: $(Get-WmiObject Win32_ComputerSystem|Select-Object Manufacturer).Manufacturer. If manufacturer is not specified the current system manufacturer will be looked up and used.", ExampleValue = @"LENOVO", AlternativeName = "ma", DefaultValue = "")>] 
                                            manufacturer : string,
                                            [<OptionalCommandParameter(Description = "Model code. Powershell script to extract the model code for (1)LENOVO: $(Get-WmiObject Win32_ComputerSystem|Select-Object Model).Model.SubString(0,4); (2) Dell: $(Get-WmiObject Win32_ComputerSystem|Select-Object SystemSKUNumber).SystemSKUNumber; (3) HP: (Get-WmiObject -Class MS_SystemInformation -Namespace \"root\\WMI\"| Select-Object BaseBoardProduct).BaseBoardProduct. If model code is not specified the current system model code will be looked up and used.", ExampleValue = @"20EQ", AlternativeName = "m", DefaultValue = "")>] 
                                            modelCode : string,
                                            [<OptionalCommandParameter(Description = "Operating system code. If operating system code is not specified the current system model code will be looked up and used.", ExampleValue = @"WIN10X64", AlternativeName = "op", DefaultValue = "")>] 
                                            operatingSystemCode : string,
                                            [<OptionalCommandParameter(Description = "Exclude updates where title or category match any of the specified regular expression patterns.", ExampleValue=[|"Software";"BIOS";"Firmware"|], DefaultValue=[||], AlternativeName = "xu")>]
                                            excludeUpdatePatterns : string[]
                                            ) : NCmdLiner.Result<int> =                
                CommandProviders.exportRemoteUdateInfo (manufacturer,modelCode, operatingSystemCode, csvFileName, overWrite, excludeUpdatePatterns)                
        
        [<Command(Description = "Export local update information for the current system to csv file. The current system is typically a reference machine and the exported info can be used to automate download of necessary updates to be installed on new systems with the same specification. It is required that vendor specific update utility (such as Lenovo System Update) is installed on the system and has been run to install all relevant updates. Currently only Lenovo System update is supported.", Summary="Export local update information for the current system to csv file.")>]
        static member ExportLocalUdateInfo (
                                            [<RequiredCommandParameter(Description = "Path to csv file.", ExampleValue = @"c:\temp\updates.csv", AlternativeName = "f")>] 
                                            csvFileName: string,
                                            [<OptionalCommandParameter(Description = "Overwrite csv file if it allready exists.", ExampleValue = false,DefaultValue = false, AlternativeName = "o")>] 
                                            overWrite : bool,
                                            [<OptionalCommandParameter(Description = "Exclude updates where title or category match any of the specified regular expression patterns.", ExampleValue=[|"Software";"BIOS";"Firmware"|], DefaultValue=[||], AlternativeName = "xu")>]
                                            excludeUpdatePatterns : string[]
                                            ) : NCmdLiner.Result<int> =                
                CommandProviders.exportLocalUdateInfo (csvFileName, overWrite, excludeUpdatePatterns)

        [<Command(Description="Create driver package for given manufacturer and model. Currently Lenovo is fully supported. Support for Dell and HP is partial and includes creating driver package with all possible updates (needed and and unneeded alike) so for Dell and HP there is more requirement for manual decsion on what to include or not after package has been created. If manufacturer or model or operating system is not given the respective values are automatically looked up for current system.",Summary="Create driver package for given manufacturer and model and operating system.")>]
        static member CreateDriverPackage(
                                         [<RequiredCommandParameter(Description = "Destination folder.",ExampleValue = @"c:\temp\Drivers\SomeModel",AlternativeName = "df")>]
                                            destinationFolder : string,
                                         [<OptionalCommandParameter(Description = "Package publisher company name. This should be the name of the company of whoever creates the driver package.", ExampleValue = @"MyCompany", AlternativeName = "pu", DefaultValue = "MyCompany")>] 
                                            packagePublisher : string,
                                         [<OptionalCommandParameter(Description = "Manufacturer as specified by the Manufacturer property of the Win32_ComputerSystem wmi class instance. Powershell script to extract the manufacturer: $(Get-WmiObject Win32_ComputerSystem|Select-Object Manufacturer).Manufacturer. If manufacturer is not specified the current system manufacturer will be looked up and used.", ExampleValue = @"LENOVO", AlternativeName = "ma", DefaultValue = "")>] 
                                            manufacturer : string,
                                         [<OptionalCommandParameter(Description = "SystemFamily as specified by the SystemFamily property of the Win32_ComputerSystem wmi class instance. Powershell script to extract the system family: $(Get-WmiObject Win32_ComputerSystem|Select-Object SystemFamily).SystemFamily. If system family is not specified the current system family will be looked up and used.", ExampleValue = @"ThinkPad P50", AlternativeName = "sf", DefaultValue = "")>] 
                                            systemFamily : string,
                                         [<OptionalCommandParameter(Description = "Model code. Powershell script to extract the model code for (1)LENOVO: $(Get-WmiObject Win32_ComputerSystem|Select-Object Model).Model.SubString(0,4); (2) Dell: $(Get-WmiObject Win32_ComputerSystem|Select-Object SystemSKUNumber).SystemSKUNumber; (3) HP: (Get-WmiObject -Class MS_SystemInformation -Namespace \"root\\WMI\"| Select-Object BaseBoardProduct).BaseBoardProduct. If model code is not specified the current system model code will be looked up and used.", ExampleValue = @"20EQ", AlternativeName = "mo", DefaultValue = "")>] 
                                            modelCode : string,
                                         [<OptionalCommandParameter(Description = "Operating system code. If operating system code is not specified the current system operating system code will be looked up and used.", ExampleValue = @"WIN10X64", AlternativeName = "op", DefaultValue = "")>] 
                                            operatingSystemCode : string,
                                         [<OptionalCommandParameter(Description = "Create driver package based on locally installed updates. Use this on a a fully updated reference machine where the vendor specific update utility (Lenovo System Update) has been run and no more updates are available. This option is currently not supported on Dell and HP.", ExampleValue = false, AlternativeName = "lu", DefaultValue = false)>]
                                            baseOnLocallyInstalledUpdates : bool,
                                         [<OptionalCommandParameter(Description = "Exclude updates where title or category match any of the specified regular expression patterns.", ExampleValue=[|"Software";"BIOS";"Firmware"|], DefaultValue=[||], AlternativeName = "xu")>]
                                            excludeUpdatePatterns : string[],
                                         [<OptionalCommandParameter(Description = "A short name describing the content of the package. Example: 'Software', 'Firmware', 'BIOS'. The package type name will be used in the package name.", ExampleValue = @"Drivers", AlternativeName = "ptn", DefaultValue = "Drivers")>] 
                                            packageTypeName : string,
                                         [<OptionalCommandParameter(Description = "Exclude Sccm package from the created package. Typically you set this to true if you want only non-driver related updates, such as BIOS or firmware, to be part of the package.", ExampleValue = false, AlternativeName = "exs", DefaultValue = false)>] 
                                            excludeSccmPackage : bool,
                                         [<OptionalCommandParameter(Description = "Do not download Sccm Package. Used as a workaround for web scraping gone wrong when Lenovo has changed the web design. Both the /sccmPackageInstaller and the /sccmPackageReadme parameters must be defined if /doNotDownloadSccmPackage=True.", ExampleValue = false, AlternativeName = "dnd", DefaultValue = false)>] 
                                         doNotDownloadSccmPackage : bool,
                                         [<OptionalCommandParameter(Description = "Specify sccm package installer to override automatic download of Sccm package. Used as a workaround for web scraping gone wrong when Lenovo has changed the web design. The sccm package installer spesified must be be downloaded manually and saved to the DriverToolCache as specified in the application configuration file (DownloadCacheDirectoryPath).", ExampleValue = "tp_x1carbon_mt20qd-20qe-x1yoga_mt20qf-20qg_w1064_1809_201910.exe", AlternativeName = "spi", DefaultValue = "")>] 
                                            sccmPackageInstaller : string,
                                         [<OptionalCommandParameter(Description = "Specify sccm package readme to override automatic download of Sccm package. Used as a workaround for web scraping gone wrong when Lenovo has changed the web design. The sccm package readme spesified must be be downloaded manually and saved to the DriverToolCache as specified in the application configuration file (DownloadCacheDirectoryPath).", ExampleValue = "tp_x1carbon_mt20qd-20qe-x1yoga_mt20qf-20qg_w1064_1809_201910.txt", AlternativeName = "spr", DefaultValue = "")>] 
                                            sccmPackageReadme : string,
                                         [<OptionalCommandParameter(Description = "Specify sccm package relase date.", ExampleValue = "2019-10-01", AlternativeName = "sprd", DefaultValue = "2001-01-01")>] 
                                            sccmPackageReleased : string
                                         ) : NCmdLiner.Result<int> = 
            CommandProviders.createDriverPackage (packagePublisher, manufacturer, systemFamily, modelCode, operatingSystemCode, destinationFolder, baseOnLocallyInstalledUpdates, excludeUpdatePatterns, packageTypeName, excludeSccmPackage, doNotDownloadSccmPackage,sccmPackageInstaller,sccmPackageReadme,sccmPackageReleased)
        
        [<Command(Description="Install driver package. This command looks for the .\Drivers sub folder. If the .\Drivers does not exist the command looks for the \Drivers.zip file and extracts it to .\Drivers folder. The command then executes each DT-Install-Package.cmd in any sub folders below the .\Drivers folder.",Summary="Install driver package")>]
        static member InstallDriverPackage(
                                            [<RequiredCommandParameter(Description = "Driver package folder path. Below this path there should be a .\Drivers sub folder or Drivers.zip.",ExampleValue = @"c:\temp\Drivers\SomeModel",AlternativeName = "dpp")>]
                                            driverPackagePath :string
                      ) : NCmdLiner.Result<int> =
            CommandProviders.installDriverPackage (driverPackagePath)
        
        [<Command(Description="Uninstall driver package. This command looks for the .\Drivers sub folder. If the .\Drivers does not exist the command looks for the \Drivers.zip file and extracts it to .\Drivers folder. The command then executes each DT-UnInstall-Package.cmd in any sub folders below the .\Drivers folder.",Summary="Uninstall driver package.")>]
        static member UnInstallDriverPackage(
                                            [<RequiredCommandParameter(Description = "Driver package folder path. Below this path there should be a .\Drivers sub folder or a Drivers.zip.",ExampleValue = @"c:\temp\Drivers\SomeModel",AlternativeName = "dpp")>]
                                            driverPackagePath :string
                      ) : NCmdLiner.Result<int> =
            CommandProviders.unInstallDriverPackage (driverPackagePath)

        [<Command(Description="Compress the Drivers folder to Drivers.zip",Summary="Compress Drivers folder.")>]
        static member CompressDriverPackage(
                                            [<RequiredCommandParameter(Description = "Driver package folder path. Below this path there should be a .\Drivers sub folder.",ExampleValue = @"c:\temp\Drivers\SomeModel",AlternativeName = "dpp")>]
                                            driverPackagePath :string
                      ) : NCmdLiner.Result<int> =
            CommandProviders.compressDriverPackage (driverPackagePath)

        [<Command(Description="Decompress Drivers.zip to \Drivers folder.",Summary="Decompress Drivers.zip.")>]
        static member DecompressDriverPackage(
                                                [<RequiredCommandParameter(Description = "Driver package folder path. Below this path there should be a .\Drivers.zip file.",ExampleValue = @"c:\temp\Drivers\SomeModel",AlternativeName = "dpp")>]
                                                driverPackagePath :string
                      ) : NCmdLiner.Result<int> =
            CommandProviders.decompressDriverPackage (driverPackagePath)

#if DEBUG
        [<Command(Description="Download Lenovo Update Package Xmls for all models")>]
        static member DownloadLenovUpdatePackageXmls() : NCmdLiner.Result<int> =
            CommandProviders.downloadLenovoUpdatePackageXmls()        
#endif