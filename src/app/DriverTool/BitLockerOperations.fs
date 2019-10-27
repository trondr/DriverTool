namespace DriverTool

module BitLockerOperations=
    open System
    open DriverTool.Environment
    open System.Management
    let logger = Logging.getLoggerByName("BitLockerOperations")
    
    let isBitLockerEnabled () =
        let nameSpace = @"\\.\root\CIMv2\Security\MicrosoftVolumeEncryption"
        let className = "Win32_EncryptableVolume"
        let managementScope = new ManagementScope(nameSpace)  
        let systemDrive = System.Environment.GetEnvironmentVariable("SystemDrive").ToUpper()
        let queryString = (sprintf "SELECT * FROM %s WHERE DriveLetter='%s'" className systemDrive)
        let query = new ObjectQuery(queryString)
        use searcher = new ManagementObjectSearcher(managementScope, query)
        use collection = searcher.Get()
        use bitLockerVolume:ManagementObject = 
            collection
            |> Seq.cast
            |> Seq.head
        let protectionStatusObject = bitLockerVolume.GetPropertyValue("ProtectionStatus")
        let protectionStatus = Convert.ToUInt32(protectionStatusObject)
        match protectionStatus with
        | 1u -> true
        | _ -> false

    let schtasksExe =
        System.IO.Path.Combine(nativeSystemFolder,"schtasks.exe")
    
    let manageBdeExe =
        System.IO.Path.Combine(nativeSystemFolder,"manage-bde.exe")

    let resumeBitLockerTaskName = 
        "DriverTool Resume BitLocker Protection"
    
    let resumeBitLockerCmdFileName =
        "DriverTool Resume BitLocker Protection.cmd"
    
    let resumeBitLockerTaskXmlFileName =
        "DriverTool Resume BitLocker Protection.xml"

    let driverToolProgramDataFolder =
        System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData),"tr.DriverTool")
    
    let installResumeBitLockerTaskXmlFile (destinationFolderPath:FileSystem.Path,resumeBitLockerCmdFilePath:FileSystem.Path,taskName:string) =
        result{
            let timeStamp = DateTime.Now.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffff")
            let xmlFormatString =
                """<?xml version="1.0" encoding="UTF-16"?>
<Task version="1.3" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
    <RegistrationInfo>
    <Date>{0}</Date>
    <Author>DriverTool\DriverTool</Author>
    <URI>\{1}</URI>
    </RegistrationInfo>
    <Triggers>
    <BootTrigger>
        <Enabled>true</Enabled>
    </BootTrigger>
    </Triggers>
    <Principals>
    <Principal id="Author">
        <UserId>S-1-5-18</UserId>
        <RunLevel>HighestAvailable</RunLevel>
    </Principal>
    </Principals>
    <Settings>
    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
    <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
    <StopIfGoingOnBatteries>true</StopIfGoingOnBatteries>
    <AllowHardTerminate>true</AllowHardTerminate>
    <StartWhenAvailable>true</StartWhenAvailable>
    <RunOnlyIfNetworkAvailable>false</RunOnlyIfNetworkAvailable>
    <IdleSettings>
        <StopOnIdleEnd>true</StopOnIdleEnd>
        <RestartOnIdle>false</RestartOnIdle>
    </IdleSettings>
    <AllowStartOnDemand>true</AllowStartOnDemand>
    <Enabled>true</Enabled>
    <Hidden>false</Hidden>
    <RunOnlyIfIdle>false</RunOnlyIfIdle>
    <DisallowStartOnRemoteAppSession>false</DisallowStartOnRemoteAppSession>
    <UseUnifiedSchedulingEngine>true</UseUnifiedSchedulingEngine>
    <WakeToRun>false</WakeToRun>
    <ExecutionTimeLimit>PT72H</ExecutionTimeLimit>
    <Priority>7</Priority>
    </Settings>
    <Actions Context="Author">
    <Exec>
        <Command>{2}</Command>
        <Arguments></Arguments>
    </Exec>
    </Actions>
</Task>    
            """
            let! existingDestinationFolderPath = DirectoryOperations.ensureDirectoryExists true destinationFolderPath
            let! resumeBitLockerTaskXmlFilePath = 
                FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue existingDestinationFolderPath,resumeBitLockerTaskXmlFileName))
            let! writeResult =             
                String.Format(xmlFormatString, timeStamp,taskName, resumeBitLockerCmdFilePath)
                |> FileOperations.writeContentToFile logger (resumeBitLockerTaskXmlFilePath)
            return resumeBitLockerTaskXmlFilePath
        }

    let installBitLockerResumeTask () =
        result{            
            let! driverToolProgramDataFolderPath = FileSystem.path driverToolProgramDataFolder
            let! existingDriverToolProgramDataFolderPath = DirectoryOperations.ensureDirectoryExists true driverToolProgramDataFolderPath
            let! resumeBitLockerCmdFilePath = EmbeddedResource.extractEmbeddedResourceByFileName (resumeBitLockerCmdFileName,existingDriverToolProgramDataFolderPath, resumeBitLockerCmdFileName)
            let! schtasksExePath = FileSystem.path schtasksExe
            let! exitCode = ProcessOperations.startConsoleProcess (schtasksExePath, sprintf "/Delete /tn \"%s\" /F" resumeBitLockerTaskName, nativeSystemFolder,-1,null, null, false)
            let! destinationFolderPath = FileSystem.path driverToolProgramDataFolder
            let! resumeBitLockerTaskXmlFilePath = installResumeBitLockerTaskXmlFile (destinationFolderPath,resumeBitLockerCmdFilePath,resumeBitLockerTaskName)
            let arguments = sprintf "/Create /tn \"%s\" /XML \"%s\"" resumeBitLockerTaskName (FileSystem.pathValue resumeBitLockerTaskXmlFilePath)
            let! exitCode = ProcessOperations.startConsoleProcess (schtasksExePath, arguments, nativeSystemFolder,-1,null, null, false)
            return exitCode
        }

    let suspendBitLockerProtection () =
        if(isBitLockerEnabled()) then
            result{
                logger.Info("Suspending BitLocker...")
                let! manageBdeExePath = FileSystem.path manageBdeExe
                let! exitCodeManagedBdeStatusBefore = ProcessOperations.startConsoleProcess (manageBdeExePath,"-status",nativeSystemFolder,-1,null,null,false)
                let! exitCodeManagedBde = ProcessOperations.startConsoleProcess (manageBdeExePath,"-protectors -disable C:",nativeSystemFolder,-1,null,null,false)
                let! exitCodeManagedBdeStatusAfter = ProcessOperations.startConsoleProcess (manageBdeExePath,"-status",nativeSystemFolder,-1,null,null,false)
                logger.Info("Installing scheduled task to resume BitLocker at next boot.")
                let! exitCodeScheduledTask = installBitLockerResumeTask ()
                return exitCodeScheduledTask
            }        
        else
            logger.Info("No need to suspend BitLocker as BitLocker is not enabled.")
            Result.Ok 0