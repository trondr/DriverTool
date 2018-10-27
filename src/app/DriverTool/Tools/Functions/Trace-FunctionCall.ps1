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
        Write-Verbose "$functionName $arguments"
    }
    else {
        Write-Host "$functionName $arguments"
    }
    $returnValue = Invoke-Command $Script
    if($Level -eq "DEBUG"){
        Write-Verbose "$functionName->$returnValue"
    }
    else {
        Write-Host "$functionName->$returnValue"
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