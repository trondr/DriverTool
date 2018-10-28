function Remove-DriverToolProgramDataFolder
{
    Trace-FunctionCall -Script {
        $DriverToolProgramDataFolder = [string]$(Get-DriverToolProgramDataFolder)
        if($([string]::IsNullOrWhiteSpace($DriverToolProgramDataFolder)) -eq $false -and $($DriverToolProgramDataFolder.EndsWith("trndr.DriverTool")) -eq $true)
        {
            Remove-Item -Path "$DriverToolProgramDataFolder" -Recurse -Force
        }
        Assert-DirectoryNotExists -DirectoryName $DriverToolProgramDataFolder -Message "DriverTool program data folder was not removed."
        $DriverToolProgramDataFolder
    }
}
#TEST: Remove-DriverToolProgramDataFolder