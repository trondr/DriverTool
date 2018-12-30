namespace DriverTool

module Commands =

    open NCmdLiner.Attributes

    [<Commands()>]
    type CommandDefinitions =        
        
        [<Command(Description = "Export Lenovo system update information for specified model to csv file. The update information is extracted from xml files located under web site url https://download.lenovo.com/catalog. Example for model 20EQ (Lenovo Thinkpad P50) and Win10: https://download.lenovo.com/catalog/20EQ_Win10.xml", Summary = "Export Lenovo system update information for specified model to csv file.")>]
        static member ExportRemoteUdateInfo (
                                            [<RequiredCommandParameter(Description = "Path to csv file.", ExampleValue = @"c:\temp\updates.csv", AlternativeName = "f")>] 
                                            csvFileName: string,
                                            [<OptionalCommandParameter(Description = "Overwrite csv file if it allready exists.", ExampleValue = false,DefaultValue = false, AlternativeName = "o")>] 
                                            overWrite : bool,
                                            [<OptionalCommandParameter(Description = "Manufacturer as specified by the Manufacturer property of the Win32_ComputerSystem wmi class instance. Powershell script to extract the manufacturer: $(Get-WmiObject Win32_ComputerSystem|Select-Object Manufacturer).Manufacturer. If manufacturer is not specified the current system manufacturer will be looked up and used.", ExampleValue = @"LENOVO", AlternativeName = "ma", DefaultValue = "")>] 
                                            manufacturer : string,
                                            [<OptionalCommandParameter(Description = "Model code as specified by the first 4 letters of the Model property of the Win32_ComputerSystem wmi class instance. Powershell script to extract the model code for (1)LENOVO: $(Get-WmiObject Win32_ComputerSystem|Select-Object Model).Model.SubString(0,4); (2) Dell: $(Get-WmiObject Win32_ComputerSystem|Select-Object SystemSKUNumber).SystemSKUNumber .If model code is not specified the current system model code will be looked up and used.", ExampleValue = @"20EQ", AlternativeName = "m", DefaultValue = "")>] 
                                            modelCode : string,
                                            [<OptionalCommandParameter(Description = "Operating system code. If operating system code is not specified the current system model code will be looked up and used.", ExampleValue = @"WIN10X64", AlternativeName = "op", DefaultValue = "")>] 
                                            operatingSystemCode : string        
                                            ) : NCmdLiner.Result<int> =                
                CommandProviders.exportRemoteUdateInfo (manufacturer,modelCode, operatingSystemCode, csvFileName, overWrite)                
        
        [<Command(Description = "Export Lenovo system update information for the current system to csv file. The current system is typically a reference machine and the exported info can be used to automate download of updates to be installed on new systems with the same specification. It is required that Lenovo System Update is installed on the system and that Lenovo System Update has been run to install all relevant updates.", Summary="Export Lenovo system update information for the current system to csv file")>]
        static member ExportLocalUdateInfo (
                                            [<RequiredCommandParameter(Description = "Path to csv file.", ExampleValue = @"c:\temp\updates.csv", AlternativeName = "f")>] 
                                            csvFileName: string,
                                            [<OptionalCommandParameter(Description = "Overwrite csv file if it allready exists.", ExampleValue = false,DefaultValue = false, AlternativeName = "o")>] 
                                            overWrite : bool
                                            ) : NCmdLiner.Result<int> =                
                CommandProviders.exportLocalUdateInfo (csvFileName, overWrite)

        [<Command(Description="",Summary="")>]
        static member CreateDriverPackage(
                                         [<RequiredCommandParameter(Description = "Destination folder.",ExampleValue = @"c:\temp\Drivers\SomeModel",AlternativeName = "df")>]
                                            destinationFolder : string,
                                         [<OptionalCommandParameter(Description = "Package publisher company name.", ExampleValue = @"MyCompany", AlternativeName = "pu", DefaultValue = "MyCompany")>] 
                                            packagePublisher : string,
                                         [<OptionalCommandParameter(Description = "Manufacturer as specified by the Manufacturer property of the Win32_ComputerSystem wmi class instance. Powershell script to extract the manufacturer: $(Get-WmiObject Win32_ComputerSystem|Select-Object Manufacturer).Manufacturer. If manufacturer is not specified the current system manufacturer will be looked up and used.", ExampleValue = @"LENOVO", AlternativeName = "ma", DefaultValue = "")>] 
                                            manufacturer : string,
                                         [<OptionalCommandParameter(Description = "SystemFamily as specified by the SystemFamily property of the Win32_ComputerSystem wmi class instance. Powershell script to extract the manufacturer: $(Get-WmiObject Win32_ComputerSystem|Select-Object SystemFamily).SystemFamily. If system family is not specified the current system family will be looked up and used.", ExampleValue = @"ThinkPad P50", AlternativeName = "sf", DefaultValue = "")>] 
                                            systemFamily : string,
                                         [<OptionalCommandParameter(Description = "Model code as specified by the Model property of the Win32_ComputerSystem wmi class instance. Powershell script to extract the model code: $(Get-WmiObject Win32_ComputerSystem|Select-Object Model).Model. If model code is not specified the current system model code will be looked up and used.", ExampleValue = @"20EQ", AlternativeName = "mo", DefaultValue = "")>] 
                                            modelCode : string,
                                         [<OptionalCommandParameter(Description = "Operating system code. If operating system code is not specified the current system operating system code will be looked up and used.", ExampleValue = @"WIN10X64", AlternativeName = "op", DefaultValue = "")>] 
                                            operatingSystemCode : string,

                                         [<OptionalCommandParameter(Description = "Create driver package based on locally installed updates. Use this on a a fully updated reference machine where the vendor specific update utility (Lenovo System Update) has been run and no more updates are available.", ExampleValue = false, AlternativeName = "lu", DefaultValue = false)>]
                                            baseOnLocallyInstalledUpdates : bool
                                         ) : NCmdLiner.Result<int> = 
            CommandProviders.createDriverPackage (packagePublisher,manufacturer, systemFamily, modelCode, operatingSystemCode, destinationFolder,baseOnLocallyInstalledUpdates)
        
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