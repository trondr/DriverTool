function Read-IniValue {
    param (
        [Parameter(Mandatory=$true)]
        [string]
        $IniFile,
        [Parameter(Mandatory=$true)]
        [string]
        $Section,
        [Parameter(Mandatory=$true)]
        [string]
        $ValueName
    )
    Trace-FunctionCall -Script {
        Import-IniFileParserLibrary | Out-Null
        $fileIniDataParser = New-Object -TypeName IniParser.FileIniDataParser
        $iniData = New-Object -TypeName IniParser.Model.IniData
        if((Test-Path -Path $IniFile))
        {
            $iniData = $fileIniDataParser.ReadFile($IniFile)
        }
        $iniData[$Section][$ValueName]
    }
}
#TEST: 
# $global:VerbosePreference = "Continue"
# Clear-Cache
# Read-IniValue -IniFile "C:\Windows\system.ini" -Section "drivers" -ValueName "timer"