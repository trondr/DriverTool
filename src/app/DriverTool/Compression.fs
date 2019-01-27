namespace DriverTool

module Compression =
    open System
    
    let extract7ZipExeFromEmbededResource destinationFolderPath =
        EmbeddedResouce.extractEmbeddedResouceByFileName ("7za.exe",destinationFolderPath,"7za.exe")

    let unzipFile (zipFile, destinationFolderPath) =
        result{            
            let! existingZipFilePath = FileOperations.ensureFileExistsWithMessage (sprintf "Zip file '%s' not found." (FileSystem.pathValue zipFile)) zipFile
            let! existingAndEmptyDestinationFolderPath = DirectoryOperations.ensureDirectoryExistsAndIsEmptyWithMessage (sprintf "Cannot unzip '%s' to an allready existing folder '%s'." (FileSystem.pathValue zipFile) (FileSystem.pathValue destinationFolderPath)) destinationFolderPath true
            let! parentDestinationFolderPath = (DirectoryOperations.getParentFolderPath destinationFolderPath)
            let! sevenZipExeFilePath = extract7ZipExeFromEmbededResource parentDestinationFolderPath
            let! existing7ZipExeFilePath = FileOperations.ensureFileExistsWithMessage (sprintf "%s not found." (FileSystem.pathValue sevenZipExeFilePath)) sevenZipExeFilePath
            let! sevenZipExitCode = ProcessOperations.startConsoleProcess (existing7ZipExeFilePath, sprintf "x \"%s\"  -o\"%s\" -y" (FileSystem.pathValue existingZipFilePath) (FileSystem.pathValue existingAndEmptyDestinationFolderPath),FileSystem.pathValue existingAndEmptyDestinationFolderPath,-1,null,null,false)
            let! cleanupResult = FileOperations.deleteFile existing7ZipExeFilePath
            return sevenZipExitCode
        }
        
    let zipFolder (sourceFolderPath, zipFile) =
        result{
            let! nonExistingZipFilePath = FileOperations.ensureFileDoesNotExistWithMessage (sprintf "Zip file allready exists: '%s'" (FileSystem.pathValue zipFile), false, zipFile)
            let! existingSourceFolderPath = DirectoryOperations.ensureDirectoryExistsWithMessage false (sprintf "Cannot zip down a non existing directory '%A'." sourceFolderPath) sourceFolderPath
            let! parentSourceFolderPath = (DirectoryOperations.getParentFolderPath sourceFolderPath)
            let! sevenZipExeFilePath = extract7ZipExeFromEmbededResource parentSourceFolderPath
            let! existing7ZipExeFilePath = FileOperations.ensureFileExistsWithMessage (sprintf "%A not found." sevenZipExeFilePath) sevenZipExeFilePath
            let! sevenZipExitCode = ProcessOperations.startConsoleProcess (existing7ZipExeFilePath, sprintf "a \"%s\" \"%s\\*\"" (FileSystem.pathValue nonExistingZipFilePath)  (FileSystem.pathValue existingSourceFolderPath),FileSystem.pathValue existingSourceFolderPath,-1,null,null,false)
            let! cleanupResult = FileOperations.deleteFile existing7ZipExeFilePath
            return sevenZipExitCode
        }