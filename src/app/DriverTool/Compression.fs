namespace DriverTool

module Compression =
    open System
    open DriverTool.Library.Logging
    let logger = getLoggerByName "Compression"

    let unzipFile (zipFile, destinationFolderPath, logger:Common.Logging.ILog) =
        result{            
            logger.Debug(new Msg(fun m -> m.Invoke((sprintf "Unzipping file: %A -> %A" zipFile destinationFolderPath))|>ignore))
            let! existingZipFilePath = FileOperations.ensureFileExistsWithMessage (sprintf "Zip file '%s' not found." (FileSystem.pathValue zipFile)) zipFile
            let! existingAndEmptyDestinationFolderPath = DirectoryOperations.ensureDirectoryExistsAndIsEmptyWithMessage (sprintf "Cannot unzip '%s' to an allready existing folder '%s'." (FileSystem.pathValue zipFile) (FileSystem.pathValue destinationFolderPath)) destinationFolderPath true            
            use sevenZipExe = new EmbeddedResource.ExtractedEmbeddedResourceByFileName("7za.exe",logger)
            let! sevenZipExeFilePath = sevenZipExe.FilePath
            let! existing7ZipExeFilePath = FileOperations.ensureFileExistsWithMessage (sprintf "%s not found." (FileSystem.pathValue sevenZipExeFilePath)) sevenZipExeFilePath
            let! sevenZipExitCode = ProcessOperations.startConsoleProcess (existing7ZipExeFilePath, sprintf "x \"%s\"  -o\"%s\" -y" (FileSystem.pathValue existingZipFilePath) (FileSystem.pathValue existingAndEmptyDestinationFolderPath),FileSystem.pathValue existingAndEmptyDestinationFolderPath,-1,null,null,false)            
            logger.Debug(new Msg(fun m -> m.Invoke((sprintf "Finished unzipping file: %A -> %A. ExitCode: %i" zipFile destinationFolderPath sevenZipExitCode))|>ignore))
            return sevenZipExitCode
        }
        
    let zipFolder (sourceFolderPath, zipFile, logger:Common.Logging.ILog) =
        result{
            logger.Debug(new Msg(fun m -> m.Invoke((sprintf "Compressing folder: %A -> %A" sourceFolderPath zipFile))|>ignore))
            let! nonExistingZipFilePath = FileOperations.ensureFileDoesNotExistWithMessage (sprintf "Zip file allready exists: '%s'" (FileSystem.pathValue zipFile)) false zipFile
            let! existingSourceFolderPath = DirectoryOperations.ensureDirectoryExistsWithMessage false (sprintf "Cannot zip down a non existing directory '%A'." sourceFolderPath) sourceFolderPath            
            use sevenZipExe = new EmbeddedResource.ExtractedEmbeddedResourceByFileName("7za.exe",logger)
            let! sevenZipExeFilePath = sevenZipExe.FilePath
            let! existing7ZipExeFilePath = FileOperations.ensureFileExistsWithMessage (sprintf "%s not found." (FileSystem.pathValue sevenZipExeFilePath)) sevenZipExeFilePath
            let! sevenZipExitCode = ProcessOperations.startConsoleProcess (existing7ZipExeFilePath, sprintf "a \"%s\" \"%s\\*\"" (FileSystem.pathValue nonExistingZipFilePath)  (FileSystem.pathValue existingSourceFolderPath),FileSystem.pathValue existingSourceFolderPath,-1,null,null,false)
            logger.Debug(new Msg(fun m -> m.Invoke((sprintf "Finished compressing folder: %A -> %A. ExitCode: %i" sourceFolderPath zipFile sevenZipExitCode))|>ignore))
            return sevenZipExitCode
        }