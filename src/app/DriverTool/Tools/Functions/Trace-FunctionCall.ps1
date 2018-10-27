function Trace-FunctionCall
{
    param(   
        [Parameter(Mandatory=$true)]
        [ScriptBlock]
        $Script
    )
    $functionName = $($(Get-PSCallStack)[1].FunctionName)
    $arguments =  $($(Get-PSCallStack)[1].Arguments)
    Write-Verbose "$functionName $arguments"
    $returnValue = Invoke-Command $Script
    Write-Verbose "$functionName->$returnValue"
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