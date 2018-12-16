function Compress-Drivers
{
    Trace-FunctionCall -Script{
        $DriversFolder = "$(Get-DriversFolder)"
        Assert-DirectoryExists -DirectoryName $DriversFolder -Message "Folder to compress not found."
        $DriversZipFile = $(Get-DriversZipFile)
        if($(Test-Path -Path "$DriversZipFile") -eq $true)
        {
            [System.IO.File]::Delete($DriversZipFile)
        }        
        Compress-Folder -FolderName "$DriversFolder" -FileName "$DriversZipFile"
    }
}
#TEST: Compress-Drivers

