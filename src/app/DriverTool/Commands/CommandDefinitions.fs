namespace DriverTool

module Commands =
    open NCmdLiner
    open NCmdLiner.Attributes
    [<Commands()>]
    type CommandDefinitions =        
        [<Command(Description = "Export Lenovo system update information for specified model to csv file. The update information is extracted from xml files located under web site url https://download.lenovo.com/catalog. Example for model 20EQ (Lenovo Thinkpad P50) and Win10: https://download.lenovo.com/catalog/20EQ_Win10.xml", Summary = "Export Lenovo system update information for specified model to csv file.")>]
        static member ExportRemoteUdateInfo (
                                            [<RequiredCommandParameter(Description = "Path to csv file.", ExampleValue = @"c:\temp\updates.csv", AlternativeName = "f")>] csvFileName: string,
                                            [<OptionalCommandParameter(Description = "Overwrite csv file if it allready exists.", ExampleValue = false,DefaultValue = false, AlternativeName = "o")>] overWrite : bool,
                                            [<OptionalCommandParameter(Description = "Model code as specified by the first 4 letters of the Model property of the Win32_ComputerSystem wmi class instance. Powershell script to extract the model code: $(Get-WmiObject Win32_ComputerSystem|Select-Object Model).Model.SubString(0,4). If model code is not specified the current system model code will be looked up and used.", ExampleValue = @"20EQ", AlternativeName = "m", DefaultValue = "")>] modelCode : string,
                                            [<OptionalCommandParameter(Description = "Operating system code Win7|Win8|Win10. If operating system code is not specified the current system model code will be looked up and used.", ExampleValue = @"Win10", AlternativeName = "op", DefaultValue = "Win10")>] operatingSystemCode : string        
                                            ) : Result<int> =
                System.Console.WriteLine("")
                Result.Ok(0)
