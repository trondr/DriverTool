function Get-FunctionsFolder {
    Trace-FunctionCall -Script {
        $functionsFolder = Get-CacedValue -ValueName "FunctionsFolder" -OnCacheMiss {
            [System.IO.Path]::Combine($(Get-ScriptFolder),"Functions")
        }    
        $functionsFolder
    }
}
#TEST:
#$global:VerbosePreference = "Continue"
#Get-FunctionsFolder