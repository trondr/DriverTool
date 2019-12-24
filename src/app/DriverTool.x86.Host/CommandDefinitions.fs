namespace DriverTool.x86.Host
        
    module CommandDefinitions =

        open NCmdLiner.Attributes

        [<Commands()>]
        type CommandDefinitions =        
            
            [<Command(Description = "Example command 1", Summary = "Export remote update information for specified model to csv file.")>]
            static member ExampleExportCommand1 (
                                                [<RequiredCommandParameter(Description = "Path to csv file.", ExampleValue = @"c:\temp\updates.csv", AlternativeName = "f")>] 
                                                csvFileName: string,
                                                [<OptionalCommandParameter(Description = "Overwrite csv file if it allready exists.", ExampleValue = false,DefaultValue = false, AlternativeName = "o")>] 
                                                overWrite : bool                                            
                                                ) : NCmdLiner.Result<int> =                
                    printfn "ExampleExportCommand 1 exporting file '%s' with overwrite=%b" csvFileName overWrite
                    NCmdLiner.Result.Ok 0
        