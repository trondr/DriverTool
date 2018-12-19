namespace DriverTool

module CompressDriverPackage =
    let logger = Logging.getLoggerByName("InstallDriverPackage")
    open System

    let compressDriverPackage (driverPackagePath:Path) =
        result{
            let! existingDriverPackagePath = DirectoryOperations.ensureDirectoryExistsWithMessage (String.Format("Failed to compress driver package as the driver package folder does not exist."),driverPackagePath,false)
            let! driversFolderPath = Path.create (System.IO.Path.Combine(existingDriverPackagePath.Value,"Drivers"))
            let! existingDriversFolderPath = DirectoryOperations.ensureDirectoryExistsWithMessage (String.Format("Failed to compress Drivers folder as the folder does not exist."),driversFolderPath,false)
            let! driversZipFilepath = Path.create (System.IO.Path.Combine(driverPackagePath.Value,"Drivers.zip"))
            let! nonExistingDriversZipFilePath = FileOperations.ensureFileDoesNotExistWithMessage (String.Format("Failed to compress Drivers folder as the Drivers.zip allready exist."),false,driversZipFilepath)
            let! zipResult = Compression.zipFolder (existingDriversFolderPath,nonExistingDriversZipFilePath)
            logger.Warn(String.Format("The Drivers folder '{0}' can now be deleted manually.",driversFolderPath.Value))
            return zipResult
        }
    
    let decompressDriverPackage (driverPackagePath:Path) =
        result{
            let! existingDriverPackagePath = DirectoryOperations.ensureDirectoryExistsWithMessage (String.Format("Failed to decompress driver package as the driver package folder does not exist."),driverPackagePath,false)
            let! driversFolderPath = Path.create (System.IO.Path.Combine(existingDriverPackagePath.Value,"Drivers"))
            let! nonexistingDriversFolderPath = DirectoryOperations.ensureDirectoryNotExistsWithMessage (String.Format("Failed to decompress Drivers.zip to Drivers folder as the folder allready exists.")) driversFolderPath
            let! driversZipFilepath = Path.create (System.IO.Path.Combine(driverPackagePath.Value,"Drivers.zip"))
            let! existingDriversZipFilePath = FileOperations.ensureFileExistsWithMessage (String.Format("Failed to decompress Drivers.zip as the file does not exist.")) driversZipFilepath
            let! zipResult = Compression.unzipFile (existingDriversZipFilePath, nonexistingDriversFolderPath)        
            return zipResult
        }
