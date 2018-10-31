
#Source: https://stackoverflow.com/questions/42107851/how-to-implement-using-statement-in-powershell

function Use-Object
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [AllowEmptyCollection()]
        [AllowNull()]
        [Object]
        $InputObject,

        [Parameter(Mandatory = $true)]
        [scriptblock]
        $ScriptBlock
    )

    try
    {
        . $ScriptBlock
    }
    finally
    {
        if ($null -ne $InputObject -and $InputObject -is [System.IDisposable])
        {
            Write-Log -Level DEBUG -Message "Disposing object $($InputObject.GetType().Name)"
            $InputObject.Dispose()
        }
    }
}
#TEST:
<#
$VerbosePreference = "Continue"
$memStream = New-Object System.IO.MemoryStream
Use-Object -InputObject $memStream -ScriptBlock {
    $writeStream = New-Object System.IO.StreamWriter $memStream
    Use-Object -InputObject $writeStream -ScriptBlock {
        $writeStream.WriteLine("Testing 1 2 3 4 5 6")
        $writeStream.Flush()
        $memStream.Seek(0,"Begin") | Out-Null
        $readStream = New-Object System.IO.StreamReader($memStream)
        Use-Object -InputObject $readStream -ScriptBlock {
            while ($readStream.Peek() -ne -1)
            {
                $readStream.ReadLine()
            }
        } -Verbose        
    } -Verbose
} -Verbose
#>