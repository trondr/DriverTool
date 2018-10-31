function Test-IsPathLocal {
    param (
        [Parameter(Mandatory=$true)]
        [string]
        $Path
    )
    Trace-FunctionCall -Script {
        $uri = New-Object System.Uri($path)
        if($uri.IsUnc)
        {
            Write-Log -Level DEBUG -Message "Path is remote unc: $Path"
            $isPathLocal = $false
        }
        else {
            $drive = Split-Path -Qualifier $path
            Write-Log -Level DEBUG -Message "Drive=$drive"
            $logicalDisk = Get-WmiObject Win32_LogicalDisk -filter "DriveType = 3 AND DeviceID = '$drive'"
            if($null -eq $logicalDisk )
            {
                Write-Log -Level DEBUG -Message "Path is remote: $path"
                $isPathLocal = $false
            }
            else {
                Write-Log -Level DEBUG -Message "Path is local: $path"
                $isPathLocal = $true
            }
        }
        return $isPathLocal
    }
    
}
#TEST: 
#TEST: Test-IsPathLocal -Path 'C:\'
#TEST: Test-IsPathLocal -Path 'G:\'
#TEST: Test-IsPathLocal -Path '\\Someserver\testshare'

