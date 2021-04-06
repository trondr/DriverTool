namespace DriverTool.Library

open System    
open DriverTool.Library.Paths
open DriverTool.Library.F

module DirectoryOperations =
    type DirectoryOperations = class end
    open System
    open System.IO
    open DriverTool.Library.Logging
        
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
        tryCatch2 (Some (sprintf "Failed to delete directory '%A'" folderPath)) deleteDirectoryUnsafe force folderPath

    let folderPathExists (directoryPath:FileSystem.Path) =
        System.IO.Directory.Exists(FileSystem.pathValue directoryPath)

    let ensureDirectoryExistsWithMessage createIfNotExists message directoryPath =
        let directoryExists = 
            folderPathExists directoryPath
        match (not directoryExists && createIfNotExists) with
        |true->
            logger.Info(sprintf  "Creating directory: '%s'..." (FileSystem.pathValue directoryPath))
            createDirectory directoryPath
        |false->
           match directoryExists with
           | true -> Result.Ok directoryPath
           | false -> Result.Error (new Exception(sprintf "Directory not found: '%s'. %s" (FileSystem.pathValue directoryPath) message))

    let ensureDirectoryExists createIfNotExists directoryPath =
        ensureDirectoryExistsWithMessage createIfNotExists String.Empty directoryPath
    
    let folderPathIsEmpty folderPath =
         match (folderPathExists folderPath) with
         |true ->
             let isEmpty = not (System.IO.Directory.GetDirectories(FileSystem.pathValue folderPath).Length > 0 || System.IO.Directory.GetFiles(FileSystem.pathValue folderPath).Length > 0)
             isEmpty
         |false -> true
    
    let ensureDirectoryExistsAndIsEmptyWithMessage  message (directoryPath:FileSystem.Path) createIfNotExists =
        match (ensureDirectoryExists createIfNotExists directoryPath) with
        |Ok dp -> 
            match (folderPathIsEmpty dp) with
            |true -> Result.Ok dp
            |false -> Result.Error (new Exception(sprintf "Directory '%s' is not empty. %s" (FileSystem.pathValue dp) message))
        |Result.Error ex -> Result.Error ex

    let ensureDirectoryExistsAndIsEmpty (directoryPath:FileSystem.Path, createIfNotExists) =
        ensureDirectoryExistsAndIsEmptyWithMessage String.Empty directoryPath createIfNotExists

    let optionToString message =
        match message with
        |Some m -> m
        |None -> String.Empty

    let ensureFolderPathIsEmpty' message folderPath =
        match folderPath with
        |Result.Error _ -> folderPath
        |Result.Ok fp ->
            match (folderPathIsEmpty fp) with
            |true -> Result.Ok fp
            |false -> toErrorResult (toException (sprintf "Folder '%A' is not empty." fp) None) message 

    let ensureFolderPathExists' force message folderPath =
        match folderPath with
        |Result.Error _ -> folderPath
        |Result.Ok fp ->
            match (folderPathExists fp) with
            |true -> Result.Ok fp
            |false -> 
                match force with
                |true -> createDirectory fp
                |false ->            
                    toErrorResult (toException (sprintf "Folder path  '%A' does not exist" fp) None) message

    let ensurefolderPathExistsIsEmpty' force (message:string option) folderPath =
        folderPath
        |>ensureFolderPathExists' force message
        |>ensureFolderPathIsEmpty' message

    let ensureDirectoryNotExistsWithMessage message (directoryPath:FileSystem.Path) =
        match folderPathExists(directoryPath) with
        |true -> Result.Error (new Exception(sprintf "Directory '%s' allready exists. %s" (FileSystem.pathValue directoryPath) message))
        |false -> Result.Ok directoryPath
    
    let getParentFolderPath (folderPath:FileSystem.Path)=        
        FileSystem.path (System.IO.DirectoryInfo(FileSystem.pathValue folderPath).Parent.FullName)

    let getParentFolderPathUnsafe (folderPath:FileSystem.Path)=        
        FileSystem.pathUnSafe (System.IO.DirectoryInfo(FileSystem.pathValue folderPath).Parent.FullName)
    
    let getSubDirectoriesUnsafe directory =
        System.IO.Directory.GetDirectories(directory)
    
    let getSubDirectories directory =
        tryCatch (Some(sprintf "Failed to get sub directories: '%s'" directory)) getSubDirectoriesUnsafe directory

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
        match (folderPathExists folderPath) with
        |true -> 
            deleteDirectory true folderPath                                    
        |false -> 
            Result.Ok folderPath

    let moveDirectoryUnsafe sourceFolderPath destinationFolderPath =
        try
            createDirectoryUnsafe (getParentFolderPathUnsafe destinationFolderPath) |> ignore
            System.IO.Directory.Move(FileSystem.pathValue sourceFolderPath,FileSystem.pathValue destinationFolderPath)
        with
        |ex ->
            logger.Warn(getAccumulatedExceptionMessages ex)
            reraise()

    let moveDirectory sourceFolderPath destinationFolderPath =
        tryCatch2 None moveDirectoryUnsafe sourceFolderPath destinationFolderPath
        
    [<AllowNullLiteral>]
    type TemporaryFolder(logger:Common.Logging.ILog)=
        let temporaryFolderPath = 
            result {
                let! nonExistingTempFolderPath = FileSystem.path (System.IO.Path.Combine(System.IO.Path.GetTempPath(),"DT",(Guid.NewGuid().ToString())))
                let! existingTempFolderPath = ensureDirectoryExists true nonExistingTempFolderPath
                return existingTempFolderPath
            }

        member this.FolderPath = temporaryFolderPath
        interface IDisposable with
            member x.Dispose() = 
                if(logger.IsDebugEnabled) then ( logger.Debug(sprintf "Disposing folder '%A'" temporaryFolderPath))
                match (result{
                    let! folderPath = temporaryFolderPath
                    let! deleted = deleteDirectory true folderPath
                    return deleted                
                }) with
                |Result.Ok v -> ()
                |Result.Error ex -> raise ex
     