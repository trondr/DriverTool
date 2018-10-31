function Get-7zipExe {
    Trace-FunctionCall -Script {
        Get-CacedValue -ValueName "7ZipExe" -OnCacheMiss{
            $7zipExe = [System.IO.Path]::Combine("$(Get-FunctionsUtilFolder)","7Zip","7za.exe")
            Assert-FileExists -FileName "$($7zipExe)" -Message "7za.exe not found."
            $7zipExe
        }
    }
}
#TEST:
# Clear-Cache
# $global:scriptFolder = "E:\Dev\github.trondr\DriverTool\src\app\DriverTool\Tools"
# $global:VerbosePreference = "Continue"
# Get-7zipExe