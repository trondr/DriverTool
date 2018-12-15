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

    let regKeyExists regKeyPath =
        let (regHive, subKeyPath) = parseRegKeyPath regKeyPath
        use regKey = regHive.OpenSubKey(subKeyPath)
        match regKey with
        |null -> false
        |_ -> true

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
    
    let openRegKeyUnsafe (regKeyPath:string, writeable:bool) =
        nullGuard regKeyPath "regKeyPath"
        logger.Info(String.Format("Opening registry key: [{0}] (writeable: {1})", regKeyPath, writeable.ToString()))
        let (regHive,subPath) = parseRegKeyPath regKeyPath
        regHive.OpenSubKey(subPath,writeable)
    
    let openRegKey (regKeyPath:string, writeable:bool) =
        tryCatch openRegKeyUnsafe  (regKeyPath, writeable)

    let rec getRegistrySubKeyPaths (regKeyPath:string) recursive : seq<string> =       
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
    
    let regValueExists (regKeyPath:string) valueName =
        use regKey = openRegKeyUnsafe (regKeyPath, false)
        let value = regKey.GetValue(valueName)
        match value with
        |null -> false
        |_ -> true
    
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
        