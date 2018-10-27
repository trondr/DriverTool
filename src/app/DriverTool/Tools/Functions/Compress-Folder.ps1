function Compress-Folder
{
    param(
        [Parameter(Mandatory=$true)]
        [string]
        $FolderName,
        [Parameter(Mandatory=$true)]
        [string]
        $FileName
        )
        Trace-FunctionCall -Script{
            Assert-DirectoryExists -DirectoryName $FolderName -Message "Folder to compress not found."
            $exitCode = Start-ConsoleProcess -FilePath "$(Get-7zipExe)" -CommandArguments "a `"$($FileName)`"  `"$($FolderName)\*`"" -WorkingDirectory "$($FolderName)"
        return $exitCode
        }
}
#TEST: Compress-Folder -FolderName "C:\Temp\20EQ_Driver_Package\2018-10-15\Drivers" -FileName "C:\Temp\20EQ_Driver_Package\2018-10-15\Drivers.zip"