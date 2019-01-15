namespace DriverTool.Tests

module Init =
    type ThisTestAssembly = { Empty:unit;}

module TestCategory=
    [<Literal>]
    let UnitTests = "UnitTests"
    [<Literal>]
    let IntegrationTests = "IntegrationTests"
    [<Literal>]
    let ManualTests = "IntegrationTests"