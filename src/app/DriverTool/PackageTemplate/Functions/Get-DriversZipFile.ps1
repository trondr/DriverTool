function Get-DriversZipFile{
    Trace-FunctionCall -Script{
        [System.IO.Path]::Combine($(Get-ScriptFolder),"Drivers.zip")
    }
}
#TEST: Get-DriversFolder