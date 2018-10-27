function Start-ConsoleProcess {
    param (
        [Parameter(Mandatory=$true)]
        [string]
        $FilePath,
        [string]
        $CommandArguments
        #,
        # [string]
        # $WorkingDirectory
    )
    Trace-FunctionCall -Level INFO -Script{
        $pinfo = New-Object System.Diagnostics.ProcessStartInfo
        $pinfo.FileName = $FilePath
        $pinfo.WorkingDirectory = ""#$WorkingDirectory
        $pinfo.RedirectStandardError = $true
        $pinfo.RedirectStandardOutput = $true
        $pinfo.UseShellExecute = $false
        $pinfo.Arguments = $CommandArguments
        $p = New-Object System.Diagnostics.Process
        $p.StartInfo = $pinfo
        $p.Start() | Out-Null
        $p.WaitForExit()
        $stdout = $p.StandardOutput.ReadToEnd()
        $stderr = $p.StandardError.ReadToEnd()
        Write-Host "Stdout: $stdout"
        Write-Host "Stderr: $stderr"
        return $p.ExitCode
    }
}
#TEST: 
Start-ConsoleProcess -FilePath "c:\windows\system32\change.exe" -CommandArguments "user /install"