function Get-LogFileLogLine
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
    $dateString = $date.ToString("MM-dd-yyyy")
    $timeString = $date.ToString("HH:mm:ss.654-120")
    $timeString = Use-Culture en-US {$date.ToString("HH:mm:ss.654-120")}
    $LogLine = "<![LOG[$Message]LOG]!><time=""$timeString"" date=""$dateString"" component=""ScriptInstaller"" context="""" type=""6"" thread=""$([System.Threading.Thread]::CurrentThread.ManagedThreadId)"" file=""?:?"">"
    $LogLine
}
#TEST. Get-LogFileLogLine -Level INFO -Message "This is a test message"