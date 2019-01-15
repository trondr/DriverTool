﻿namespace DriverTool

module BitLockerOperations=
    let logger = Logging.getLoggerByName("BitLockerOperations")
    open System
    open DriverTool.Environment
    open System.Management

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
    
    let installResumeBitLockerTaskXmlFile (destinationFolderPath:Path,resumeBitLockerCmdFilePath:Path,taskName:string) =
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
            let! existingDestinationFolderPath = DirectoryOperations.ensureDirectoryExists (destinationFolderPath,true)            
            let! resumeBitLockerTaskXmlFilePath = 
                Path.create (System.IO.Path.Combine(existingDestinationFolderPath.Value,resumeBitLockerTaskXmlFileName))
            let! writeResult =             
                String.Format(xmlFormatString, timeStamp,taskName, resumeBitLockerCmdFilePath.Value)
                |> FileOperations.writeContentToFile resumeBitLockerTaskXmlFilePath.Value
            return resumeBitLockerTaskXmlFilePath
        }

    let installBitLockerResumeTask () =
        result{            
            let! driverToolProgramDataFolderPath = Path.create driverToolProgramDataFolder
            let! existingDriverToolProgramDataFolderPath = DirectoryOperations.ensureDirectoryExists (driverToolProgramDataFolderPath, true)
            let! resumeBitLockerCmdFilePath = EmbeddedResouce.extractEmbeddedResouceByFileName (resumeBitLockerCmdFileName,existingDriverToolProgramDataFolderPath, resumeBitLockerCmdFileName)
            let! exitCode = ProcessOperations.startConsoleProcess (schtasksExe, String.Format("/Delete /tn \"{0}\" /F", resumeBitLockerTaskName), nativeSystemFolder,-1,null, null, false)
            let! destinationFolderPath = Path.create driverToolProgramDataFolder
            let! resumeBitLockerTaskXmlFilePath = installResumeBitLockerTaskXmlFile (destinationFolderPath,resumeBitLockerCmdFilePath,resumeBitLockerTaskName)
            let! exitCode = ProcessOperations.startConsoleProcess (schtasksExe, String.Format("/Create /tn \"{0}\" /XML \"{1}\"",resumeBitLockerTaskName, resumeBitLockerTaskXmlFilePath.Value), nativeSystemFolder,-1,null, null, false)
            return exitCode
        }

    let suspendBitLockerProtection () =
        if(isBitLockerEnabled()) then
            result{
                logger.Info("Suspending BitLocker...")
                let! exitCodeManagedBdeStatusBefore = ProcessOperations.startConsoleProcess (manageBdeExe,"-status",nativeSystemFolder,-1,null,null,false)
                let! exitCodeManagedBde = ProcessOperations.startConsoleProcess (manageBdeExe,"-protectors -disable C:",nativeSystemFolder,-1,null,null,false)
                let! exitCodeManagedBdeStatusAfter = ProcessOperations.startConsoleProcess (manageBdeExe,"-status",nativeSystemFolder,-1,null,null,false)
                logger.Info("Installing scheduled task to resume BitLocker at next boot.")
                let! exitCodeScheduledTask = installBitLockerResumeTask ()
                return exitCodeScheduledTask
            }        
        else
            logger.Info("No need to suspend BitLocker as BitLocker is not enabled.")
            Result.Ok 0