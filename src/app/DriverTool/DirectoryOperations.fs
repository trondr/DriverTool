namespace DriverTool
module DirectoryOperations =
    type DirectoryOperations = class end
    open System
    open Logging

    let createDirectoryUnsafe (directoryPath:Path) =
        System.IO.Directory.CreateDirectory(directoryPath.Value) |> ignore
        directoryPath

    let createDirectory (directoryPath:Path) =
        try
            Result.Ok (createDirectoryUnsafe directoryPath)
        with
        | ex -> Result.Error (new Exception(String.Format("Failed to create directory '{0}'",directoryPath.Value),ex))
    
    let directoryPathExists (directoryPath:Path) =
        System.IO.Directory.Exists(directoryPath.Value)

    let ensureDirectoryExists (directoryPath:Path, createIfNotExists) =
        let directoryExists = 
            directoryPathExists directoryPath
        match (not directoryExists && createIfNotExists) with
        |true->
            Logger<DirectoryOperations>.InfoFormat("Creating directory: '{0}'...",directoryPath.Value)
            createDirectory directoryPath
        |false->
           match directoryExists with
           | true -> Result.Ok directoryPath
           | false -> Result.Error (new Exception(String.Format("Directory not found: '{0}'", directoryPath.Value)))

    
    let directoryIsEmpty (directoryPath:Path) =
         match (directoryPathExists directoryPath) with
         |true ->
             let isEmpty = not (System.IO.Directory.GetDirectories(directoryPath.Value).Length > 0 || System.IO.Directory.GetFiles(directoryPath.Value).Length > 0)
             isEmpty
         |false -> true
    
    let ensureDirectoryExistsAndIsEmpty (directoryPath:Path, createIfNotExists) =
        match (ensureDirectoryExists (directoryPath, createIfNotExists)) with
        |Ok dp -> 
            match (directoryIsEmpty dp) with
            |true -> Result.Ok dp
            |false -> Result.Error (new Exception(String.Format("Directory '{0}' is not empty.", dp.Value)))
        |Error ex -> Result.Error ex
