function Assert-DirectoryNotExists
{
    Param(
        [ValidateNotNullOrEmpty()]
        [String]
        $DirectoryName=$(throw "DirectoryName must be specified."),
        [String]
        $Message
    )
    Trace-FunctionCall -Script{
        if([System.IO.Directory]::Exists($DirectoryName) -eq $true)
        {
            $(throw "$Message Error: Directory '$DirectoryName' exists.")
        }  
        Write-Verbose "Directory '$DirectoryName' does not exist. Ok."
    }  
}
#TEST : 
#Assert-DirectoryNotExists -DirectoryName "c:\temp\dssfs" -Message "dssfs was not found."
#Assert-DirectoryNotExists -DirectoryName "c:\temp" -Message "temp was found."