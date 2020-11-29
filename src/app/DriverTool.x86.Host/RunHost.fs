namespace DriverTool.x86.Host

module RunHost =
    open System
    open System.ServiceModel
    open System.ServiceModel.Description
    open DriverTool.Library.Logging
    let logger = getLoggerByName "RunHost"

    let runHost () =
        let serviceUri = new Uri("http://localhost:8733/Design_Time_Addresses/DriverTool.x86.Service/ToolService/")
        let host = new ServiceHost(typeof<DriverTool.x86.Service.ToolService>, serviceUri)
        host.AddServiceEndpoint(typeof<DriverTool.x86.Service.IToolService>, new WSHttpBinding(), "") |> ignore
        let smb = new ServiceMetadataBehavior()
        smb.HttpGetEnabled <- true
        host.Description.Behaviors.Add(smb)
        host.Open()
        Console.WriteLine("Service is hosted at " + serviceUri.ToString());
        let cancelationTokenSource = new System.Threading.CancellationTokenSource()
        cancelationTokenSource.Token.WaitHandle.WaitOne() |>ignore 
        0