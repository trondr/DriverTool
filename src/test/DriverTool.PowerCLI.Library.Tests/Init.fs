namespace DriverTool.PowerCLI.Library.Tests

[<AutoOpen>]
module Init =
    type ThisTestAssembly = { Empty:unit;}
    open Serilog

    let getConsoleDebugLogger (loggerName:string) =
        let log = Serilog.LoggerConfiguration()
                    .MinimumLevel.Is(Serilog.Events.LogEventLevel.Debug)
                    .Enrich.FromLogContext()                      
                    .WriteTo.Console()
                    .CreateLogger()
        Serilog.Log.Logger <- log
        Common.Logging.LogManager.Adapter <- (new Common.Logging.Serilog.SerilogFactoryAdapter())
        Common.Logging.LogManager.GetLogger(loggerName)      

[<AutoOpen>]
module TestCategory=
    [<Literal>]
    let UnitTests = "UnitTests"
    [<Literal>]
    let IntegrationTests = "IntegrationTests"
    [<Literal>]
    let ManualTests = "ManualTests"