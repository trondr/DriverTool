#Source: https://stackoverflow.com/questions/24370814/how-to-capture-process-output-asynchronously-in-powershell
function Start-ConsoleProcess {
    # Runs the specified executable and captures its exit code, stdout
    # and stderr.
    # Returns: custom object.
    param(
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [String]$FilePath,
        [Parameter(Mandatory=$false)]
        [String[]]$CommandArguments,
        [Parameter(Mandatory=$false)]
        [string]$WorkingDirectory,
        [Parameter(Mandatory=$false)]
        [String]$sVerb,
        [Parameter(Mandatory=$false)]
        [string]$LogFile
    )
    Trace-FunctionCall -Level INFO -Script {
        # Setting process invocation parameters.
        $oPsi = New-Object -TypeName System.Diagnostics.ProcessStartInfo
        $oPsi.CreateNoWindow = $true
        $oPsi.UseShellExecute = $false
        $oPsi.RedirectStandardOutput = $true
        $oPsi.RedirectStandardError = $true
        $oPsi.FileName = $FilePath
        if (! [String]::IsNullOrEmpty($CommandArguments)) {
            $oPsi.Arguments = $CommandArguments
        }

        if (! [String]::IsNullOrEmpty($WorkingDirectory)) {
            $oPsi.WorkingDirectory = $WorkingDirectory
        }

        if (! [String]::IsNullOrEmpty($sVerb)) {
            $oPsi.Verb = $sVerb
        }

        # Creating process object.
        $oProcess = New-Object -TypeName System.Diagnostics.Process
        $oProcess.StartInfo = $oPsi

        # Creating string builders to store stdout and stderr.
        $oStdOutBuilder = New-Object -TypeName System.Text.StringBuilder
        $oStdErrBuilder = New-Object -TypeName System.Text.StringBuilder

        # Adding event handers for stdout and stderr.
        $sScripBlock = {
            if (! [String]::IsNullOrEmpty($EventArgs.Data)) {
                $Event.MessageData.AppendLine($EventArgs.Data)
                # Write-Host $($EventArgs.Data) -ForegroundColor Green
                # Write-Log -Level INFO -Message $($EventArgs.Data)
                # if($LogFile)
                # {
                #     $($EventArgs.Data) | Out-File -FilePath $($LogFile) -Encoding utf8
                # }
            }
        }
        $oStdOutEvent = Register-ObjectEvent -InputObject $oProcess -Action $sScripBlock -EventName 'OutputDataReceived' -MessageData $oStdOutBuilder
        $oStdErrEvent = Register-ObjectEvent -InputObject $oProcess -Action $sScripBlock -EventName 'ErrorDataReceived' -MessageData $oStdErrBuilder

        # Starting process.
        [Void]$oProcess.Start()
        $oProcess.BeginOutputReadLine()
        $oProcess.BeginErrorReadLine()
        [Void]$oProcess.WaitForExit()
        $oProcess.CancelErrorRead()
        $oProcess.CancelOutputRead()

        # Unregistering events to retrieve process output.
        Unregister-Event -SourceIdentifier $oStdOutEvent.Name
        Unregister-Event -SourceIdentifier $oStdErrEvent.Name

        $oResult = New-Object -TypeName PSObject -Property ([Ordered]@{
            "ExeFile"  = $FilePath;
            "Args"     = $CommandArguments -join " ";
            "ExitCode" = $oProcess.ExitCode;
            "StdOut"   = $oStdOutBuilder.ToString().Trim();
            "StdErr"   = $oStdErrBuilder.ToString().Trim()
        })
        if (! [String]::IsNullOrEmpty($($oResult.StdOut))) {
            Write-Log -Level INFO -Message $($oResult.StdOut)
            if($LogFile)
            {
                $($oResult.StdErr) | Out-File -FilePath $($LogFile) -Encoding utf8 -Append
            }
        }
        if (! [String]::IsNullOrEmpty($($oResult.StdErr))) {
            Write-Log -Level ERROR -Message $($oResult.StdErr)
            if($LogFile)
            {
                $($oResult.StdErr) | Out-File -FilePath $($LogFile) -Encoding utf8 -Append
            }
        }
        return $oResult.ExitCode
    }
}
#TEST: Start-ConsoleProcess -FilePath "c:\windows\system32\change.exe" -CommandArguments "user /install"