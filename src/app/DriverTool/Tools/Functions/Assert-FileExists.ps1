function Assert-FileExists
{
    Param(
        [ValidateNotNullOrEmpty()]
        [String]
        $FileName=$(throw "FileName must be specified."),
        [String]
        $Message
    )
    if([System.IO.File]::Exists($FileName) -eq $false)
    {
        $(throw "$Message Error: File '$FileName' does not exist.")
    }    
}
#TEST : Assert-FileExists -FileName "c:\temp\notes.txt" -Message "Notes.txt was not found."