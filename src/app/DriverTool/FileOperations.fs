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
    
    let ensureFileDoesNotExistWithMessage (message,overwrite,filePath:Path) =
        match System.IO.File.Exists(filePath.Value) with
        | true -> 
            match overwrite with
            | true -> deleteFile filePath        
            | false -> Result.Error (new FileExistsException(String.Format("File allready exists: '{0}'. {1}", filePath.Value, message)) :> Exception)
        | false -> Result.Ok filePath
    
    let ensureFileDoesNotExist (overwrite, filePath:Path) : Result<Path, Exception> = 
        ensureFileDoesNotExistWithMessage (String.Empty,overwrite, filePath)
    
    let ensureFileDoesNotExistR overwrite (path:Result<Path, Exception>) : Result<Path, Exception> = 
        match path with
        |Error  ex -> Result.Error ex
        |Ok p -> ensureFileDoesNotExist (overwrite, p)
    
    let ensureFileExists (path:Path) : Result<Path, Exception> = 
        match System.IO.File.Exists(path.Value) with
        | true -> Result.Ok path            
        | false -> Result.Error (new System.IO.FileNotFoundException(String.Format("File does not exist: '{0}'", path.Value)) :> Exception)
    
    let ensureFileExistsWithMessage message (path:Path) = 
        match System.IO.File.Exists(path.Value) with
        | true -> Result.Ok path            
        | false -> Result.Error (new System.IO.FileNotFoundException(message) :> Exception)

    let ensureFileExistsR (path:Result<Path, Exception>) : Result<Path, Exception> = 
        match path with
        |Ok p -> ensureFileExists p
        |Error ex -> Result.Error ex

    let fileExists filePath =
        System.IO.File.Exists(filePath)

    let getFileSize filePath =
        (new System.IO.FileInfo(filePath)).Length
 
    let writeContentToFile (filePath:string) (content:string) =         
        try
            use sw = (new System.IO.StreamWriter(filePath))
            Result.Ok (sw.Write(content))
        with
        |ex -> Result.Error ex
    
    let readContentFromFile (filePath:string) = 
        try
            use sr = (new System.IO.StreamReader(filePath))
            Result.Ok (sr.ReadToEnd())
        with
        |ex -> Result.Error ex
    
    let copyFileUnsafe (sourceFilePath:Path, destinationFilePath:Path, force) =
        System.IO.File.Copy(sourceFilePath.Value, destinationFilePath.Value, force)
        destinationFilePath
    
    let copyFile (sourceFilePath:Path, destinationFilePath:Path, force) =
        tryCatchWithMessage copyFileUnsafe (sourceFilePath, destinationFilePath, force) (String.Format("Failed to copy file: '{0}'->{1}. ",sourceFilePath.Value,destinationFilePath.Value))

    let copyFiles (destinationFolderPath:Path) (files:seq<string>) =
        files
        |>Seq.map(fun fp -> 
                    result{
                        let sourceFile = (new System.IO.FileInfo(fp))
                        let! sourceFilePath = Path.create sourceFile.FullName
                        let! destinationFilePath = Path.create (System.IO.Path.Combine(destinationFolderPath.Value, sourceFile.Name))
                        let! copyResult = copyFile (sourceFilePath,destinationFilePath,true)
                        return copyResult
                    }
                 )
        |>Seq.toArray
        |>toAccumulatedResult