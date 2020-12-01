namespace DriverTool.x86.Host
        
    module CommandDefinitions =

        open NCmdLiner.Attributes
        open DriverTool.x86.Host.RunHost

        [<Commands()>]
        type CommandDefinitions =        
            
            [<Command(Description = "Run DriverTool x86 host, listening for requests from main DriverTool process.", Summary = "Run DriverTool x86 host.")>]
            static member RunHost () =
                    NCmdLiner.Result.Ok (runHost ())
        