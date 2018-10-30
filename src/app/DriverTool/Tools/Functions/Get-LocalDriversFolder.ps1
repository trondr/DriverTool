function Get-LocalDriversFolder{
    Trace-FunctionCall -Script{
        Get-CacedValue -ValueName "LocalDriversFolder" -OnCacheMiss {            
            $WindowsFolder = [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::Windows)            
            [System.IO.Path]::Combine($WindowsFolder,"Drivers",$(Get-PackageFolderName))
        }
    }
}
#TEST: 
#Clear-Cache
#Get-LocalDriversFolder

