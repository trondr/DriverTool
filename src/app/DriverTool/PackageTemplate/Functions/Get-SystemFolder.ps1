function Get-SystemFolder
{
    Trace-FunctionCall -Script {
        $systemFolder = [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::System)
        return $systemFolder
    }
}
#TEST: Get-SystemFolder