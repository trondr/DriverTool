function Start-ConsoleProcess {
    param (
        [Parameter(Mandatory=$true)]
        [string]
        $FilePath,
        [string]
        $Arguments
    )
    Write-Host "Start-ConsoleProcess -FilePath '$FilePath' -Arguments '$Arguments'"
    $pinfo = New-Object System.Diagnostics.ProcessStartInfo
    $pinfo.FileName = $FilePath
    $pinfo.RedirectStandardError = $true
    $pinfo.RedirectStandardOutput = $true
    $pinfo.UseShellExecute = $false
    $pinfo.Arguments = $Arguments
    $p = New-Object System.Diagnostics.Process
    $p.StartInfo = $pinfo
    $p.Start() | Out-Null
    $p.WaitForExit()
    $stdout = $p.StandardOutput.ReadToEnd()
    $stderr = $p.StandardError.ReadToEnd()
    Write-Host "Stdout: $stdout"
    Write-Host "Stderr: $stderr"
    Write-Host "Start-ConsoleProcess->$($p.ExitCode)"
    return $p.ExitCode
}
#TEST: Start-ConsoleProcess -FilePath "c:\windows\system32\change.exe" -Arguments "user /install"