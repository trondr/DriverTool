function Get-PackageFolderName
{
    Trace-FunctionCall -Script {
        Get-CacedValue -ValueName "PackageFolderName" -OnCacheMiss {
            $ComputerVendor = $(Get-InstallProperty -PropertyName "ComputerVendor")
            $ComputerModel = $(Get-InstallProperty -PropertyName "ComputerModel")            
            $OsShortName = $(Get-InstallProperty -PropertyName "OsShortName")
            "$($ComputerVendor)_$($ComputerModel)_$($OsShortName)"
        }
    }
}
#TEST: Get-PackageFolderName