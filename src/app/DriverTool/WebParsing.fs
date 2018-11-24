namespace DriverTool

module WebParsing =
    open System
    open FSharp.Interop.Dynamic
    open System.Reflection

    let getContentFromWebPage (uri:string)  =
        let ieType = Type.GetTypeFromProgID("InternetExplorer.Application", true)
        let ie = Activator.CreateInstance(ieType)
        ie?Navigate(uri)
        while (ie?ReadyState <> 4) do
            System.Threading.Thread.Sleep(100)
        let ieDocument = ie?Document
        let parentWindow = ieDocument?parentWindow
        parentWindow?execScript("var JSIEVariable = new XMLSerializer().serializeToString(document);", "javascript") |> ignore        
        let parentWindowType = parentWindow?GetType() :> Type
        let obj = parentWindowType.InvokeMember("JSIEVariable", BindingFlags.GetProperty, null, parentWindow, null)        
        let html = obj.ToString()
        ie?Quit()
        html

    let getLenovoSccmPackageDownloadUrl (uri:string) =
        try
            let content = getContentFromWebPage uri
            Result.Ok content
        with
        |ex -> Result.Error ex
