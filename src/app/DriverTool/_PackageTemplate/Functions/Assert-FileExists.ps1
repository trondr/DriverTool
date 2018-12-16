function Assert-FileExists
{
    Param(
        [ValidateNotNullOrEmpty()]
        [String]
        $FileName=$(throw "FileName must be specified."),
        [String]
        $Message
    )
    Trace-FunctionCall -Script{
        if([System.IO.File]::Exists($FileName) -eq $false)
        {
            $(throw "$Message Error: File '$FileName' does not exist.")
        }
        Write-Log -Level DEBUG -Message "File '$FileName' exists!"
    }
}
#TEST : Assert-FileExists -FileName "c:\temp\notes.txt" -Message "Notes.txt was not found."