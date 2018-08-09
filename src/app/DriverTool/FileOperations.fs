namespace DriverTool
open F
open DriverTool
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
    
    type FilePath =
        | PathResult of Result<Path,Exception>
        | Path of Path
        
    let private ensureFileDoesNotExistP (path:Path) overwrite : Result<Path, Exception> = 
        match System.IO.File.Exists(path.Value) with
        | true -> 
            match overwrite with
            | true -> deleteFile path        
            | false -> Result.Error (new FileExistsException(String.Format("File exists: '{0}'", path.Value)) :> Exception)
        | false -> Result.Ok path

    let private ensureFileDoesNotExistPR path overwrite : Result<Path,Exception> = 
        match path with
        | Ok p  -> ensureFileDoesNotExistP p overwrite
        | Error ex -> Result.Error ex

    let ensureFileDoesNotExist overwrite path  = 
        match path with
        | PathResult(path) -> ensureFileDoesNotExistPR path overwrite
        | Path(path) -> ensureFileDoesNotExistP path overwrite
