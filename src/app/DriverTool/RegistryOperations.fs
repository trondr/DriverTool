namespace DriverTool

module RegistryOperations =
    open DriverTool.Logging
    let logger = Logging.getLoggerByName("RegistryOperations")
    open System
    open Microsoft.Win32;
        
    let toRegHive regHiveName =
        match regHiveName with
        |"HKCU"|"HKEY_CURRENT_USER" -> Registry.CurrentUser
        |"HKLM"|"HKEY_LOCAL_MACHINE" -> Registry.LocalMachine
        |"HKCR"|"HKEY_CLASSES_ROOT" -> Registry.ClassesRoot        
        |"HKCC"|"HKEY_CURRENT_CONFIG" -> Registry.CurrentConfig
        |"HKU"|"HKEY_USERS" -> Registry.Users
        |_ -> raise (new Exception("Unknown registry hive: " + regHiveName))

    let parseRegKeyPath regKeyPath =
        match regKeyPath with
        |Regex @"(HKLM|HKEY_LOCAL_MACHINE|HKCU|HKEY_CURRENT_USER|HKCR|HKEY_CLASSES_ROOT|HKU|HKEY_USERS|HKCC|HKEY_CURRENT_CONFIG)\\(.*)$" [regHiveName;subKeyPath] -> (toRegHive regHiveName,subKeyPath)
        |_ -> raise (new Exception("Invalid registry key path: " + regKeyPath))

    let openRegKeyUnsafe (regKeyPath:string, writeable:bool) =
        nullGuard regKeyPath "regKeyPath"
        logger.Debug(new Msg(fun m ->m.Invoke((sprintf  "Opening registry key: [%s] (writeable: %b)" regKeyPath writeable))|>ignore))
        let (regHive,subPath) = parseRegKeyPath regKeyPath
        regHive.OpenSubKey(subPath,writeable)
    
    let openRegKey (regKeyPath:string, writeable:bool) =
        tryCatch openRegKeyUnsafe  (regKeyPath, writeable)
    
    let openRegKeyOptionalBase (regKeyPath:string, writeable:bool) loggerIsEnabled logWrite =
        let regKeyResult = openRegKey (regKeyPath,writeable)
        match regKeyResult with
        |Ok regKey ->
            match regKey with
            |null -> None
            |_ -> Some regKey
        |Result.Error ex ->
            if(loggerIsEnabled) then logWrite(sprintf "Failed to open registry key [%s] due to: %s" regKeyPath ex.Message)
            None
    
    let openRegKeyOptional (regKeyPath:string, writeable:bool) =
        openRegKeyOptionalBase (regKeyPath, writeable) logger.IsDebugEnabled logger.Debug

    type OpenRegKeyFunc = (string*bool) -> Result<RegistryKey,Exception>

    let regKeyExistsBase (openRegKeyFunc:OpenRegKeyFunc) (regKeyPath:string) loggerIsEnabled logWrite =
        let regKeyResult = openRegKeyFunc (regKeyPath, false)
        match regKeyResult with        
        |Ok rk ->
            use regKey = rk            
            match regKey with
            |null -> false
            |_ -> true
        |Result.Error ex ->
            if (loggerIsEnabled) then logWrite(sprintf "Failed to open registry key [%s] due to: %s" regKeyPath ex.Message)
            false

    let regKeyExists (regKeyPath:string) =
        regKeyExistsBase openRegKey (regKeyPath) logger.IsDebugEnabled logger.Debug
    
    let getRegkeyValue (regKey:RegistryKey, valueName) =
        nullGuard regKey "regKey"
        let value = regKey.GetValue(valueName)
        match (value) with
        |null -> None
        |_ -> Some(value)

    type GetRegKeyValueFunc = (RegistryKey*string) -> option<obj>

    let regValueExistsBase (openRegKeyFunc:OpenRegKeyFunc) (getRegkeyValueFunc:GetRegKeyValueFunc) (regKeyPath:string) valueName loggerIsEnabled logWrite =
        let regKeyResult = openRegKeyFunc  (regKeyPath, false)
        match regKeyResult with        
        |Ok rk ->
            match rk with
            |null -> false
            |_ ->
                use regKey = rk
                let value = getRegkeyValueFunc (regKey,valueName)
                match value with
                |Some _ -> true
                |None -> false
        |Result.Error ex ->
            if (loggerIsEnabled) then logWrite(new Msg(fun m ->m.Invoke((sprintf "Failed to open registry key [%s] due to: %s" regKeyPath ex.Message))|>ignore))
            false

    let regValueExists (regKeyPath:string) valueName =
        regValueExistsBase openRegKey getRegkeyValue regKeyPath valueName logger.IsDebugEnabled logger.Debug

    let deleteRegKey regKeyPath =
        let (regHive, subKeyPath) = parseRegKeyPath regKeyPath
        use regKey = regHive.OpenSubKey(subKeyPath)
        match regKey with
        |null -> ()
        |_ ->
            regKey.Dispose()
            regHive.DeleteSubKeyTree(subKeyPath)
     
    let createRegKey regKeyPath =
        let (regHive, subKeyPath) = parseRegKeyPath regKeyPath
        let regKey = regHive.OpenSubKey(subKeyPath)
        match regKey with
        |null -> regHive.CreateSubKey(subKeyPath)
        |_ -> regKey

    let rec getRegistrySubKeyPaths (regKeyPath:string) recursive : seq<string> =       
        if(not (regKeyExists regKeyPath)) then
            Seq.empty<string>
        else
            use regKey = openRegKeyUnsafe (regKeyPath, false)
            let subKeyNames = regKey.GetSubKeyNames()
            let subKeyPaths = 
                subKeyNames
                |>Seq.map(fun n -> 
                            let subKeyPath = regKeyPath + @"\" + n
                            subKeyPath
                         )
                |>Seq.toArray

            seq {
                for keyPath in subKeyPaths do  
                    yield keyPath
                    if recursive then
                        for childSubKeyPath in (getRegistrySubKeyPaths keyPath recursive) do
                            yield childSubKeyPath
            }
    
    type OpenRegKeyOptionalFunc = (string*bool) -> option<RegistryKey>

    let getRegValueBase (openRegKeyFunc:OpenRegKeyOptionalFunc) (getRegkeyValueFunc:GetRegKeyValueFunc) regKeyPath valueName =
        let regKeyResult = openRegKeyFunc (regKeyPath, false)
        match regKeyResult with
        |Some regKey ->
            getRegkeyValueFunc (regKey, valueName)
        |None -> None
    /// <summary>
    /// Get reg value.
    /// </summary>
    /// <param name="regKeyPath">Example: "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion"</param>
    /// <param name="valueName">Example: "ReleaseId"</param>
    let getRegValue regKeyPath valueName =
        getRegValueBase openRegKeyOptional getRegkeyValue regKeyPath valueName

    let regValueIsBase (openRegKey:OpenRegKeyOptionalFunc) (getRegValueFunc:GetRegKeyValueFunc) regKeyPath valueName value =
        let regKeyOptional = openRegKey (regKeyPath, false)
        match regKeyOptional with
        |Some rk -> 
            use regKey = rk
            let actualValue = getRegValueFunc (regKey, valueName)
            match actualValue with
            |Some v ->
                if v.Equals(value) then
                    true
                else
                    false
            |None -> 
                false
        |None ->            
            false

    let regValueIs (regKeyPath:string) valueName value =
        regValueIsBase openRegKeyOptional getRegkeyValue regKeyPath valueName value

    let setRegValue (regKeyPath:string) valueName value = 
        use regKey = openRegKeyUnsafe (regKeyPath, true)                
        regKey.SetValue(valueName,value)
        