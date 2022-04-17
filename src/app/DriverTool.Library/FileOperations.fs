namespace DriverTool.Library

open System
open Common.Logging
open DriverTool.Library.Paths
open DriverTool.Library.F
open DriverTool.Library.Logging
open DriverTool.Library.FileSystem
open DriverTool.Library.DirectoryOperations

module FileOperations =

    /// Delete file. Throw exception if not succesful.
    let deleteFile' filePath =
        System.IO.File.Delete(FileSystem.longPathValue filePath)
        filePath

    /// Delete file.
    let deleteFile filePath =
        tryCatch (Some (sprintf "Failed to delete file: '%A'" filePath)) deleteFile' filePath

    type FileExistsException(message : string) =
        inherit Exception(message)    
    
    let ensureFileDoesNotExistWithMessage message overwrite filePath =
        match fileExists filePath with
        | true -> 
            match overwrite with
            | true -> deleteFile filePath    
            | false -> Result.Error (new FileExistsException(sprintf "File allready exists: '%s'. %s" (FileSystem.pathValue filePath) message) :> Exception)
        | false -> Result.Ok filePath
    
    let ensureFileDoesNotExist overwrite filePath = 
        ensureFileDoesNotExistWithMessage String.Empty overwrite filePath
    
    let ensureFileExists path = 
        match fileExists  path with
        | true -> Result.Ok path            
        | false -> Result.Error (new System.IO.FileNotFoundException(sprintf "File does not exist: '%s'" (FileSystem.pathValue path)) :> Exception)
    
    let ensureFileExistsWithMessage message path = 
        match fileExists path with
        | true -> Result.Ok path            
        | false -> Result.Error (new System.IO.FileNotFoundException(message) :> Exception)
    
    let getFileSize filePath =
        (new System.IO.FileInfo(FileSystem.longPathValue filePath)).Length
 
    let writeContentToFile (logger:Common.Logging.ILog) filePath (content:string) =         
        try
            use sw = (new System.IO.StreamWriter(FileSystem.longPathValue filePath))
            if(logger.IsDebugEnabled) then ( logger.Debug(sprintf "Writing content to file '%A' (TID: %i)" filePath System.Threading.Thread.CurrentThread.ManagedThreadId))
            (sw.Write(content))            
            if(logger.IsDebugEnabled) then ( logger.Debug(sprintf "Finished writing content to file '%A' (TID: %i)" filePath System.Threading.Thread.CurrentThread.ManagedThreadId))
            Result.Ok filePath
        with
        |ex -> Result.Error ex
    
    let readContentFromFile filePath = 
        try
            use sr = (new System.IO.StreamReader(FileSystem.longPathValue filePath))
            Result.Ok (sr.ReadToEnd())
        with
        |ex -> Result.Error ex
    
    let copyFileUnsafe force sourceFilePath destinationFilePath =
        System.IO.File.Copy(FileSystem.longPathValue sourceFilePath, FileSystem.longPathValue destinationFilePath, force)
        destinationFilePath
    
    /// Copy file
    let copyFile force sourceFilePath destinationFilePath =
        tryCatch3 (Some (sprintf "Failed to copy file: '%A'->%A. " sourceFilePath destinationFilePath)) copyFileUnsafe force sourceFilePath destinationFilePath 

    let copyFilePaths (destinationFolderPath) (files:seq<Path>) =
        files
        |>Seq.map(fun fp -> 
                    result{
                        let sourceFile = (new System.IO.FileInfo(FileSystem.pathValue fp))
                        let! sourceFilePath = FileSystem.path sourceFile.FullName
                        let! destinationFilePath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue destinationFolderPath, sourceFile.Name))
                        let! copyResult = copyFile true sourceFilePath destinationFilePath
                        return copyResult
                    }
                 )
        |>Seq.toArray
        |>toAccumulatedResult

    /// <summary>
    /// Prepend a period to a file extension if necessary
    /// </summary>
    /// <param name="extension"></param>
    let toExtension (extension:string) =
        if(extension.StartsWith(".")) then
            extension
        else
            "." + extension
    /// <summary>
    /// Ensure that path have specified file extension
    /// </summary>
    /// <param name="path"></param>
    /// <param name="extension"></param>
    let ensureFileExtension extension path = 
        let expectedExtension = toExtension extension
        let actualExtension = System.IO.Path.GetExtension(FileSystem.pathValue path).ToLower()
        match actualExtension with
        | e when e = expectedExtension -> Result.Ok path            
        | _ -> Result.Error (new Exception(sprintf "File does not have extension '%s': '%A'" extension path))

    let deleteFileIfExists filePath =
        if(System.IO.File.Exists(FileSystem.pathValue filePath)) then
            System.IO.File.Delete(FileSystem.pathValue filePath)

    
    //Source: http://fssnip.net/1k
    open System.IO

    /// Represents a sequence of values 'T where items 
    /// are generated asynchronously on-demand
    type AsyncSeq<'T> = Async<AsyncSeqInner<'T>> 
    and AsyncSeqInner<'T> =
      | Ended
      | Item of 'T * AsyncSeq<'T>
    
    /// Read file 'fn' in blocks of size 'size'
    /// (returns on-demand asynchronous sequence)
    let readInBlocks (stream:FileStream) size = 
        async {                            
            let buffer = Array.zeroCreate size
            /// Returns next block as 'Item' of async seq
            let rec nextBlock() = 
                async {
                    let! count = stream.AsyncRead(buffer, 0, size)
                    if count = 0 then return Ended
                    else 
                        // Create buffer with the right size
                        let res = 
                            if count = size then buffer
                            else buffer |> Seq.take count |> Array.ofSeq
                        return Item(res, nextBlock()) 
                }
            return! nextBlock()
        }
        
    /// Asynchronous function that compares two asynchronous sequences
    /// item by item. If an item doesn't match, 'false' is returned
    /// immediately without generating the rest of the sequence. If the
    /// lengths don't match, exception is thrown.
    let rec compareAsyncSeqs seq1 seq2 = async {
      let! item1 = seq1
      let! item2 = seq2
      match item1, item2 with 
      | Item(b1, ns1), Item(b2, ns2) when b1 <> b2 -> return false
      | Item(b1, ns1), Item(b2, ns2) -> return! compareAsyncSeqs ns1 ns2
      | Ended, Ended -> return true
      | _ -> return failwith "Size doesn't match" }

    /// Compare two files using 1k blocks
    let compareFileUnsafe (filePath1, filePath2) =
        use stream1 = File.OpenRead(FileSystem.pathValue filePath1)
        use stream2 = File.OpenRead(FileSystem.pathValue filePath2)
        let s1 = readInBlocks stream1 1000        
        let s2 = readInBlocks stream2 1000
        let isEqual =
            compareAsyncSeqs s1 s2
            |> Async.RunSynchronously                
        isEqual

    let compareFile filePath1 filePath2 =
        tryCatch (Some (sprintf "Failed to compare files: '%A' <-> %A. " filePath1 filePath2)) compareFileUnsafe (filePath1, filePath2)

    let compareDirectory directoryPath1 directoryPath2 =
        imperative{
            let directories1 = System.IO.Directory.GetDirectories(FileSystem.longPathValue directoryPath1)   
            let directories2 = System.IO.Directory.GetDirectories(FileSystem.longPathValue directoryPath2)                        
            if( directories1.Length <> directories2.Length) then 
                return false
        
            let relativeDirectories1 = directories1 |> Array.map(fun d -> d.Replace(FileSystem.longPathValue directoryPath1,"")) |> Array.sort
            let relativeDirectories2 = directories2 |> Array.map(fun d -> d.Replace(FileSystem.longPathValue directoryPath2,"")) |> Array.sort
            
            if( relativeDirectories1 <> relativeDirectories2) then 
                return false
            
            let files1 = DirectoryOperations.getFilesUnsafe true (directoryPath1) |>Array.sort
            let files2 = DirectoryOperations.getFilesUnsafe true (directoryPath2) |> Array.sort
            if( files1.Length <> files2.Length) then 
                return false

            let files2To1 = files2 |> Array.map(fun f -> f.Replace(FileSystem.longPathValue directoryPath2,FileSystem.longPathValue directoryPath1)) |> Array.sort

            if( files1 <> files2To1) then 
                return false

            let file1AndFile2Tuples =
                files1 
                |> Array.zip files2

            for (file1,file2) in file1AndFile2Tuples do 
                let isEqualResult = 
                    result
                        {
                            let! existingFile1 = FileSystem.existingFilePathString file1
                            let! existingFile2 = FileSystem.existingFilePathString file2
                            let! filesAreEqual = compareFile existingFile1 existingFile2 
                            return filesAreEqual
                        }
                let isEqual =
                    match isEqualResult with 
                    |Result.Ok b -> b                    
                    |Result.Error _ -> false
                if(not isEqual) then 
                    return false                
            return true
        }
        
    let toFileName filePath =
        Path.GetFileName(FileSystem.longPathValue filePath)

    let createRandomFile logger folderPath =
        result {
            let! existingFolderPath = DirectoryOperations.ensureDirectoryExists false folderPath
            let! randomFilePath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue existingFolderPath, System.IO.Path.GetRandomFileName()))
            let! writeResult = writeContentToFile logger randomFilePath (System.Guid.NewGuid().ToString())
            return randomFilePath
        }

    [<AllowNullLiteral>]
    type TemporaryFile() =
           let createTestFile =                        
               match FileSystem.path (System.IO.Path.GetTempFileName()) with
               | Result.Ok path -> path
               | Result.Error ex -> raise ex
           
           member _this.Path = createTestFile
           interface IDisposable with
               member this.Dispose() =
                   match fileExists this.Path with
                   | true -> System.IO.File.Delete(FileSystem.longPathValue this.Path)
                   | false -> ()
    
    let copyFileIfExists sourceFile destinationFolderPath = 
        match(result{
            let! sourceFilePath = FileSystem.path sourceFile
            let! existingSourceFilePath = FileSystem.existingFilePath sourceFilePath
            let! verifiedSourceFilePath = FileSystem.path (FileSystem.pathValue existingSourceFilePath)
            let fileName = PathOperations.getFileNameFromPath sourceFilePath
            let! destinationFilePath = PathOperations.combinePaths2 destinationFolderPath fileName    
            let! copiedFilePath = copyFile true verifiedSourceFilePath destinationFilePath
            return copiedFilePath
        }) with
        |Result.Ok p -> Result.Ok p
        |Result.Error _ ->            
            Result.Ok destinationFolderPath
    
    let copyFileIfExists' sourceFile destinationFolderPath = 
        match sourceFile with
        | Some sf ->
            match(result{
                let! sourceFilePath = FileSystem.path sf
                let! existingSourceFilePath = FileSystem.existingFilePath sourceFilePath
                let! verifiedSourceFilePath = FileSystem.path (FileSystem.pathValue existingSourceFilePath)
                let fileName = PathOperations.getFileNameFromPath sourceFilePath
                let! destinationFilePath = PathOperations.combinePaths2 destinationFolderPath fileName    
                let! copiedFilePath = copyFile true verifiedSourceFilePath destinationFilePath
                return copiedFilePath
            }) with
            |Result.Ok p -> Result.Ok p
            |Result.Error _ ->            
                Result.Ok destinationFolderPath
        |None -> Result.Ok destinationFolderPath

    let getParentPath' filePath =
        let fileInfo = new System.IO.FileInfo(FileSystem.pathValue filePath)
        FileSystem.pathUnSafe fileInfo.Directory.FullName

    let getParentPath filePath =
        tryCatch (Some (sprintf "Failed to get parent directory of '%s'" (FileSystem.pathValue filePath))) getParentPath' filePath