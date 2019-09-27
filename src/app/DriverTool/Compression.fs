namespace DriverTool

module Compression =
    open System
    let logger = Logging.getLoggerByName "Compression"

    let extract7ZipExeFromEmbededResource destinationFolderPath =
        EmbeddedResource.extractEmbeddedResouceByFileName ("7za.exe",destinationFolderPath,"7za.exe")

    let unzipFile (zipFile, destinationFolderPath) =
        result{            
            let! existingZipFilePath = FileOperations.ensureFileExistsWithMessage (sprintf "Zip file '%s' not found." (FileSystem.pathValue zipFile)) zipFile
            let! existingAndEmptyDestinationFolderPath = DirectoryOperations.ensureDirectoryExistsAndIsEmptyWithMessage (sprintf "Cannot unzip '%s' to an allready existing folder '%s'." (FileSystem.pathValue zipFile) (FileSystem.pathValue destinationFolderPath)) destinationFolderPath true            
            use sevenZipExe = new EmbeddedResource.ExtractedEmbeddedResource("7za.exe",logger)
            let! sevenZipExeFilePath = sevenZipExe.FilePath
            let! existing7ZipExeFilePath = FileOperations.ensureFileExistsWithMessage (sprintf "%s not found." (FileSystem.pathValue sevenZipExeFilePath)) sevenZipExeFilePath
            let! sevenZipExitCode = ProcessOperations.startConsoleProcess (existing7ZipExeFilePath, sprintf "x \"%s\"  -o\"%s\" -y" (FileSystem.pathValue existingZipFilePath) (FileSystem.pathValue existingAndEmptyDestinationFolderPath),FileSystem.pathValue existingAndEmptyDestinationFolderPath,-1,null,null,false)
            let! cleanupResult = FileOperations.deleteFile existing7ZipExeFilePath
            return sevenZipExitCode
        }
        
    let zipFolder (sourceFolderPath, zipFile) =
        result{
            let! nonExistingZipFilePath = FileOperations.ensureFileDoesNotExistWithMessage (sprintf "Zip file allready exists: '%s'" (FileSystem.pathValue zipFile)) false zipFile
            let! existingSourceFolderPath = DirectoryOperations.ensureDirectoryExistsWithMessage false (sprintf "Cannot zip down a non existing directory '%A'." sourceFolderPath) sourceFolderPath            
            use sevenZipExe = new EmbeddedResource.ExtractedEmbeddedResource("7za.exe",logger)
            let! sevenZipExeFilePath = sevenZipExe.FilePath
            let! existing7ZipExeFilePath = FileOperations.ensureFileExistsWithMessage (sprintf "%s not found." (FileSystem.pathValue sevenZipExeFilePath)) sevenZipExeFilePath
            let! sevenZipExitCode = ProcessOperations.startConsoleProcess (existing7ZipExeFilePath, sprintf "a \"%s\" \"%s\\*\"" (FileSystem.pathValue nonExistingZipFilePath)  (FileSystem.pathValue existingSourceFolderPath),FileSystem.pathValue existingSourceFolderPath,-1,null,null,false)
            let! cleanupResult = FileOperations.deleteFile existing7ZipExeFilePath
            return sevenZipExitCode
        }