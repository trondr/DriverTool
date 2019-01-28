namespace DriverTool

module CompressDriverPackage =
    let logger = Logging.getLoggerByName("InstallDriverPackage")
    open System

    let compressDriverPackage (driverPackagePath:FileSystem.Path) =
        result{
            let! existingDriverPackagePath = DirectoryOperations.ensureDirectoryExistsWithMessage false (String.Format("Failed to compress driver package as the driver package folder does not exist.")) driverPackagePath
            let! driversFolderPath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue existingDriverPackagePath,"Drivers"))
            let! existingDriversFolderPath = DirectoryOperations.ensureDirectoryExistsWithMessage false (String.Format("Failed to compress Drivers folder as the folder does not exist.")) driversFolderPath
            let! driversZipFilepath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue driverPackagePath,"Drivers.zip"))
            let! nonExistingDriversZipFilePath = FileOperations.ensureFileDoesNotExistWithMessage (String.Format("Failed to compress Drivers folder as the Drivers.zip allready exist.")) false driversZipFilepath
            let! zipResult = Compression.zipFolder (existingDriversFolderPath,nonExistingDriversZipFilePath)
            logger.Warn(String.Format("The Drivers folder '{0}' can now be deleted manually.",driversFolderPath))
            return zipResult
        }
    
    let decompressDriverPackage (driverPackagePath:FileSystem.Path) =
        result{
            let! existingDriverPackagePath = 
                DirectoryOperations.ensureDirectoryExistsWithMessage false (String.Format("Failed to decompress driver package as the driver package folder does not exist.")) driverPackagePath
            let! driversFolderPath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue existingDriverPackagePath,"Drivers"))
            let! nonexistingDriversFolderPath = DirectoryOperations.ensureDirectoryNotExistsWithMessage (String.Format("Failed to decompress Drivers.zip to Drivers folder as the folder allready exists.")) driversFolderPath
            let! driversZipFilepath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue driverPackagePath,"Drivers.zip"))
            let! existingDriversZipFilePath = FileOperations.ensureFileExistsWithMessage (String.Format("Failed to decompress Drivers.zip as the file does not exist.")) driversZipFilepath
            let! zipResult = Compression.unzipFile (existingDriversZipFilePath, nonexistingDriversFolderPath)        
            return zipResult
        }
