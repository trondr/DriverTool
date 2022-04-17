namespace DriverTool.Library

open System    
open DriverTool.Library.Paths
open DriverTool.Library.F

type InvalidPathException(path:string, message : string) =
        inherit Exception(
            match String.IsNullOrWhiteSpace(message) with
            |false  -> sprintf "The path '%s' is not valid. %s" path message
            |true -> sprintf "The path '%s' is not valid." path
            )

module FileSystem = 
    open System.IO
    open DriverTool.Library.RegExp

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

    ///Check if path is an unc path
    let isUncPathValue path =
        let uri = new System.Uri(path)
        uri.IsUnc

    ///Check if path is a Win32 long path
    let isLongPathValue (path:string) =
        path.StartsWith(@"\\?\")

    ///Convert path to a Win32 long path (prefixed with: '\\?\') that enables underlying Win32 api to support paths longer than 255 charcters.
    let toLongPathValue path =
        match path with
        | p when isLongPathValue p -> p //Path is allready a long path
        | p when isUncPathValue p -> (sprintf @"\\?\UNC\%s" (replace @"^\\\\" "" true p)) //Path is an unc path
        | _ -> (sprintf @"\\?\%s" path) //Path is a local path

    ///Convert path to short path without the \\?\ prefix.
    let toShortPathValue path =
        replace @"^\\\\\?\\" "" true path

    ///Create path, return path as a result object.
    let path path =
        match path with
        |null -> Result.Error (new InvalidPathException("","Path cannot be null.") :> Exception)
        |NullOrEmpty -> Result.Error (new InvalidPathException("","Path cannot be empty.") :> Exception)
        |InvalidPathCharacters -> Result.Error ((new InvalidPathException(path,"Path contains invalid characters.")) :> Exception)
        |WildCardCharacters -> Result.Error ((new InvalidPathException(path,"Path contains wild card characters.")) :> Exception)
        |p -> Result.Ok (Path  (toShortPathValue p))

    //Create path, throw an exception if not a valid path.
    let pathUnSafe pathString = 
        match (path pathString) with
        |Ok p -> p
        |Error ex -> raise ex

    ///Get path value
    let pathValue (Path path) = path

    ///Get short path value (without the long path prefix: \\?\)
    let shortPathValue (Path path) = toShortPathValue path

    ///Get long path value (with the long path prefix: \\?\)
    let longPathValue (Path path) = toLongPathValue path
    
    ///Check if file exists
    let fileExists (filePath:Path) =
        System.IO.File.Exists(longPathValue filePath)

    let (|FilePathExists|_|) =
        fileExists
        >> ifTrueThen FilePathExists

    let existingFilePath (filePath:Path) =
        result{
            let! existingFilePath =
                match filePath with
                |FilePathExists -> Result.Ok (Path (pathValue filePath))
                |_ -> Result.Error (new FileNotFoundException("File not found: " + pathValue filePath):>Exception)
            return existingFilePath
        }

    let existingFilePathString (pathString:string) =
        result{
            let! p = (path pathString)
            let! existingPath = existingFilePath p
            return existingPath
        }

    let directoryExists (directoryPath:Path) =
        System.IO.Directory.Exists(longPathValue directoryPath)

    let (|DirectoryPathExists|_|) =
        directoryExists
        >> ifTrueThen DirectoryPathExists

    let existingDirectoryPath (pathString:string) =
        result{
            let! p = (path pathString)
            let! existingPath =
                match p with
                |DirectoryPathExists -> Result.Ok (Path (pathValue p))
                | p -> Result.Error (new DirectoryNotFoundException("Directory not found: " + pathValue p):>Exception)
            return existingPath
        }

    let getDirectoryPath filePath =
        pathUnSafe (System.IO.Path.GetDirectoryName(pathValue filePath))
    