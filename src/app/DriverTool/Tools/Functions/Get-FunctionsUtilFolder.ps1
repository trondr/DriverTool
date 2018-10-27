function Get-FunctionsUtilFolder {
    Trace-FunctionCall -Script {
        $functionsUtilFolder = Get-CacedValue -ValueName "FunctionsUtilFolder" -OnCacheMiss {
            [System.IO.Path]::Combine($(Get-FunctionsFolder),"Util")
        }    
        $functionsUtilFolder
    }
}
#TEST:
# $global:VerbosePreference = "Continue"
# Get-FunctionsUtilFolder

