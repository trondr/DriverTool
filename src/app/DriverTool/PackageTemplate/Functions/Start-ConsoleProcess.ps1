function Start-ConsoleProcess {
    param (
        [Parameter(Mandatory=$true)]
        [string]
        $FilePath,
        [string]
        $CommandArguments,
        [string]
        $WorkingDirectory,
        [string]
        $LogFile
    )
    Trace-FunctionCall -Level INFO -Script{
        $pinfo = New-Object System.Diagnostics.ProcessStartInfo
        $pinfo.FileName = $FilePath
        $pinfo.WorkingDirectory = $WorkingDirectory
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
        Write-Log -Level INFO -Message "Stdout: $stdout"
        if([string]::IsNullOrWhiteSpace($stderr) -eq $false)
        {
            Write-Log -Level ERROR -Message "Stderr: $stderr"
        }
        
        if($LogFile)
        {
            $stdout | Out-File -FilePath $($LogFile) -Encoding utf8
            if([string]::IsNullOrWhiteSpace($stderr) -eq $false)
            {                
                $stderr | Out-File -FilePath $($LogFile) -Encoding utf8 -Append
            }
        }
        return $p.ExitCode
    }
}
#TEST: 
#Start-ConsoleProcess -FilePath "c:\windows\system32\change.exe" -CommandArguments "user /install"