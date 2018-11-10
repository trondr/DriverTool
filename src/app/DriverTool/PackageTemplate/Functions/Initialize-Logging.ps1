function Initialize-Logging {
    $logFile = Get-LogFile
    $log4NetAssembly = Import-Log4NetLibrary
    [log4net.GlobalContext]::Properties["LogFile"] = $logFile
    $appConfigFile = Get-Item -Path $([System.IO.Path]::Combine($(Get-ScriptFolder),"Install.config.xml"))
    $loggerRepository = [log4net.LogManager]::GetRepository($log4NetAssembly)
    [log4net.Config.XmlConfigurator]::ConfigureAndWatch($loggerRepository,$appConfigFile) |Out-Null   
    $global:LoggingIsInitialized = $true
}
$global:VerbosePreference = "Continue"
Clear-Cache
Initialize-Logging