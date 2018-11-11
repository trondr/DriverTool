﻿namespace DriverTool

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
                                            [<OptionalCommandParameter(Description = "Model code as specified by the first 4 letters of the Model property of the Win32_ComputerSystem wmi class instance. Powershell script to extract the model code: $(Get-WmiObject Win32_ComputerSystem|Select-Object Model).Model.SubString(0,4). If model code is not specified the current system model code will be looked up and used.", ExampleValue = @"20EQ", AlternativeName = "m", DefaultValue = "")>] 
                                            modelCode : string,
                                            [<OptionalCommandParameter(Description = "Operating system code Win7|Win8|Win10. If operating system code is not specified the current system model code will be looked up and used.", ExampleValue = @"Win10", AlternativeName = "op", DefaultValue = "Win10")>] 
                                            operatingSystemCode : string        
                                            ) : NCmdLiner.Result<int> =                
                CommandProviders.exportRemoteUdateInfo (modelCode, operatingSystemCode, csvFileName, overWrite)                
        
        [<Command(Description="",Summary="")>]
        static member CreateDriverPackage(
                                         [<RequiredCommandParameter(Description = "Destination folder.",ExampleValue = @"c:\temp\Drivers\SomeModel",AlternativeName = "df")>]
                                            destinationFolder : string,
                                         [<OptionalCommandParameter(Description = "Package publisher company name.", ExampleValue = @"MyCompany", AlternativeName = "m", DefaultValue = "MyCompany")>] 
                                            packagePublisher : string,
                                         [<OptionalCommandParameter(Description = "Manufacturer as specified by the Manufacturer property of the Win32_ComputerSystem wmi class instance. Powershell script to extract the manufacturer: $(Get-WmiObject Win32_ComputerSystem|Select-Object Manufacturer).Manufacturer. If manufacturer is not specified the current system manufacturer will be looked up and used.", ExampleValue = @"LENOVO", AlternativeName = "m", DefaultValue = "")>] 
                                            manufacturer : string,
                                        [<OptionalCommandParameter(Description = "SystemFamily as specified by the SystemFamily property of the Win32_ComputerSystem wmi class instance. Powershell script to extract the manufacturer: $(Get-WmiObject Win32_ComputerSystem|Select-Object SystemFamily).SystemFamily. If system family is not specified the current system family will be looked up and used.", ExampleValue = @"ThinkPad P50", AlternativeName = "m", DefaultValue = "")>] 
                                            systemFamily : string,
                                         [<OptionalCommandParameter(Description = "Model code as specified by the Model property of the Win32_ComputerSystem wmi class instance. Powershell script to extract the model code: $(Get-WmiObject Win32_ComputerSystem|Select-Object Model).Model. If model code is not specified the current system model code will be looked up and used.", ExampleValue = @"20EQ", AlternativeName = "m", DefaultValue = "")>] 
                                            modelCode : string,
                                         [<OptionalCommandParameter(Description = "Operating system code. If operating system code is not specified the current system operating system code will be looked up and used.", ExampleValue = @"Win10X64", AlternativeName = "op", DefaultValue = "")>] 
                                            operatingSystemCode : string,
                                         [<OptionalCommandParameter(Description = "Log directory where install logs will be written.", ExampleValue = @"%public%\Logs", AlternativeName = "ld", DefaultValue = "%public%\Logs")>] 
                                            logDirectory : string
                                         ) : NCmdLiner.Result<int> = 
            CommandProviders.createDriverPackage (packagePublisher,manufacturer, systemFamily, modelCode, operatingSystemCode, destinationFolder, logDirectory)
