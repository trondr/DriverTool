function Invoke-RoboCopy
{
     param(
        [Parameter(Mandatory=$true)]
        [string]
        $SourceFolder,
        [Parameter(Mandatory=$true)]
        [string]
        $DestinationFolder,
        [Parameter(Mandatory=$true)]
        [string]
        $Options
        )
        $robocopyExitCode = Start-ConsoleProcess -FilePath "$(Get-RobocopyExe)" -Arguments "`"$($SourceFolder)`"  `"$($DestinationFolder)`" $Options"
        $exitCode = Get-RoboCopyResult -RoboCopyExitCode $robocopyExitCode
        return $exitCode
}
#TEST: Invoke-RoboCopy -SourceFolder C:\temp\20EQ_Driver_Package\2018-10-15\Tools -DestinationFolder C:\Temp\temp_tools -Options "*.* /V /S /E"

