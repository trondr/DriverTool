namespace DriverTool

module CompressDriverPackage =
    open DriverTool.Library.Logging
    let logger = getLoggerByName("InstallDriverPackage")
    open System

    let compressDriverPackage (driverPackagePath:FileSystem.Path) =
        result{
            let! existingDriverPackagePath = DirectoryOperations.ensureDirectoryExistsWithMessage false (sprintf "Failed to compress driver package as the driver package folder does not exist.") driverPackagePath
            let! driversFolderPath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue existingDriverPackagePath,"Drivers"))
            let! existingDriversFolderPath = DirectoryOperations.ensureDirectoryExistsWithMessage false (sprintf "Failed to compress Drivers folder as the folder does not exist.") driversFolderPath
            let! driversZipFilepath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue driverPackagePath,"Drivers.zip"))
            let! nonExistingDriversZipFilePath = FileOperations.ensureFileDoesNotExistWithMessage (sprintf "Failed to compress Drivers folder as the Drivers.zip allready exist.") false driversZipFilepath
            let! zipResult = Compression.zipFolder (existingDriversFolderPath,nonExistingDriversZipFilePath,logger)
            logger.Warn(msg (sprintf "The Drivers folder '%s' can now be deleted manually." (FileSystem.pathValue driversFolderPath)))
            return zipResult
        }
    
    let decompressDriverPackage (driverPackagePath:FileSystem.Path) =
        result{
            let! existingDriverPackagePath = 
                DirectoryOperations.ensureDirectoryExistsWithMessage false (sprintf "Failed to decompress driver package as the driver package folder does not exist.") driverPackagePath
            let! driversFolderPath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue existingDriverPackagePath,"Drivers"))
            let! nonexistingDriversFolderPath = DirectoryOperations.ensureDirectoryNotExistsWithMessage (sprintf "Failed to decompress Drivers.zip to Drivers folder as the folder allready exists.") driversFolderPath
            let! driversZipFilepath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue driverPackagePath,"Drivers.zip"))
            let! existingDriversZipFilePath = FileOperations.ensureFileExistsWithMessage (sprintf "Failed to decompress Drivers.zip as the file does not exist.") driversZipFilepath
            let! zipResult = Compression.unzipFile (existingDriversZipFilePath, nonexistingDriversFolderPath,logger)        
            return zipResult
        }
