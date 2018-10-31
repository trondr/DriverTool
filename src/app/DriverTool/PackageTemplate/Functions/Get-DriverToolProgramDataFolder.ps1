function Get-DriverToolProgramDataFolder
{
    Trace-FunctionCall -Script{
        # Get-CacedValue -ValueName "DriverToolProgramDataFolder" -OnCacheMiss {
            $DriverToolProgramDataFolder = [System.IO.Path]::Combine($env:ProgramData,"trndr.DriverTool")
            if($(Test-Path $DriverToolProgramDataFolder) -eq $false)
            {
                New-Item -ItemType Directory -Path "$DriverToolProgramDataFolder" | Out-Null
            }
            Assert-DirectoryExists -DirectoryName "$DriverToolProgramDataFolder" -Message "DriverTool program data folder does not exist."
            $DriverToolProgramDataFolder
        #}
    }
}
#TEST: Get-DriverToolProgramDataFolder
