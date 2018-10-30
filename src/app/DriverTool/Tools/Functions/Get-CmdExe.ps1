function Get-CmdExe{
    Trace-FunctionCall -Script {
        Get-CacedValue -ValueName "CmdExe" -OnCacheMiss {
            $cmdExe = [System.IO.Path]::Combine($(Get-SystemFolder),"cmd.exe")
            Assert-FileExists -FileName $cmdExe -Message "cmd.exe not found."
            $cmdExe
        }        
    }
}