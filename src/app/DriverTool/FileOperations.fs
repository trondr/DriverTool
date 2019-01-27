namespace DriverTool
open F
open System

module FileOperations =
    let deleteFileUnsafe path  =
        System.IO.File.Delete (FileSystem.pathValue path)

    let deleteFile path = 
        let deleteFileResult = tryCatch deleteFileUnsafe path
        match deleteFileResult with
        | Ok _ -> Result.Ok path
        | Error ex -> Result.Error ex

    type FileExistsException(message : string) =
        inherit Exception(message)    
    
    let ensureFileDoesNotExistWithMessage (message,overwrite,filePath) =
        match System.IO.File.Exists(FileSystem.pathValue filePath) with
        | true -> 
            match overwrite with
            | true -> deleteFile filePath        
            | false -> Result.Error (new FileExistsException(String.Format("File allready exists: '{0}'. {1}", FileSystem.pathValue filePath, message)) :> Exception)
        | false -> Result.Ok filePath
    
    let ensureFileDoesNotExist (overwrite, filePath) = 
        ensureFileDoesNotExistWithMessage (String.Empty, overwrite, filePath)
    
    let ensureFileExists path = 
        match System.IO.File.Exists(FileSystem.pathValue  path) with
        | true -> Result.Ok path            
        | false -> Result.Error (new System.IO.FileNotFoundException(String.Format("File does not exist: '{0}'", FileSystem.pathValue path)) :> Exception)
    
    let ensureFileExistsWithMessage message path = 
        match System.IO.File.Exists(FileSystem.pathValue path) with
        | true -> Result.Ok path            
        | false -> Result.Error (new System.IO.FileNotFoundException(message) :> Exception)
    
    let fileExists filePath =
        System.IO.File.Exists(FileSystem.pathValue filePath)

    let getFileSize filePath =
        (new System.IO.FileInfo(FileSystem.pathValue filePath)).Length
 
    let writeContentToFile filePath (content:string) =         
        try
            use sw = (new System.IO.StreamWriter(FileSystem.pathValue filePath))
            Result.Ok (sw.Write(content))
        with
        |ex -> Result.Error ex
    
    let readContentFromFile filePath = 
        try
            use sr = (new System.IO.StreamReader(FileSystem.pathValue filePath))
            Result.Ok (sr.ReadToEnd())
        with
        |ex -> Result.Error ex
    
    let copyFileUnsafe (sourceFilePath, destinationFilePath, force) =
        System.IO.File.Copy(FileSystem.pathValue sourceFilePath, FileSystem.pathValue destinationFilePath, force)
        destinationFilePath
    
    let copyFile (sourceFilePath, destinationFilePath, force) =
        tryCatchWithMessage copyFileUnsafe (sourceFilePath, destinationFilePath, force) (sprintf "Failed to copy file: '%A'->%A. " sourceFilePath destinationFilePath)

    let copyFiles (destinationFolderPath) (files:seq<string>) =
        files
        |>Seq.map(fun fp -> 
                    result{
                        let sourceFile = (new System.IO.FileInfo(fp))
                        let! sourceFilePath = FileSystem.path sourceFile.FullName
                        let! destinationFilePath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue destinationFolderPath, sourceFile.Name))
                        let! copyResult = copyFile (sourceFilePath,destinationFilePath,true)
                        return copyResult
                    }
                 )
        |>Seq.toArray
        |>toAccumulatedResult
    
    let ensureFileExtension (path, extension:string) : Result<FileSystem.Path, Exception> = 
        match System.IO.Path.GetExtension(FileSystem.pathValue path).ToLower() with
        | e when e = extension -> Result.Ok path            
        | _ -> Result.Error (new Exception(sprintf "File does not have extension '%s': '%A'" extension path))

    let deleteFileIfExists (filePath:FileSystem.Path) =
        if(System.IO.File.Exists(FileSystem.pathValue filePath)) then
            System.IO.File.Delete(FileSystem.pathValue filePath)