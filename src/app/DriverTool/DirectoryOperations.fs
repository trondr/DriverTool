namespace DriverTool
module DirectoryOperations =
    type DirectoryOperations = class end
    open System
    open System.IO
    open DriverTool.Logging
    
    let logger = Logging.getLoggerByName("DirectoryOperations")

    let createDirectoryUnsafe (directoryPath:FileSystem.Path) =
        System.IO.Directory.CreateDirectory(FileSystem.pathValue directoryPath) |> ignore
        directoryPath

    let createDirectory (directoryPath:FileSystem.Path) =
        try
            Result.Ok (createDirectoryUnsafe directoryPath)
        with
        | ex -> Result.Error (new Exception(sprintf "Failed to create directory '%s'" (FileSystem.pathValue directoryPath),ex))
    
    let deleteDirectoryUnsafe force (folderPath:FileSystem.Path) =
            match (System.IO.Directory.Exists(FileSystem.pathValue folderPath)) with
            |true -> 
                System.IO.Directory.Delete(FileSystem.pathValue folderPath, force)
                folderPath
            |false -> 
                folderPath
     
    let deleteDirectory force (folderPath:FileSystem.Path) =
        tryCatch2 deleteDirectoryUnsafe force folderPath

    let directoryPathExists (directoryPath:FileSystem.Path) =
        System.IO.Directory.Exists(FileSystem.pathValue directoryPath)

    let ensureDirectoryExistsWithMessage createIfNotExists message directoryPath =
        let directoryExists = 
            directoryPathExists directoryPath
        match (not directoryExists && createIfNotExists) with
        |true->
            logger.Info(msg (sprintf  "Creating directory: '%s'..." (FileSystem.pathValue directoryPath)))
            createDirectory directoryPath
        |false->
           match directoryExists with
           | true -> Result.Ok directoryPath
           | false -> Result.Error (new Exception(sprintf "Directory not found: '%s'. %s" (FileSystem.pathValue directoryPath) message))

    let ensureDirectoryExists createIfNotExists directoryPath =
        ensureDirectoryExistsWithMessage createIfNotExists String.Empty directoryPath
    
    let directoryIsEmpty directoryPath =
         match (directoryPathExists directoryPath) with
         |true ->
             let isEmpty = not (System.IO.Directory.GetDirectories(FileSystem.pathValue directoryPath).Length > 0 || System.IO.Directory.GetFiles(FileSystem.pathValue directoryPath).Length > 0)
             isEmpty
         |false -> true
    
    let ensureDirectoryExistsAndIsEmptyWithMessage  message (directoryPath:FileSystem.Path) createIfNotExists =
        match (ensureDirectoryExists createIfNotExists directoryPath) with
        |Ok dp -> 
            match (directoryIsEmpty dp) with
            |true -> Result.Ok dp
            |false -> Result.Error (new Exception(sprintf "Directory '%s' is not empty. %s" (FileSystem.pathValue dp) message))
        |Result.Error ex -> Result.Error ex

    let ensureDirectoryExistsAndIsEmpty (directoryPath:FileSystem.Path, createIfNotExists) =
        ensureDirectoryExistsAndIsEmptyWithMessage String.Empty directoryPath createIfNotExists

    let ensureDirectoryNotExistsWithMessage message (directoryPath:FileSystem.Path) =
        match directoryPathExists(directoryPath) with
        |true -> Result.Error (new Exception(sprintf "Directory '%s' allready exists. %s" (FileSystem.pathValue directoryPath) message))
        |false -> Result.Ok directoryPath
    
    let getParentFolderPath (folderPath:FileSystem.Path)=        
        FileSystem.path (System.IO.DirectoryInfo(FileSystem.pathValue folderPath).Parent.FullName)
    
    let getSubDirectoriesUnsafe directory =
        System.IO.Directory.GetDirectories(directory)
    
    let getSubDirectories directory =
        tryCatch getSubDirectoriesUnsafe directory

    let getSubDirectoryPaths directoryPath =
        result{
            let! existingDirectoryPath = ensureDirectoryExists false directoryPath
            let! subDirectories = getSubDirectories (FileSystem.pathValue existingDirectoryPath)
            let! subDirectoryPaths = 
                    subDirectories
                    |>Seq.map(fun subDirectory -> FileSystem.path subDirectory)
                    |>toAccumulatedResult
            return subDirectoryPaths
        }

    let toSearchOptions recurse =
        match recurse with
        |false -> System.IO.SearchOption.TopDirectoryOnly
        |true -> System.IO.SearchOption.AllDirectories
    
    let getFilesUnsafe recurse directoryPath =
        let searchOptions = toSearchOptions recurse                
        System.IO.Directory.GetFiles(FileSystem.pathValue directoryPath,"*.*",searchOptions)

    let getFiles recurse directoryPath =
        try
            (getFilesUnsafe recurse directoryPath)
            |>Seq.map(fun fn -> FileSystem.path fn)
            |>toAccumulatedResult
        with
        |ex -> Result.Error (new Exception(sprintf "Failed to get files in folder '%s' due to: %s" (FileSystem.pathValue directoryPath) ex.Message,ex))

    let findFilesUnsafe recurse searchPattern folder =
        let searchOptions = toSearchOptions recurse
        System.IO.Directory.GetFiles(folder, searchPattern,searchOptions)

    let findFiles recurse searchPattern folder =
        try
            (findFilesUnsafe recurse searchPattern (FileSystem.pathValue folder))
            |>Seq.map(fun fn -> FileSystem.path fn)
            |>toAccumulatedResult
        with
        |ex -> Result.Error (new Exception(sprintf "Failed to find files in folder '%s' due to: %s" (FileSystem.pathValue folder) ex.Message,ex))
    
    let deleteDirectoryIfExists folderPath =
        match (directoryPathExists folderPath) with
        |true -> 
            deleteDirectory true folderPath                                    
        |false -> 
            Result.Ok folderPath

    [<AllowNullLiteral>]
    type TemporaryFolder()=
        let temporaryFolderPath = 
            result {
                let! nonExistingTempFolderPath = FileSystem.path (System.IO.Path.Combine(System.IO.Path.GetTempPath(),(Guid.NewGuid().ToString())))
                let! existingTempFolderPath = ensureDirectoryExists true nonExistingTempFolderPath
                return existingTempFolderPath
            }

        member this.FolderPath = temporaryFolderPath
        interface IDisposable with
            member x.Dispose() = 
                logger.Debug(new Msg(fun m -> m.Invoke((sprintf "Disposing folder '%A'" temporaryFolderPath))|>ignore))
                match (result{
                    let! folderPath = temporaryFolderPath
                    let! deleted = deleteDirectoryIfExists folderPath
                    return deleted                                                
                }) with
                |Result.Ok v -> ()
                |Result.Error ex -> raise ex