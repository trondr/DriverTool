function Assert-FileNotExists
{
    Param(
        [ValidateNotNullOrEmpty()]
        [String]
        $FileName=$(throw "FileName must be specified."),
        [String]
        $Message
    )
    Trace-FunctionCall -Script{
        if([System.IO.File]::Exists($FileName) -eq $true)
        {
            $(throw "$Message Error: File '$FileName' exists.")
        }
        Write-Log -Level DEBUG -Message "File '$FileName' does not exist. Ok!"
    }
}
#TEST : Assert-FileNotExists -FileName "c:\temp\notes.txt" -Message "Notes.txt was found."