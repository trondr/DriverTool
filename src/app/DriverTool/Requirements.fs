namespace DriverTool

module Requirements =
    
    open DriverTool.Library.Environment

    let isAdministrator () =
        let windowsIdentity = System.Security.Principal.WindowsIdentity.GetCurrent()
        let windowsPrincipal= new System.Security.Principal.WindowsPrincipal(windowsIdentity)
        let administratorRole=System.Security.Principal.WindowsBuiltInRole.Administrator
        windowsPrincipal.IsInRole(administratorRole)        

    let assertIsAdministrator (message) =
        let isAdministrator = isAdministrator()
        match isAdministrator with
        |true -> Result.Ok true
        |false-> Result.Error (new System.Exception(message))

    let assertIsRunningNativeProcess message =
        match(isNativeProcessBit) with
        |true -> Result.Ok true
        |false -> Result.Error (new System.Exception(message))