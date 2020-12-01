namespace DriverTool.Library

open DriverTool.Library.F

module Environment=
    
    let isAdministrator () =
        let windowsIdentity = System.Security.Principal.WindowsIdentity.GetCurrent()
        let windowsPrincipal= new System.Security.Principal.WindowsPrincipal(windowsIdentity)
        let administratorRole=System.Security.Principal.WindowsBuiltInRole.Administrator
        windowsPrincipal.IsInRole(administratorRole)
    
    let expandEnvironmentVariables (text:string) =
        System.Environment.ExpandEnvironmentVariables(text)
    
    let replaceValueWithValueName (text:string, value:string, valueName:string) :string =
        if (value.Length > 1) then
            let replacedText =
                text.Replace(value, System.String.Format("%{0}%", valueName))
            replacedText
        else
            text

    let unExpandEnironmentVariables (text:string) :string =
        let environmentVariables = System.Environment.GetEnvironmentVariables()
        let keys = 
            environmentVariables.Keys
            |> Seq.cast<string>
        let values = 
            environmentVariables.Values
            |> Seq.cast<string>
        let valueKeyMap = 
            keys
            |> Seq.zip values
            |> Seq.sortBy (fun (v,k) -> -v.Length) //Make sure to replace the longest parts of the text first
        let mutable unexpandedText = text
        valueKeyMap
        |> Seq.map (fun (v,k) -> 
                unexpandedText <- replaceValueWithValueName (unexpandedText, v, k)
            )
        |>Seq.toArray
        |>ignore
        unexpandedText
    
    let windowsFolder =
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.Windows)

    let nativeSystemFolder =
        let sysNative = System.IO.Path.Combine(windowsFolder,"sysnative")
        let sysNativeCmdExe = System.IO.Path.Combine(sysNative,"cmd.exe")
        if((System.IO.Directory.Exists(sysNative)) && (System.IO.File.Exists(sysNativeCmdExe))) then
            sysNative
        else
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.System)
    
    let is64BitOperatingSystem =
        System.Environment.Is64BitOperatingSystem
    
    let is64BitProcess =
        System.Environment.Is64BitProcess

    let isNativeProcessBit =
        if((is64BitOperatingSystem && is64BitProcess) || (not is64BitOperatingSystem)) then
            true        
        else
            false

    let processBit =
        match (System.IntPtr.Size) with
        |8 -> "64-Bit"
        |4 -> "32-Bit"
        |_ -> (raise (new System.Exception(sprintf "Failed to calculate process bit due to unknown IntPtr size: %i" System.IntPtr.Size )))