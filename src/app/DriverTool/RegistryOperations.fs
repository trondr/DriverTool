namespace DriverTool

module RegistryOperations =
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
        if (logger.IsDebugEnabled) then logger.Debug(String.Format("Opening registry key: [{0}] (writeable: {1})", regKeyPath, writeable.ToString()))
        let (regHive,subPath) = parseRegKeyPath regKeyPath
        regHive.OpenSubKey(subPath,writeable)
    
    let openRegKey (regKeyPath:string, writeable:bool) =
        tryCatch openRegKeyUnsafe  (regKeyPath, writeable)
    
    type OpenRegKeyFunc = (string*bool) -> Result<RegistryKey,Exception>

    let regKeyExistsBase (openRegKeyFunc:OpenRegKeyFunc) (regKeyPath:string) loggerIsEnabled logWrite =
        let regKeyResult = openRegKeyFunc (regKeyPath, false)
        match regKeyResult with        
        |Ok rk ->
            use regKey = rk            
            match regKey with
            |null -> false
            |_ -> true
        |Error ex ->
            if (loggerIsEnabled) then logWrite(String.Format("Failed to open registry key [{0}] due to: {1}", regKeyPath, ex.Message))
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
        |Error ex ->
            if (loggerIsEnabled) then logWrite(String.Format("Failed to open registry key [{0}] due to: {1}", regKeyPath, ex.Message))
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
            regHive.DeleteSubKey(subKeyPath)
     
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
    
    let regValueIs (regKeyPath:string) valueName value =
        use regKey = openRegKeyUnsafe (regKeyPath, false)
        let actualValue = regKey.GetValue(valueName)
        if actualValue.Equals(value) then
            true
        else
            false

    let setRegValue (regKeyPath:string) valueName value = 
        use regKey = openRegKeyUnsafe (regKeyPath, true)                
        regKey.SetValue(valueName,value)
        