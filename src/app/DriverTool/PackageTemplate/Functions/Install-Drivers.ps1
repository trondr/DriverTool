function Install-Drivers
{
    Trace-FunctionCall -Script {        
        $exitCode = Copy-Drivers
        if($exitCode -ne 0)
        {
            Write-Log -Level ERROR -Message "Unable to install drivers due to issue with copying drivers locally."
            return $exitCode
        }        
        $LocalDriversFolder = Get-LocalDriversFolder
        $activeDriverFolders = Get-ChildItem -Path $LocalDriversFolder -Directory | Where-Object {$_.Name -notmatch "^_"} | Sort-Object -Property Name
        $inactiveDriverFolders = Get-ChildItem -Path $LocalDriversFolder -Directory | Where-Object {$_.Name -match "^_"} | Sort-Object -Property Name

        $activeDriverFolders | ForEach-Object { Write-Log -Level INFO -Message "Will be processed: $($_.Name)"}
        $inactiveDriverFolders | ForEach-Object { Write-Log -Level INFO -Message "Will NOT be processed: $($_.Name)"}
        
        $installScripts = $activeDriverFolders | ForEach-Object { [System.IO.Path]::Combine($_.FullName,$(Get-InstallPackageScriptName))}
        $installScripts | ForEach-Object { Assert-FileExists -FileName "$_" -Message "'$(Get-InstallPackageScriptName)' is missing from folder '$_'" } | Out-Null
        
        $exitCode = 0
        $activeDriverFolders | ForEach-Object { 
            $activeDriverFolder = $_
            $script = [System.IO.Path]::Combine($_.FullName,$(Get-InstallPackageScriptName))
            $scriptLogFile = [System.IO.Path]::Combine($(Get-LogDirectory),"DT-Driver-Install-$(Get-PackageFolderName)-$($activeDriverFolder.Name.Replace(".","_")).log")
            $exitCode += Start-ConsoleProcess -FilePath "$(Get-CmdExe)" -CommandArguments "/c `"$($script)`"" -WorkingDirectory "$($activeDriverFolder.FullName)" -LogFile "$scriptLogFile"
        }
        Reset-ConfigFlags | Out-Null
        Write-Log INFO -Message "Accumulated Exit Code: $exitCode"
        $adjustedExitCode = Invoke-Command -ScriptBlock { if($exitCode -gt 3010) {3010} else {$exitCode}}
        Write-Log INFO -Message "Adjusted Exit Code: $exitCode"        
        $adjustedExitCode
    }
}


