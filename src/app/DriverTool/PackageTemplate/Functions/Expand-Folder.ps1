function Expand-Folder
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
            Assert-FileExists -FileName $FileName -Message "File to expand not found."
            Assert-DirectoryNotExists -DirectoryName $FolderName -Message "Cannot expand to and allready existing folder."
            New-Item -ItemType Directory -Path $FolderName -Force | Out-Null
            $exitCode = Start-ConsoleProcess -FilePath "$(Get-7zipExe)" -CommandArguments "x `"$($FileName)`"  -o`"$($FolderName)`" -y" -WorkingDirectory "$($FolderName)"
        return $exitCode
        }
}
#TEST: Expand-Folder -FolderName "C:\Temp\20EQ_Driver_Package\2018-10-15\Drivers" -FileName "C:\Temp\20EQ_Driver_Package\2018-10-15\Drivers.zip"