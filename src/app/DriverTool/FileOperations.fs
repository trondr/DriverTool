namespace DriverTool
open F
open System

module FileOperations =
    open FileSystem

    let deleteFileUnsafe path  =
        System.IO.File.Delete (FileSystem.pathValue path)

    let deleteFile path = 
        let deleteFileResult = tryCatch deleteFileUnsafe path
        match deleteFileResult with
        | Ok _ -> Result.Ok path
        | Error ex -> Result.Error ex

    type FileExistsException(message : string) =
        inherit Exception(message)    
    
    let ensureFileDoesNotExistWithMessage message overwrite filePath =
        match System.IO.File.Exists(FileSystem.pathValue filePath) with
        | true -> 
            match overwrite with
            | true -> deleteFile filePath        
            | false -> Result.Error (new FileExistsException(String.Format("File allready exists: '{0}'. {1}", FileSystem.pathValue filePath, message)) :> Exception)
        | false -> Result.Ok filePath
    
    let ensureFileDoesNotExist overwrite filePath = 
        ensureFileDoesNotExistWithMessage String.Empty overwrite filePath
    
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
            (sw.Write(content))
            Result.Ok filePath
        with
        |ex -> Result.Error ex
    
    let readContentFromFile filePath = 
        try
            use sr = (new System.IO.StreamReader(FileSystem.pathValue filePath))
            Result.Ok (sr.ReadToEnd())
        with
        |ex -> Result.Error ex
    
    let copyFileUnsafe force sourceFilePath destinationFilePath =
        System.IO.File.Copy(FileSystem.pathValue sourceFilePath, FileSystem.pathValue destinationFilePath, force)
        destinationFilePath
    
    let copyFile force sourceFilePath destinationFilePath =
        tryCatch3WithMessage copyFileUnsafe force sourceFilePath destinationFilePath (sprintf "Failed to copy file: '%A'->%A. " sourceFilePath destinationFilePath)

    let copyFiles (destinationFolderPath) (files:seq<string>) =
        files
        |>Seq.map(fun fp -> 
                    result{
                        let sourceFile = (new System.IO.FileInfo(fp))
                        let! sourceFilePath = FileSystem.path sourceFile.FullName
                        let! destinationFilePath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue destinationFolderPath, sourceFile.Name))
                        let! copyResult = copyFile true sourceFilePath destinationFilePath
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
    let readInBlocks filePath size = async {
      let stream = File.OpenRead(FileSystem.existingFilePathValue filePath)
      let buffer = Array.zeroCreate size
  
      /// Returns next block as 'Item' of async seq
      let rec nextBlock() = async {
        let! count = stream.AsyncRead(buffer, 0, size)
        if count = 0 then return Ended
        else 
          // Create buffer with the right size
          let res = 
            if count = size then buffer
            else buffer |> Seq.take count |> Array.ofSeq
          return Item(res, nextBlock()) }

      return! nextBlock() }

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
        let s1 = readInBlocks filePath1 1000
        let s2 = readInBlocks filePath2 1000
        compareAsyncSeqs s1 s2
        |> Async.RunSynchronously
    
    let compareFile filePath1 filePath2 =
        tryCatchWithMessage compareFileUnsafe (filePath1, filePath2) (sprintf "Failed to compare files: '%A' <-> %A. " filePath1 filePath2)

    let compareDirectory directoryPath1 directoryPath2 =
        imperative{
            let directories1 = System.IO.Directory.GetDirectories(FileSystem.existingDirectoryPathValue directoryPath1)   
            let directories2 = System.IO.Directory.GetDirectories(FileSystem.existingDirectoryPathValue directoryPath2)                        
            if( directories1.Length <> directories2.Length) then 
                return false
        
            let relativeDirectories1 = directories1 |> Array.map(fun d -> d.Replace(FileSystem.existingDirectoryPathValue directoryPath1,"")) |> Array.sort
            let relativeDirectories2 = directories2 |> Array.map(fun d -> d.Replace(FileSystem.existingDirectoryPathValue directoryPath2,"")) |> Array.sort
            
            if( relativeDirectories1 <> relativeDirectories2) then 
                return false
            
            let files1 = DirectoryOperations.getFilesUnsafe true (FileSystem.existingDirectoryPathValueToPath directoryPath1) |>Array.sort
            let files2 = DirectoryOperations.getFilesUnsafe true (FileSystem.existingDirectoryPathValueToPath directoryPath2) |> Array.sort
            if( files1.Length <> files2.Length) then 
                return false

            let files2To1 = files2 |> Array.map(fun f -> f.Replace(FileSystem.existingDirectoryPathValue directoryPath2,FileSystem.existingDirectoryPathValue directoryPath1)) |> Array.sort

            if( files1 <> files2To1) then 
                return false

            let file1AndFile2Tuples =
                files1 
                |> Array.zip files2

            for (file1,file2) in file1AndFile2Tuples do 
                let isEqualResult = 
                    result
                        {
                            let! existingFile1 = FileSystem.existingFilePath file1
                            let! existingFile2 = FileSystem.existingFilePath file2
                            let! filesAreEqual = compareFile existingFile1 existingFile2 
                            return filesAreEqual
                        }
                let isEqual =
                    match isEqualResult with 
                    |Ok b -> b                    
                    |Error _ -> false
                if(not isEqual) then 
                    return false
                


            return true
        }
        
            