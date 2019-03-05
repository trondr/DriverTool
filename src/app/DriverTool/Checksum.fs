namespace DriverTool

module Checksum=
    open System
    open System.IO
    open System.Security.Cryptography
    open DriverTool
    open DriverTool.FileOperations
    
    let logger = Logging.getLoggerByName("Checksum")

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
        Logging.genericLogger Logging.LogLevel.Debug computeFileHashFromHashLengthBase filePath hashLength
 
    let hasSameFileHashPartial fileExists getFileSize computeFileHashFromHashLength (destinationFilePath:FileSystem.Path, sourceFileHash:string, sourceFileSize:Int64) =
            match(fileExists destinationFilePath) with
            | true ->   
                let destinationFileSize = getFileSize destinationFilePath
                let isSameFileSize = 
                    (sourceFileSize = destinationFileSize) || (sourceFileSize = 0L)
                match isSameFileSize with        
                |true ->              
                    let destinationHash = computeFileHashFromHashLength destinationFilePath sourceFileHash.Length
                    let sourceHash = sourceFileHash|>toLower
                    if(logger.IsDebugEnabled) then logger.Debug(sprintf "Comparing destination file (%s) hash [%s] and source file hash [%s]..." (FileSystem.pathValue destinationFilePath) destinationHash sourceHash)
                    (sourceHash = destinationHash)                
                | false  -> false
            |false -> false

    let hasSameFileHashBase (filePath:FileSystem.Path, sourceFileHash:string, fileSize:Int64) =
        hasSameFileHashPartial fileExists getFileSize computeFileHashFromHashLength (filePath, sourceFileHash, fileSize)

    let hasSameFileHash (filePath:FileSystem.Path, crc:string, fileSize:Int64) =
        Logging.genericLogger Logging.LogLevel.Debug hasSameFileHashBase (filePath, crc, fileSize)

    let fileHashToBase64String fileHash = 
        Convert.ToBase64String(fileHash)

    let base64StringToFileHash fileHashBase64String = 
        Convert.FromBase64String(fileHashBase64String)

        