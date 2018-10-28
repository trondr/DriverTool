function Expand-Drivers
{
    Trace-FunctionCall -Script{
        $DriversFolder = "$(Get-DriversFolder)"
        $DriversZipFile = $(Get-DriversZipFile)
        Assert-FileExists -FileName "$DriversZipFile" -Message "Not able to expand drivers as Drivers.zip does not exist."
        Assert-DirectoryNotExists -DirectoryName $DriversFolder -Message "Drivers Folder allread exists. Please delete or rename the folder before expanding Drivers.zip"
        Expand-Folder -FolderName "$DriversFolder" -FileName "$DriversZipFile"
    }
}
#TEST: Expand-Drivers