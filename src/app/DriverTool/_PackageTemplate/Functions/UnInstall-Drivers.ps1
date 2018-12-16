function UnInstall-Drivers
{
    Trace-FunctionCall -Script {                        
        $LocalDriversFolder = Get-LocalDriversFolder
        If($(Test-Path -Path $LocalDriversFolder) -eq $true)
        {
            $activeDriverFolders = Get-ChildItem -Path $LocalDriversFolder -Directory | Where-Object {$_.Name -notmatch "^_"} | Sort-Object -Property Name
            $inactiveDriverFolders = Get-ChildItem -Path $LocalDriversFolder -Directory | Where-Object {$_.Name -match "^_"} | Sort-Object -Property Name

            $activeDriverFolders | ForEach-Object { Write-Log -Level INFO -Message "Will be processed: $($_.Name)"}
            $inactiveDriverFolders | ForEach-Object { Write-Log -Level INFO -Message "Will NOT be processed: $($_.Name)"}
            
            $unInstallScripts = $activeDriverFolders | ForEach-Object { [System.IO.Path]::Combine($_.FullName,$(Get-InstallPackageScriptName))}
            $unInstallScripts | ForEach-Object { Assert-FileExists -FileName "$_" -Message "'$(Get-UninstallPackageScriptName)' is missing from folder '$_'" } | Out-Null

            $exitCode = 0
            $activeDriverFolders | ForEach-Object { 
                $activeDriverFolder = $_
                $script = [System.IO.Path]::Combine($_.FullName,$(Get-UninstallPackageScriptName))
                $scriptLogFile = [System.IO.Path]::Combine($(Get-LogDirectory),"DT-Driver-UnInstall-$(Get-PackageFolderName)-$($activeDriverFolder.Name.Replace(".","_")).log")
                $exitCode += Start-ConsoleProcess -FilePath "$(Get-CmdExe)" -CommandArguments "/c `"$($script)`"" -WorkingDirectory "$($activeDriverFolder.FullName)" -LogFile "$scriptLogFile"
            }
            Remove-Item -Path "$LocalDriversFolder" -Recurse -Force -ErrorAction SilentlyContinue | Out-Null
            Write-Log INFO -Message "Accumulated Exit Code: $exitCode"
            $adjustedExitCode = Invoke-Command -ScriptBlock { if($exitCode -gt 3010) {3010} else {$exitCode}}
            Write-Log INFO -Message "Adjusted Exit Code: $exitCode"
            $adjustedExitCode
        }
        else {
            Write-Log -Level WARN -Message "Local drivers folder does not exit. Nothing to uninstall."
            $exitCode = 0
            $exitCode
        }
    }
}
