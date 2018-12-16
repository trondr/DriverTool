


function Get-ChangeExe
{
    Trace-FunctionCall -Script {
        [System.IO.Path]::Combine($(Get-SystemFolder), "change.exe")
    }
}
#TEST: Get-ChangeExe