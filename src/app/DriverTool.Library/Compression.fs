﻿namespace DriverTool.Library

module Compression =    
    open DriverTool.Library.Logging
    let logger = getLoggerByName "Compression"
    open DriverTool.Library.F
    open DriverTool.Library

    let unzipFile (zipFile, destinationFolderPath, logger:Common.Logging.ILog) =
        result{            
            logger.Info(sprintf "Unzipping file: %A -> %A" zipFile destinationFolderPath)
            let! existingZipFilePath = FileOperations.ensureFileExistsWithMessage (sprintf "Zip file '%s' not found." (FileSystem.pathValue zipFile)) zipFile
            let! existingAndEmptyDestinationFolderPath = DirectoryOperations.ensureDirectoryExistsAndIsEmptyWithMessage (sprintf "Cannot unzip '%s' to an allready existing folder '%s'." (FileSystem.pathValue zipFile) (FileSystem.pathValue destinationFolderPath)) destinationFolderPath true            
            let resourceAssembly = System.Reflection.Assembly.GetExecutingAssembly()
            use sevenZipExe = new EmbeddedResource.ExtractedEmbeddedResourceByFileName(resourceAssembly,"7za.exe",logger)
            let! sevenZipExeFilePath = sevenZipExe.FilePath
            let! existing7ZipExeFilePath = FileOperations.ensureFileExistsWithMessage (sprintf "%s not found." (FileSystem.pathValue sevenZipExeFilePath)) sevenZipExeFilePath
            let! sevenZipExitCode = ProcessOperations.startConsoleProcess (existing7ZipExeFilePath, sprintf "x \"%s\"  -o\"%s\" -y" (FileSystem.pathValue existingZipFilePath) (FileSystem.pathValue existingAndEmptyDestinationFolderPath),FileSystem.pathValue existingAndEmptyDestinationFolderPath,-1,null,null,false)            
            logger.Info(sprintf "Finished unzipping file: %A -> %A. ExitCode: %i" zipFile destinationFolderPath sevenZipExitCode)
            return sevenZipExitCode
        }
        
    let zipFolder (sourceFolderPath, zipFile, logger:Common.Logging.ILog) =
        result{
            logger.Info(sprintf "Compressing folder: %A -> %A" sourceFolderPath zipFile)
            let! nonExistingZipFilePath = FileOperations.ensureFileDoesNotExistWithMessage (sprintf "Zip file allready exists: '%s'" (FileSystem.pathValue zipFile)) false zipFile
            let! existingSourceFolderPath = DirectoryOperations.ensureDirectoryExistsWithMessage false (sprintf "Cannot zip down a non existing directory '%A'." sourceFolderPath) sourceFolderPath            
            let resourceAssembly = System.Reflection.Assembly.GetExecutingAssembly()
            use sevenZipExe = new EmbeddedResource.ExtractedEmbeddedResourceByFileName(resourceAssembly,"7za.exe",logger)
            let! sevenZipExeFilePath = sevenZipExe.FilePath
            let! existing7ZipExeFilePath = FileOperations.ensureFileExistsWithMessage (sprintf "%s not found." (FileSystem.pathValue sevenZipExeFilePath)) sevenZipExeFilePath
            let! sevenZipExitCode = ProcessOperations.startConsoleProcess (existing7ZipExeFilePath, sprintf "a \"%s\" \"%s\\*\"" (FileSystem.pathValue nonExistingZipFilePath)  (FileSystem.pathValue existingSourceFolderPath),FileSystem.pathValue existingSourceFolderPath,-1,null,null,false)
            logger.Info(sprintf "Finished compressing folder: %A -> %A. ExitCode: %i" sourceFolderPath zipFile sevenZipExitCode)
            return sevenZipExitCode
        }