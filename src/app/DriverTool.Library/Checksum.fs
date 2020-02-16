namespace DriverTool.Library

module Checksum=
    open System
    open System.IO
    open System.Security.Cryptography    
    open DriverTool.Library.FileOperations
    open DriverTool.Library.Logging    
    let logger = DriverTool.Library.Logging.getLoggerByName "Checksum"

    let getHashAlgorithmFromHashStringLength hashStringLength = 
        match hashStringLength with
            | 32 -> MD5.Create() :> HashAlgorithm
            | 40 -> SHA1.Create() :> HashAlgorithm
            | 64 -> SHA256.Create() :> HashAlgorithm
            | 96 -> SHA384.Create() :> HashAlgorithm
            | 128 -> SHA512.Create() :> HashAlgorithm
            | _  -> SHA512.Create() :> HashAlgorithm

    let computeFileHash filePath (hashAlgorithm:HashAlgorithm)  =
        use fileStream = new FileStream(FileSystem.pathValue filePath, FileMode.Open, FileAccess.Read)
        hashAlgorithm.ComputeHash(fileStream)

    let fileHashToString fileHash =
        BitConverter.ToString(fileHash).Replace("-", "").ToLower()
    
    let computeFileHashString filePath (hashAlgorithm:HashAlgorithm) =
        (computeFileHash filePath hashAlgorithm)
        |>fileHashToString
    
    let computeFileHashSha256String filePath =
        (computeFileHash filePath (SHA256.Create()))
        |>fileHashToString

    let toLower (text:string) =
        text.ToLower()

    let computeFileHashFromHashLengthBase filePath hashLength =
        (getHashAlgorithmFromHashStringLength hashLength)                
        |> computeFileHash filePath
        |> fileHashToString
        |> toLower

    let computeFileHashFromHashLength filePath hashLength =
        genericLogger LogLevel.Debug computeFileHashFromHashLengthBase filePath hashLength
 
    let hasSameFileHash'' (logger:Common.Logging.ILog) checkFileExists getFileSize computeFileHashFromHashLength (destinationFile:FileSystem.Path, sourceFileHash:string option, sourceFileSize:Int64 option) =
        logger.Debug( new Msg(fun m -> m.Invoke(sprintf "Checking if source and destination file (%A) has the same file hash (%A)." destinationFile sourceFileHash)|>ignore))
        let destinationFileExists = checkFileExists destinationFile        
        match destinationFileExists with
        | true ->
            logger.Debug( new Msg(fun m -> m.Invoke(sprintf "Destination file (%A) exists. Check if file hash is the same." destinationFile)|>ignore))
            let doCompareHash =
                match sourceFileSize with
                |Some sourceSize ->
                    let destinationSize = getFileSize destinationFile
                    let fileSizeIsEqual = (sourceSize = destinationSize)
                    logger.Debug( new Msg(fun m -> m.Invoke(sprintf "Comparing source and destination file size. Is equal: %b." fileSizeIsEqual)|>ignore))
                    fileSizeIsEqual
                |None -> 
                    logger.Debug( new Msg(fun m -> m.Invoke(sprintf "Source file size has not been specified, force compare of source and destination hash.")|>ignore))
                    true
            let fileHashIsEqual =
                match doCompareHash with
                |false ->                     
                    logger.Debug( new Msg(fun m -> m.Invoke(sprintf "File size was not equal, implying that file hash can not be equal. No neeed to invoke expensive computation of file hash. hasSameFileHash->false.")|>ignore))
                    false 
                |true ->                    
                    logger.Debug( new Msg(fun m -> m.Invoke(sprintf "File size is equal. Invoke computation of file hash to check if source file and destination file has the same file hash.")|>ignore))
                    match sourceFileHash with
                    |Some sh ->                        
                        let sourceHash = sh|>toLower
                        let destinationHash = computeFileHashFromHashLength destinationFile sourceHash.Length
                        logger.Debug( new Msg(fun m -> m.Invoke(sprintf "Comparing destination file (%A) hash [%s] and source file hash [%s]..." destinationFile destinationHash sourceHash)|>ignore))
                        (sourceHash = destinationHash)
                    |None -> 
                        logger.Debug( new Msg(fun m -> m.Invoke(sprintf "Source hash has not been specified. hasSameFileHash->false")|>ignore))
                        false                    
            fileHashIsEqual
        | false ->            
            logger.Debug( new Msg(fun m -> m.Invoke(sprintf "Destination file (%A) does not exist. hasSameFileHash->false." destinationFile)|>ignore))
            false

    let hasSameFileHash' (filePath:FileSystem.Path, sourceFileHash:string option, fileSize:Int64 option) =
        hasSameFileHash'' logger fileExists getFileSize computeFileHashFromHashLength (filePath, sourceFileHash, fileSize)

    let hasSameFileHash2 (filePath:FileSystem.Path, crc:string option, fileSize:Int64 option) =
        DriverTool.Library.Logging.genericLogger LogLevel.Debug hasSameFileHash' (filePath, crc, fileSize)
        
    let hasSameFileHashPartial fileExists getFileSize computeFileHashFromHashLength (destinationFilePath:FileSystem.Path, sourceFileHash:string, sourceFileSize:Int64) =
            match(fileExists destinationFilePath) with
            | true ->   
                let destinationFileSize = getFileSize destinationFilePath
                let isSameFileSize = 
                    (sourceFileSize = destinationFileSize) || (sourceFileSize = 0L)
                match isSameFileSize with        
                |true ->                                  
                    let destinationHash = (computeFileHashFromHashLength destinationFilePath sourceFileHash.Length) |> toLower
                    let sourceHash = sourceFileHash|>toLower
                    logger.Debug( new Msg(fun m -> m.Invoke(sprintf "Comparing destination file (%s) hash [%s] and source file hash [%s]..." (FileSystem.pathValue destinationFilePath) destinationHash sourceHash)|>ignore))
                    (sourceHash = destinationHash) || (String.IsNullOrWhiteSpace(sourceHash))
                | false  -> false
            |false -> false

    let hasSameFileHashBase (filePath:FileSystem.Path, sourceFileHash:string, fileSize:Int64) =
        hasSameFileHashPartial fileExists getFileSize computeFileHashFromHashLength (filePath, sourceFileHash, fileSize)

    let hasSameFileHash (filePath:FileSystem.Path, crc:string, fileSize:Int64) =
        DriverTool.Library.Logging.genericLogger LogLevel.Debug hasSameFileHashBase (filePath, crc, fileSize)

    let fileHashToBase64String fileHash = 
        Convert.ToBase64String(fileHash)

    let base64StringToFileHash fileHashBase64String = 
        Convert.FromBase64String(fileHashBase64String)

        