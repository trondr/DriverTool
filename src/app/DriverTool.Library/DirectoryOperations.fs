namespace DriverTool.Library

open System    
open DriverTool.Library.Paths
open DriverTool.Library.F
open DriverTool.Library.FileSystem

module DirectoryOperations =
    type DirectoryOperations = class end
    open System
    open System.IO
    open DriverTool.Library.Logging
        
    let logger = Logging.getLoggerByName("DirectoryOperations")

    /// Create directory, throw exception if not succesful.
    let createDirectoryUnsafe directoryPath =
        System.IO.Directory.CreateDirectory(FileSystem.longPathValue directoryPath) |> ignore
        directoryPath

    ///Create directory
    let createDirectory (directoryPath:FileSystem.Path) =
        try
            Result.Ok (createDirectoryUnsafe directoryPath)
        with
        | ex -> Result.Error (new Exception(sprintf "Failed to create directory '%s'" (FileSystem.pathValue directoryPath),ex))
    
    ///Delete directory, throw exception if not succesful
    let deleteDirectoryUnsafe force (folderPath:FileSystem.Path) =
            match (directoryExists folderPath) with
            |true -> 
                System.IO.Directory.Delete(FileSystem.longPathValue folderPath, force)
                folderPath
            |false -> 
                folderPath
    
    ///Delete directory
    let deleteDirectory force (folderPath:FileSystem.Path) =        
        tryCatch2 (Some (sprintf "Failed to delete directory '%A'" folderPath)) deleteDirectoryUnsafe force folderPath

    /// Get files in directory
    let getFiles' directoryPath =
        System.IO.Directory.GetFiles(FileSystem.longPathValue directoryPath)
        |>Array.map FileSystem.pathUnSafe

    /// Get sub directories in directory. Throw exception if not succesful.
    let getDirectories' directoryPath =
        System.IO.Directory.GetDirectories(FileSystem.longPathValue directoryPath)
        |>Array.map FileSystem.pathUnSafe

    /// Get sub directories in directory.
    let getSubDirectories path =
        tryCatch (Some(sprintf "Failed to get sub directories: '%A'" path)) getDirectories' path

    let ensureDirectoryExistsWithMessage createIfNotExists message directoryPath =
        let directoryExists = 
            directoryExists directoryPath
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
    
    /// Check if directory is empty
    let directoryPathIsEmpty directoryPath =
         match (directoryExists directoryPath) with
         |true ->
             let isEmpty = not (Array.length (getDirectories' directoryPath) > 0 || Array.length (getFiles' directoryPath) > 0)
             isEmpty
         |false -> true
    
    let ensureDirectoryExistsAndIsEmptyWithMessage  message (directoryPath:FileSystem.Path) createIfNotExists =
        match (ensureDirectoryExists createIfNotExists directoryPath) with
        |Ok dp -> 
            match (directoryPathIsEmpty dp) with
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
            match (directoryPathIsEmpty fp) with
            |true -> Result.Ok fp
            |false -> toErrorResult (toException (sprintf "Folder '%A' is not empty." fp) None) message 

    let ensureFolderPathExists' force message folderPath =
        match folderPath with
        |Result.Error _ -> folderPath
        |Result.Ok fp ->
            match (directoryExists fp) with
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

    let ensureDirectoryNotExistsWithMessage message directoryPath =
        match directoryExists directoryPath with
        |true -> Result.Error (new Exception(sprintf "Directory '%A' allready exists. %s" directoryPath message))
        |false -> Result.Ok directoryPath
    
    let getParentFolderPath (folderPath:FileSystem.Path)=        
        FileSystem.path (System.IO.DirectoryInfo(FileSystem.longPathValue folderPath).Parent.FullName)

    let getParentFolderPathUnsafe (folderPath:FileSystem.Path)=        
        FileSystem.pathUnSafe (System.IO.DirectoryInfo(FileSystem.longPathValue folderPath).Parent.FullName)
            
    let toSearchOptions recurse =
        match recurse with
        |false -> System.IO.SearchOption.TopDirectoryOnly
        |true -> System.IO.SearchOption.AllDirectories
    
    let getFilesUnsafe recurse directoryPath =
        let searchOptions = toSearchOptions recurse                
        System.IO.Directory.GetFiles(FileSystem.longPathValue directoryPath,"*.*",searchOptions)

    let getFiles recurse directoryPath =
        try
            (getFilesUnsafe recurse directoryPath)
            |>Seq.map(fun fn -> FileSystem.path fn)
            |>toAccumulatedResult
        with
        |ex -> Result.Error (new Exception(sprintf "Failed to get files in folder '%s' due to: %s" (FileSystem.pathValue directoryPath) ex.Message,ex))

    let findFilesUnsafe recurse searchPattern directoryPath =
        let searchOptions = toSearchOptions recurse
        System.IO.Directory.GetFiles(FileSystem.longPathValue directoryPath, searchPattern,searchOptions)

    let findFiles recurse searchPattern directoryPath =
        try
            (findFilesUnsafe recurse searchPattern directoryPath)
            |>Seq.map(fun fn -> FileSystem.path fn)
            |>toAccumulatedResult
        with
        |ex -> Result.Error (new Exception(sprintf "Failed to find files in folder '%s' due to: %s" (FileSystem.pathValue directoryPath) ex.Message,ex))
    
    let deleteDirectoryIfExists folderPath =
        match (directoryExists folderPath) with
        |true -> 
            deleteDirectory true folderPath                                    
        |false -> 
            Result.Ok folderPath

    let moveDirectoryUnsafe sourceFolderPath destinationFolderPath =
        try
            createDirectoryUnsafe (getParentFolderPathUnsafe destinationFolderPath) |> ignore
            System.IO.Directory.Move(FileSystem.longPathValue sourceFolderPath,FileSystem.longPathValue destinationFolderPath)
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
     