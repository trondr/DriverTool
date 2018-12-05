namespace DriverTool

module BitLockerOperations=
        
    let systemFolder =
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.System)
    
    let schtasksExe =
        System.IO.Path.Combine(systemFolder,"schtasks.exe")
    
    let manageBdeExe =
        System.IO.Path.Combine(systemFolder,"manage-bde.exe")

    let resumeBitLockerTaskName = 
        "DriverTool Resume BitLocker Protection"
    
    let suspendBitLockerProtection =
        result{
            let! exitCode = ProcessOperations.startConsoleProcess (manageBdeExe,"-protectors -disable C:",systemFolder,null,false)
            //To do implement scheduled task to resume bitlocker
            return exitCode
        }        

    