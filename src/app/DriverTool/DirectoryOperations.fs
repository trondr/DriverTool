namespace DriverTool
module DirectoryOperations =
    type DirectoryOperations = class end
    open System
    
    let logger = Logging.getLoggerByName("DirectoryOperations")

    let createDirectoryUnsafe (directoryPath:FileSystem.Path) =
        System.IO.Directory.CreateDirectory(FileSystem.pathValue directoryPath) |> ignore
        directoryPath

    let createDirectory (directoryPath:FileSystem.Path) =
        try
            Result.Ok (createDirectoryUnsafe directoryPath)
        with
        | ex -> Result.Error (new Exception(String.Format("Failed to create directory '{0}'",directoryPath),ex))
    
    let deleteDirectoryUnsafe (force ,folderPath:FileSystem.Path) =
            match (System.IO.Directory.Exists(FileSystem.pathValue folderPath)) with
            |true -> System.IO.Directory.Delete(FileSystem.pathValue folderPath, force)
            |false -> ()
     
    let deleteDirectory force (folderPath:FileSystem.Path) =
        tryCatch deleteDirectoryUnsafe (force, folderPath)

    let directoryPathExists (directoryPath:FileSystem.Path) =
        System.IO.Directory.Exists(FileSystem.pathValue directoryPath)

    let ensureDirectoryExistsWithMessage (message,directoryPath:FileSystem.Path, createIfNotExists) =
        let directoryExists = 
            directoryPathExists directoryPath
        match (not directoryExists && createIfNotExists) with
        |true->
            logger.InfoFormat("Creating directory: '{0}'...",directoryPath)
            createDirectory directoryPath
        |false->
           match directoryExists with
           | true -> Result.Ok directoryPath
           | false -> Result.Error (new Exception(String.Format("Directory not found: '{0}'. {1}", directoryPath, message)))

    let ensureDirectoryExists (directoryPath:FileSystem.Path, createIfNotExists) =
        ensureDirectoryExistsWithMessage (String.Empty,directoryPath,createIfNotExists)
    
    let directoryIsEmpty (directoryPath:FileSystem.Path) =
         match (directoryPathExists directoryPath) with
         |true ->
             let isEmpty = not (System.IO.Directory.GetDirectories(FileSystem.pathValue directoryPath).Length > 0 || System.IO.Directory.GetFiles(FileSystem.pathValue directoryPath).Length > 0)
             isEmpty
         |false -> true
    
    let ensureDirectoryExistsAndIsEmptyWithMessage  message (directoryPath:FileSystem.Path) createIfNotExists =
        match (ensureDirectoryExists (directoryPath, createIfNotExists)) with
        |Ok dp -> 
            match (directoryIsEmpty dp) with
            |true -> Result.Ok dp
            |false -> Result.Error (new Exception(String.Format("Directory '{0}' is not empty. " + message, FileSystem.pathValue dp)))
        |Error ex -> Result.Error ex

    let ensureDirectoryExistsAndIsEmpty (directoryPath:FileSystem.Path, createIfNotExists) =
        ensureDirectoryExistsAndIsEmptyWithMessage String.Empty directoryPath createIfNotExists

    let ensureDirectoryNotExistsWithMessage message (directoryPath:FileSystem.Path) =
        match directoryPathExists(directoryPath) with
        |true -> Result.Error (new Exception(String.Format("Directory '{0}' allready exists. " + message, FileSystem.pathValue directoryPath)))
        |false -> Result.Ok directoryPath
    
    let getParentFolderPath (folderPath:FileSystem.Path)=        
        FileSystem.path (System.IO.DirectoryInfo(FileSystem.pathValue folderPath).Parent.FullName)
    
    let getSubDirectoriesUnsafe directory =
        System.IO.Directory.GetDirectories(directory)
    
    let getSubDirectories directory =
        tryCatch getSubDirectoriesUnsafe directory

    let getSubDirectoryPaths (directoryPath:FileSystem.Path) =
        result{
            let! existingDirectoryPath = ensureDirectoryExists (directoryPath, false)
            let! subDirectories = getSubDirectories (FileSystem.pathValue existingDirectoryPath)
            let! subDirectoryPaths = 
                    subDirectories
                    |>Seq.map(fun subDirectory -> FileSystem.path subDirectory)
                    |>toAccumulatedResult
            return subDirectoryPaths
        }
        