namespace DriverTool
open F
open System

module FileOperations =
    let deleteFileUnsafe (path: FileSystem.Path)  =
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
        ensureFileDoesNotExistWithMessage (String.Empty,overwrite, filePath)
    
    let ensureFileDoesNotExistR overwrite (path:Result<FileSystem.Path, Exception>) : Result<FileSystem.Path, Exception> = 
        match path with
        |Error  ex -> Result.Error ex
        |Ok p -> ensureFileDoesNotExist (overwrite, p)
    
    let ensureFileExists (path:FileSystem.Path) : Result<FileSystem.Path, Exception> = 
        match System.IO.File.Exists(FileSystem.pathValue  path) with
        | true -> Result.Ok path            
        | false -> Result.Error (new System.IO.FileNotFoundException(String.Format("File does not exist: '{0}'", FileSystem.pathValue path)) :> Exception)
    
    let ensureFileExistsWithMessage message (path:FileSystem.Path) = 
        match System.IO.File.Exists(FileSystem.pathValue path) with
        | true -> Result.Ok path            
        | false -> Result.Error (new System.IO.FileNotFoundException(message) :> Exception)

    let ensureFileExistsR (path:Result<FileSystem.Path, Exception>) : Result<FileSystem.Path, Exception> = 
        match path with
        |Ok p -> ensureFileExists p
        |Error ex -> Result.Error ex

    let fileExistsUnsafe (filePath:string) =
        System.IO.File.Exists(filePath)
    
    let fileExists (filePath:FileSystem.Path) =
        System.IO.File.Exists(FileSystem.pathValue filePath)

    let getFileSize filePath =
        (new System.IO.FileInfo(FileSystem.pathValue filePath)).Length
 
    let writeContentToFile (filePath:FileSystem.Path) (content:string) =         
        try
            use sw = (new System.IO.StreamWriter(FileSystem.pathValue filePath))
            Result.Ok (sw.Write(content))
        with
        |ex -> Result.Error ex
    
    let readContentFromFile (filePath:FileSystem.Path) = 
        try
            use sr = (new System.IO.StreamReader(FileSystem.pathValue filePath))
            Result.Ok (sr.ReadToEnd())
        with
        |ex -> Result.Error ex
    
    let copyFileUnsafe (sourceFilePath:FileSystem.Path, destinationFilePath:FileSystem.Path, force) =
        System.IO.File.Copy(FileSystem.pathValue sourceFilePath, FileSystem.pathValue destinationFilePath, force)
        destinationFilePath
    
    let copyFile (sourceFilePath:FileSystem.Path, destinationFilePath:FileSystem.Path, force) =
        tryCatchWithMessage copyFileUnsafe (sourceFilePath, destinationFilePath, force) (String.Format("Failed to copy file: '{0}'->{1}. ",sourceFilePath,destinationFilePath))

    let copyFiles (destinationFolderPath:FileSystem.Path) (files:seq<string>) =
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
    
    let ensureFileExtension (path:FileSystem.Path, extension:string) : Result<FileSystem.Path, Exception> = 
        match System.IO.Path.GetExtension(FileSystem.pathValue path).ToLower() with
        | e when e = extension -> Result.Ok path            
        | _ -> Result.Error (new Exception(String.Format("File does not have extension '{0}': '{1}'",extension, FileSystem.pathValue path)))

    let deleteFileIfExists (filePath:FileSystem.Path) =
        if(System.IO.File.Exists(FileSystem.pathValue filePath)) then
            System.IO.File.Delete(FileSystem.pathValue filePath)