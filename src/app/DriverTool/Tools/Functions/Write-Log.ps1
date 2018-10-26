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
        $Stamp = (Get-Date).toString("yyyy-MM-dd HH:mm:ss")
        $Line = "$Stamp $($Level.PadRight(7)) $Message"
        If($Logfile) {
            Add-Content -Path "$Logfile" -Value $Line | Out-Null
        }
        if(($Level -eq "FATAL") -and ($VerbosePreference -eq "SilentlyContinue"))
        {
            Write-Host $Line -ForegroundColor $LogLevelColorMap[$Level] -BackgroundColor White
            [System.Console]::Beep(2000,1000)
        }
        else {
            Write-Host $Line -ForegroundColor $LogLevelColorMap[$Level]                
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