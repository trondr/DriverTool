namespace DriverTool

module RegistryOperations =
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