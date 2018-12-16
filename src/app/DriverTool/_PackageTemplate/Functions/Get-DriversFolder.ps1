function Get-DriversFolder{
    Trace-FunctionCall -Script{
        [System.IO.Path]::Combine($(Get-ScriptFolder),"Drivers")
    }
}
#TEST: Get-DriversFolder