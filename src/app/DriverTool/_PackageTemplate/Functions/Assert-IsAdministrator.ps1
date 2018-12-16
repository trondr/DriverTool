function Assert-IsAdministrator
{
    Param(
        [String]
        $Message
    )
    Trace-FunctionCall -Script{
        if($(Test-IsAdministrator) -eq $false)
        {
            $errorMessage = "$Message Current user '$($env:USERNAME)' is not administrator.".Trim()
            Write-Log -Level ERROR $errorMessage
            $(throw $errorMessage)
        }  
        Write-Log -Level DEBUG -Message "Current user '$($env:USERNAME)' is administrator.!"
    }
}
#TEST: Assert-IsAdministrator