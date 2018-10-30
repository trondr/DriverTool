function Copy-Drivers {
     Trace-FunctionCall -Script{
        $exitCode = 0
        $LocalDriversFolder = Get-LocalDriversFolder
        New-Item -ItemType Directory -Path "$LocalDriversFolder" -Force | Out-Null
        Assert-DirectoryExists -DirectoryName "$LocalDriversFolder" -Message "Local drivers folder does not exist."
        $SourceDriversFolder = Get-DriversFolder
        if($(Test-Path -Path $SourceDriversFolder) -eq $true)
        {
           $RobocopyExitCode = Invoke-RoboCopy -SourceFolder "$SourceDriversFolder" -DestinationFolder "$LocalDriversFolder" -Options "*.* /MIR"
           $exitCode = Get-RoboCopyResult -RoboCopyExitCode $RobocopyExitCode
        }
        else {
           $DriversZipFile = Get-DriversZipFile
           Assert-FileExists -FileName "$DriversZipFile" -Message "Drivers.zip not found."
           $exitCode = Expand-Folder -FolderName "$LocalDriversFolder" -FileName "$DriversZipFile"            
        }
        $exitCode
     }   
}
