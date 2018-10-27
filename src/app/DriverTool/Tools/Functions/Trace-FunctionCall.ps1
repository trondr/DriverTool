function Trace-FunctionCall
{
    param(   
        [Parameter(Mandatory=$true)]
        [ScriptBlock]
        $Script,
        [string]
        $Level="DEBUG"
    )
    $functionName = $($(Get-PSCallStack)[1].FunctionName)
    $arguments =  $($(Get-PSCallStack)[1].Arguments)
    if($Level -eq "DEBUG"){
        Write-Log -Level DEBUG -Message "$functionName $arguments"
    }
    else {
        Write-Log -Level INFO -Message "$functionName $arguments"
    }
    $returnValue = Invoke-Command $Script
    if($Level -eq "DEBUG"){
        Write-Log -Level DEBUG -Message "$functionName->$returnValue"
    }
    else {
        Write-Log -Level INFO -Message "$functionName->$returnValue"
    }
    return $returnValue
}
#TEST:
# Trace-FunctionCall -Script{
#     Write-Host "Calling some function"
#     "SomeTest"
# }

# function MyTestFunc{
#     Trace-FunctionCall -Script{
#         Write-Host "Calling some function"
#         "SomeTest"
#     }
# }
# MyTestFunc