namespace DriverTool

module Compression =
    open System
    
    let extract7ZipExeFromEmbededResource destinationFolderPath =
        EmbeddedResouce.extractEmbeddedResouceByFileName ("7za.exe",destinationFolderPath,"7za.exe")

    let unzipFile (zipFile:Path, destinationFolderPath:Path) =
        result{
            
            let! existingZipFilePath = FileOperations.ensureFileExistsWithMessage (String.Format("Zip file '{0}' not found.",zipFile.Value)) zipFile
            let! existingAndEmptyDestinationFolderPath = DirectoryOperations.ensureDirectoryExistsAndIsEmptyWithMessage (String.Format("Cannot unzip '{0}' to an allready existing folder '{0}'.",zipFile.Value,destinationFolderPath.Value)) destinationFolderPath true
            let! parentDestinationFolderPath = (DirectoryOperations.getParentFolderPath destinationFolderPath)
            let! sevenZipExeFilePath = extract7ZipExeFromEmbededResource parentDestinationFolderPath
            let! existing7ZipExeFilePath = FileOperations.ensureFileExistsWithMessage (String.Format("{0} not found.",sevenZipExeFilePath.Value)) sevenZipExeFilePath
            let! sevenZipExitCode = ProcessOperations.startConsoleProcess (existing7ZipExeFilePath.Value, String.Format("x \"{0}\"  -o\"{1}\" -y", existingZipFilePath.Value, existingAndEmptyDestinationFolderPath.Value),existingAndEmptyDestinationFolderPath.Value,-1,null,null,false)
            let! cleanupResult = FileOperations.deleteFile existing7ZipExeFilePath
            return sevenZipExitCode
        }
        
    let zipFolder (sourceFolderPath:Path, zipFile:Path) =
        result{
            let! nonExistingZipFilePath = FileOperations.ensureFileDoesNotExistWithMessage ((String.Format("Zip file '{0}' not found.",zipFile.Value)),false,zipFile)
            let! existingSourceFolderPath = DirectoryOperations.ensureDirectoryExistsWithMessage ( (String.Format("Cannot zip down a non existing directory '{0}'.",sourceFolderPath.Value)), sourceFolderPath, false)
            let! parentSourceFolderPath = (DirectoryOperations.getParentFolderPath sourceFolderPath)
            let! sevenZipExeFilePath = extract7ZipExeFromEmbededResource parentSourceFolderPath
            let! existing7ZipExeFilePath = FileOperations.ensureFileExistsWithMessage (String.Format("{0} not found.",sevenZipExeFilePath.Value)) sevenZipExeFilePath
            let! sevenZipExitCode = ProcessOperations.startConsoleProcess (existing7ZipExeFilePath.Value, String.Format("a \"{0}\" \"{1}\\*\"", nonExistingZipFilePath.Value, existingSourceFolderPath.Value),existingSourceFolderPath.Value,-1,null,null,false)
            let! cleanupResult = FileOperations.deleteFile existing7ZipExeFilePath
            return sevenZipExitCode
        }