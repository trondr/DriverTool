function Get-LocalDriversFolder{
    Trace-FunctionCall -Script{
        Get-CacedValue -ValueName "LocalDriversFolder" -OnCacheMiss {
            $ComputerVendor = $(Get-InstallProperty -PropertyName "ComputerVendor")
            $ComputerModel = $(Get-InstallProperty -PropertyName "ComputerModel")            
            $OsShortName = $(Get-InstallProperty -PropertyName "OsShortName")
            $WindowsFolder = [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::Windows)            
            [System.IO.Path]::Combine($WindowsFolder,"Drivers","$($ComputerVendor)_$($ComputerModel)_$($OsShortName)")
        }
    }
}
#TEST: 
#Clear-Cache
#Get-LocalDriversFolder