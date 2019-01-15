namespace DriverTool
module DirectoryOperations =
    type DirectoryOperations = class end
    open System

    let logger = Logging.getLoggerByName("DirectoryOperations")

    let createDirectoryUnsafe (directoryPath:Path) =
        System.IO.Directory.CreateDirectory(directoryPath.Value) |> ignore
        directoryPath

    let createDirectory (directoryPath:Path) =
        try
            Result.Ok (createDirectoryUnsafe directoryPath)
        with
        | ex -> Result.Error (new Exception(String.Format("Failed to create directory '{0}'",directoryPath.Value),ex))
    
    let deleteDirectoryUnsafe (force ,folderPath:Path) =
            match (System.IO.Directory.Exists(folderPath.Value)) with
            |true -> System.IO.Directory.Delete(folderPath.Value, force)
            |false -> ()
     
    let deleteDirectory force (folderPath:Path) =
        tryCatch deleteDirectoryUnsafe (force, folderPath)

    let directoryPathExists (directoryPath:Path) =
        System.IO.Directory.Exists(directoryPath.Value)

    let ensureDirectoryExistsWithMessage (message,directoryPath:Path, createIfNotExists) =
        let directoryExists = 
            directoryPathExists directoryPath
        match (not directoryExists && createIfNotExists) with
        |true->
            logger.InfoFormat("Creating directory: '{0}'...",directoryPath.Value)
            createDirectory directoryPath
        |false->
           match directoryExists with
           | true -> Result.Ok directoryPath
           | false -> Result.Error (new Exception(String.Format("Directory not found: '{0}'. {1}", directoryPath.Value, message)))

    let ensureDirectoryExists (directoryPath:Path, createIfNotExists) =
        ensureDirectoryExistsWithMessage (String.Empty,directoryPath,createIfNotExists)
    
    let directoryIsEmpty (directoryPath:Path) =
         match (directoryPathExists directoryPath) with
         |true ->
             let isEmpty = not (System.IO.Directory.GetDirectories(directoryPath.Value).Length > 0 || System.IO.Directory.GetFiles(directoryPath.Value).Length > 0)
             isEmpty
         |false -> true
    
    let ensureDirectoryExistsAndIsEmptyWithMessage  message (directoryPath:Path) createIfNotExists =
        match (ensureDirectoryExists (directoryPath, createIfNotExists)) with
        |Ok dp -> 
            match (directoryIsEmpty dp) with
            |true -> Result.Ok dp
            |false -> Result.Error (new Exception(String.Format("Directory '{0}' is not empty. " + message, dp.Value)))
        |Error ex -> Result.Error ex

    let ensureDirectoryExistsAndIsEmpty (directoryPath:Path, createIfNotExists) =
        ensureDirectoryExistsAndIsEmptyWithMessage String.Empty directoryPath createIfNotExists

    let ensureDirectoryNotExistsWithMessage message (directoryPath:Path) =
        match directoryPathExists(directoryPath) with
        |true -> Result.Error (new Exception(String.Format("Directory '{0}' allready exists. " + message, directoryPath.Value)))
        |false -> Result.Ok directoryPath
    
    let getParentFolderPath (folderPath:Path)=        
        Path.create (System.IO.DirectoryInfo(folderPath.Value).Parent.FullName)
    
    let getSubDirectoriesUnsafe directory =
        System.IO.Directory.GetDirectories(directory)
    
    let getSubDirectories directory =
        tryCatch getSubDirectoriesUnsafe directory

    let getSubDirectoryPaths (directoryPath:Path) =
        result{
            let! existingDirectoryPath = ensureDirectoryExists (directoryPath, false)
            let! subDirectories = getSubDirectories existingDirectoryPath.Value
            let! subDirectoryPaths = 
                    subDirectories
                    |>Seq.map(fun subDirectory -> Path.create subDirectory)
                    |>toAccumulatedResult
            return subDirectoryPaths
        }
        