﻿namespace DriverTool

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
            let! sevenZipExitCode = ProcessOperations.startConsoleProcess (existing7ZipExeFilePath.Value, String.Format("x \"{0}\"  -o\"{1}\" -y", existingZipFilePath.Value, existingAndEmptyDestinationFolderPath.Value),existingAndEmptyDestinationFolderPath.Value,null,false)
            let! cleanupResult = FileOperations.deleteFile existing7ZipExeFilePath
            return sevenZipExitCode
        }
        
