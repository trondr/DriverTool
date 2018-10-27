function Assert-IsAdministrator
{
    Trace-FunctionCall -Script{
        if($(Test-IsAdministrator) -eq $false)
        {
            $errorMessage = "Current user '$($env:USERNAME)' is not administrator."
            Write-Log -Level ERROR $errorMessage
            $(throw $errorMessage)
        }  
        Write-Log -Level DEBUG -Message "Current user '$($env:USERNAME)' is administrator.!"
    }
}
#TEST: Assert-IsAdministrator