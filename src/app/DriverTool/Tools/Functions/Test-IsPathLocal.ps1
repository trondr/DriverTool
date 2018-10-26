function Test-IsPathLocal {
    param (
        [Parameter(Mandatory=$true)]
        [string]
        $Path
    )
    Write-Verbose "Test-IsPathLocal -Path '$Path'..."
    $uri = New-Object System.Uri($path)
    if($uri.IsUnc)
    {
        Write-Verbose "Path is remote unc: $Path"
        $isPathLocal = $false
    }
    else {
        $drive = Split-Path -Qualifier $path
        Write-Verbose "Drive=$drive"
        $logicalDisk = Get-WmiObject Win32_LogicalDisk -filter "DriveType = 3 AND DeviceID = '$drive'"
        if($null -eq $logicalDisk )
        {
            Write-Verbose "Path is remote: $path"
            $isPathLocal = $false
        }
        else {
            Write-Verbose "Path is local: $path"
            $isPathLocal = $true
        }
    }
    Write-Verbose "Test-IsPathLocal -Path '$Path'->$isPathLocal"
    return $isPathLocal
}
#TEST: 
#TEST: Test-IsPathLocal -Path 'C:\'
#TEST: Test-IsPathLocal -Path 'G:\'
#TEST: Test-IsPathLocal -Path '\\Someserver\testshare'

