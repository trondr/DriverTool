namespace DriverTool

module Robocopy=
    open System

    let logger = Logging.getLoggerByName("Robocopy")

    let roboCopyExitCode2ExitCode roboCopyExitCode =
        match roboCopyExitCode with
        | 16 ->
            logger.Error("Robocopy result 16: ***FATAL ERROR***")
            Result.Ok 1
        | 15 ->
            logger.Error("Robocopy result 15: OKCOPY + FAIL + MISMATCHES + XTRA")
            Result.Ok 1
        | 14 ->
            logger.Error("Robocopy result 14: FAIL + MISMATCHES + XTRA")
            Result.Ok 1
        | 13 ->
            logger.Error("Robocopy result 13: OKCOPY + FAIL + MISMATCHES")
            Result.Ok 1
        | 12 ->
            logger.Error("Robocopy result 12: FAIL + MISMATCHES")
            Result.Ok 1
        | 11 ->
            logger.Error("Robocopy result 11: OKCOPY + FAIL + XTRA")
            Result.Ok 1
        | 10 ->
            logger.Error("Robocopy result 10: FAIL + XTRA")
            Result.Ok 1
        | 9 ->
            logger.Error("Robocopy result 9: OKCOPY + FAIL")
            Result.Ok 1
        | 8 ->
            logger.Error("Robocopy result 8: FAIL")
            Result.Ok 1
        | 7 ->
            logger.Info("Robocopy result 7: OKCOPY + MISMATCHES + XTRA")
            Result.Ok 0
        | 6 ->
            logger.Info("Robocopy result 6: MISMATCHES + XTRA")
            Result.Ok 0
        | 5 ->
            logger.Info("Robocopy result 5: OKCOPY + MISMATCHES")
            Result.Ok 0
        | 4 ->
            logger.Info("Robocopy result 4: MISMATCHES")
            Result.Ok 0
        | 3 ->
            logger.Info("Robocopy result 3: OKCOPY + XTRA")
            Result.Ok 0
        | 2 ->
            logger.Info("Robocopy result 2: XTRA")
            Result.Ok 0
        | 1 ->
            logger.Info("Robocopy result 1: OKCOPY")
            Result.Ok 0
        | 0 ->
            logger.Info("Robocopy result 0: No Change")
            Result.Ok 0
        | _ -> Result.Error (new Exception(sprintf "Unknown Robocopy exit code: %i" roboCopyExitCode))
    
    open System
    open DriverTool.Environment


    let roboCopyExe =
        System.IO.Path.Combine(nativeSystemFolder,"Robocopy.exe")
        
    let roboCopy (sourceFolderPath:FileSystem.Path, destinationFolderPath:FileSystem.Path, robocopyOptions:string) =
        result{
            let! roboCopyExeFilePath = FileSystem.path roboCopyExe
            let! existingRobocopyExeFilePath = FileOperations.ensureFileExists roboCopyExeFilePath
            let! existingSourceFolderPath = DirectoryOperations.ensureDirectoryExists false sourceFolderPath
            let arguments = sprintf "\"%s\" \"%s\" %s" (FileSystem.pathValue existingSourceFolderPath) (FileSystem.pathValue destinationFolderPath) robocopyOptions
            let! roboCopyExitCode = ProcessOperations.startConsoleProcess (existingRobocopyExeFilePath,arguments,FileSystem.pathValue existingSourceFolderPath,-1,null,null,false)
            let! exitCode = roboCopyExitCode2ExitCode roboCopyExitCode
            let! copyResult = 
                match exitCode with
                |0 -> Result.Ok 0
                |_ -> Result.Error (new Exception(sprintf "Failed to copy '%s' -> '%s'. Robocopy returned: %i" (FileSystem.pathValue existingSourceFolderPath) (FileSystem.pathValue destinationFolderPath) roboCopyExitCode))
            return copyResult
        }
        
        
