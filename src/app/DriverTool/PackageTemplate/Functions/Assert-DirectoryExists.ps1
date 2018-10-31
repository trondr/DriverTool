function Assert-DirectoryExists
{
    Param(
        [ValidateNotNullOrEmpty()]
        [String]
        $DirectoryName=$(throw "DirectoryName must be specified."),
        [String]
        $Message
    )
    Trace-FunctionCall -Script{
        if([System.IO.Directory]::Exists($DirectoryName) -eq $false)
        {
            $(throw "$Message Error: Directory '$DirectoryName' does not exist.")
        }  
        Write-Log -Level DEBUG -Message "Directory '$DirectoryName' exists!"
    }  
}
#TEST : 
#Assert-DirectoryExists -DirectoryName "c:\temp\dssfs" -Message "dssfs was not found."
#Assert-DirectoryExists -DirectoryName "c:\temp" -Message "temp was not found."