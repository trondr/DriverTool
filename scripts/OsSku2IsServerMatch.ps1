#Copied OS SKUs from table: https://docs.microsoft.com/en-us/windows/desktop/api/sysinfoapi/nf-sysinfoapi-getproductinfo
#into the following format (using a Notepad++ macro):
#Code;Value;Meaning
#PRODUCT_BUSINESS;0x00000006;Business
#PRODUCT_BUSINESS_N;0x00000010;Business N
#PRODUCT_CLUSTER_SERVER;0x00000012;HPC Edition
#[...]
$osSkus = Import-Csv -Path "C:\Temp\DriverToolCache\OSSkus.csv" -Delimiter ';'|Sort-Object -Property Value
$osSkusTransformed = @()
foreach($osSku in $osSkus)
{
    $value = [Convert]::ToInt64($osSku.Value,16)
    $name = $osSku.Meaning
    $isServer = "false"
    if($name -match "Server")
    {
        $isServer = "true"        
    }
    $item = [PSCustomObject]@{

        Value        = $value
        Name         = $name
        IsServer     = $isServer
    }
    $osSkusTransformed+=$item    
}
$osSkusTransformed|Sort-Object -Property Value|ForEach-Object { Write-Output "|$($_.Value)u -> $($_.IsServer) //$($_.Name)"}
$osSkusTransformed|Sort-Object -Property Value|ForEach-Object { Write-Output "[<TestCase($($_.Value)u,$($_.IsServer),`"$($_.Name)`")>]"}


