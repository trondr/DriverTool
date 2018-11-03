function Get-ConsoleLogLine
{
    param (
        [Parameter(Mandatory=$False)]
        [ValidateSet("INFO","SUCCESS","WARN","ERROR","FATAL","DEBUG")]
        [String]
        $Level = "INFO",

        [Parameter(Mandatory=$True)]
        [string]
        $Message
    )
    $date = Get-Date
    $Stamp = $date.toString("yyyy-MM-dd HH:mm:ss")  
    $ConsoleLine = "$Stamp $($Level.PadRight(7)) $Message"
    $ConsoleLine
}
#TEST: Get-ConsoleLogLine -Level INFO -Message "This is a info message"
#TEST: Get-ConsoleLogLine -Level ERROR -Message "This is a error message"