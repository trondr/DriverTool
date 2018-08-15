namespace DriverTool
open F
open System

module FileOperations =
    let deleteFileUnsafe (path: Path)  =
        System.IO.File.Delete path.Value

    let deleteFile path = 
        let deleteFileResult = tryCatch deleteFileUnsafe path
        match deleteFileResult with
        | Ok _ -> Result.Ok path
        | Error ex -> Result.Error ex

    type FileExistsException(message : string) =
        inherit Exception(message)    
    
    let ensureFileDoesNotExist overwrite (path:Path) : Result<Path, Exception> = 
        match System.IO.File.Exists(path.Value) with
        | true -> 
            match overwrite with
            | true -> deleteFile path        
            | false -> Result.Error (new FileExistsException(String.Format("File exists: '{0}'", path.Value)) :> Exception)
        | false -> Result.Ok path
    
    let ensureFileExists (path:Path) : Result<Path, Exception> = 
        match System.IO.File.Exists(path.Value) with
        | true -> Result.Ok path            
        | false -> Result.Error (new System.IO.FileNotFoundException(String.Format("File does not exist: '{0}'", path.Value)) :> Exception)