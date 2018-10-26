function Get-LogDirectory {
    Write-Verbose "Get-LogDirectory..."
    $logDirectory = $global:logDirectory
    if($(Test-Path variable:global:logDirectory) -and ($null -ne $logDirectory) -and (Test-Path -Path $logDirectory))
    {
        $logDirectory = $global:logDirectory
    }
    else {
        $global:logDirectory = Get-LogDirectoryFromInstallXml
        if($null -ne $logDirectory)
        {
            New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
            $logDirectory = $global:logDirectory
        }
        else {
            $global:logDirectory = [System.IO.Path]::Combine([System.Environment]::ExpandEnvironmentVariables("%PUBLIC%"),"Logs")
            $logDirectory = $global:logDirectory
            New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
        }
    }
    Write-Verbose "Get-LogDirectory->$logDirectory"
    return $logDirectory
}
#TEST: Get-LogDirectory