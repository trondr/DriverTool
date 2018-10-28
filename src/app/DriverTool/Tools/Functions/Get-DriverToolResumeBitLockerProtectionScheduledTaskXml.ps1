function Get-DriverToolResumeBitLockerProtectionScheduledTaskXml
{
    Trace-FunctionCall -Script {
        $schedulesTaskXmlString = 
@"
<?xml version="1.0" encoding="UTF-16"?>
<Task version="1.3" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
    <RegistrationInfo>
    <Date>$($(Get-Date).ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffff"))</Date>
    <Author>DriverTool\DriverTool</Author>
    <URI>\$(Get-DriverToolResumeBitLockerProtectionScheduledTaskName)</URI>
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
        <Command>$(Install-DriverToolResumeBitLockerProtectionCmd)</Command>
        <Arguments>Install</Arguments>
    </Exec>
    </Actions>
</Task>    
"@            
            $DestinationXml = [System.IO.Path]::Combine("$(Get-DriverToolProgramDataFolder)","DriverTool Resume BitLocker Protection.xml")
            Out-File -FilePath "$DestinationXml" -Encoding Default -InputObject $schedulesTaskXmlString -Force
            $DestinationXml
    }
}
#TEST: Get-DriverToolResumeBitLockerProtectionScheduledTaskXml