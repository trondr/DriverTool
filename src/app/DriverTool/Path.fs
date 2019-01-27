﻿namespace DriverTool

open System    
open Paths

type InvalidPathException(path:string, message : string) =
        inherit Exception(
            match String.IsNullOrWhiteSpace(message) with
            |false  -> String.Format("The path '{0}' is not valid. {1}", path, message)
            |true -> String.Format("The path '{0}' is not valid.", path)
            )

module FileSystem = 
    open System.IO

    type Path = private Path of string
    
    let ifTrueThen success =
        function
        |true -> Some success
        |false -> None

    let (|NullOrEmpty|_|) =
        String.IsNullOrWhiteSpace 
        >> ifTrueThen NullOrEmpty
    
    let (|InvalidPathCharacters|_|) =
        HasInvalidPathCharacters
        >> ifTrueThen InvalidPathCharacters

    let (|WildCardCharacters|_|) =
        HasWildCardCharacters
        >> ifTrueThen WildCardCharacters

    let path path =
        match path with
        |null -> Result.Error (new InvalidPathException("","Path cannot be null.") :> Exception)
        |NullOrEmpty -> Result.Error (new InvalidPathException("","Path cannot be empty.") :> Exception)
        |InvalidPathCharacters -> Result.Error ((new InvalidPathException(path,"Path contains invalid characters.")) :> Exception)
        |WildCardCharacters p -> Result.Error ((new InvalidPathException(path,"Path contains wild card characters.")) :> Exception)
        |p -> Result.Ok (Path p)

    let pathUnSafe pathString = 
        match (path pathString) with
        |Ok p -> p
        |Error ex -> raise ex

    let pathValue (Path path) = path

    type ExistingFilePath = private ExistingFilePath of string

    let fileExists (filePath:Path) =
        System.IO.File.Exists(pathValue filePath)

    let (|FilePathExists|_|) =
        fileExists
        >> ifTrueThen FilePathExists

    let existingFilePath (pathString:string) =
        result{
            let! p = (path pathString)
            let! existingPath =
                match p with
                |FilePathExists -> Result.Ok (ExistingFilePath (pathValue p))
                | p -> Result.Error (new FileNotFoundException("File not found: " + pathValue p):>Exception)
            return existingPath
        }

    let existingFilePathValue (ExistingFilePath existingFilePath) = existingFilePath
    
    let existingFilePathValueToPath existingFilePath =
        Path (existingFilePathValue existingFilePath)

    type ExistingDirectoryPath = private ExistingDirectoryPath of string

    let directoryExists (directoryPath:Path) =
        System.IO.Directory.Exists(pathValue directoryPath)

    let (|DirectoryPathExists|_|) =
        directoryExists
        >> ifTrueThen DirectoryPathExists

    let existingDirectoryPath (pathString:string) =
        result{
            let! p = (path pathString)
            let! existingPath =
                match p with
                |DirectoryPathExists -> Result.Ok (ExistingDirectoryPath (pathValue p))
                | p -> Result.Error (new DirectoryNotFoundException("Directory not found: " + pathValue p):>Exception)
            return existingPath
        }
    
    let existingDirectoryPathValue (ExistingDirectoryPath existingDirectoryPath) = existingDirectoryPath
    
    let existingDirectoryPathValueToPath existingDirectoryPath =
        Path (existingDirectoryPathValue existingDirectoryPath)
     