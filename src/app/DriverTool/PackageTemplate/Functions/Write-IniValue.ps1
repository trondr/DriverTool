function Write-IniValue {
    param (
        [Parameter(Mandatory=$true)]
        [string]
        $IniFile,
        [Parameter(Mandatory=$true)]
        [string]
        $Section,
        [Parameter(Mandatory=$true)]
        [string]
        $ValueName,
        [Parameter(Mandatory=$true)]
        [string]
        $Value
    )
    Trace-FunctionCall -Script {
        Import-IniFileParserLibrary | Out-Null
        $fileIniDataParser = New-Object -TypeName IniParser.FileIniDataParser
        $iniData = New-Object -TypeName IniParser.Model.IniData
        if((Test-Path -Path $IniFile))
        {
            $iniData = $fileIniDataParser.ReadFile($IniFile)
        }
        $iniData[$Section][$ValueName] = $Value
        $fileIniDataParser.WriteFile($IniFile,$iniData)
    }
}
#TEST: 
#$global:VerbosePreference = "Continue"
#Write-IniValue -IniFile "C:\temp\test.ini" -Section "configtest" -ValueName "test1" -Value "testvalue1"