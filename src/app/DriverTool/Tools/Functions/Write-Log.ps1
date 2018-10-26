Set-StrictMode -Version Latest

#Credits: https://stackoverflow.com/questions/7834656/create-log-file-in-powershell

function Write-Log {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$False)]
        [ValidateSet("INFO","SUCCESS","WARN","ERROR","FATAL","DEBUG")]
        [String]
        $Level = "INFO",

        [Parameter(Mandatory=$True)]
        [string]
        $Message,

        [Parameter(Mandatory=$False)]
        [string]
        $Logfile="$(Get-LogFile)"
    )
    
    begin {
        $LogLevelColorMap = @{}
        $LogLevelColorMap.Add("INFO","White")
        $LogLevelColorMap.Add("SUCCESS","Green")
        $LogLevelColorMap.Add("WARN","Yellow")
        $LogLevelColorMap.Add("ERROR","Red")
        $LogLevelColorMap.Add("FATAL","Red")
        $LogLevelColorMap.Add("DEBUG","Cyan")
    }
    
    process {        
        if(($Level -eq "DEBUG") -and ($VerbosePreference -eq "SilentlyContinue"))
        {
            return
        }
        $date = Get-Date
        $Stamp = $date.toString("yyyy-MM-dd HH:mm:ss")
        $ConsoleLine = "$Stamp $($Level.PadRight(7)) $Message"
        If($Logfile) {
            $dateString = $date.ToString("MM-dd-yyyy")
            $timeString = $date.ToString("HH:mm:ss.654-120")
            $timeString = Use-Culture en-US {$date.ToString("HH:mm:ss.654-120")}
            $LogLine = "<![LOG[$Message]LOG]!><time=""$timeString"" date=""$dateString"" component=""ScriptInstaller"" context="""" type=""6"" thread=""$([System.Threading.Thread]::CurrentThread.ManagedThreadId)"" file=""?:?"">"
            Add-Content -Path "$Logfile" -Value $LogLine | Out-Null
        }
        if(($Level -eq "FATAL") -and ($VerbosePreference -eq "SilentlyContinue"))
        {
            Write-Host $ConsoleLine -ForegroundColor $LogLevelColorMap[$Level] -BackgroundColor White
            [System.Console]::Beep(2000,1000)
        }
        else {
            Write-Host $ConsoleLine -ForegroundColor $LogLevelColorMap[$Level]                
        }
    }
    
    end {
    }
}
#TEST:
<#
$VerbosePreference = "Continue"
Write-Log -Message "Test" -Level INFO
Write-Log -Message "Test" -Level SUCCESS
Write-Log -Message "Test" -Level ERROR
Write-Log -Message "Test" -Level WARN
Write-Log -Message "Test" -Level FATAL
Write-Log -Message "Test" -Level DEBUG
#>