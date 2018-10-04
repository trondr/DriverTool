module Checksum

open System
open System.IO
open System.Security.Cryptography

let getHashAlgorithmFromHashStringLength hashStringLength = 
    match hashStringLength with
        | 32 -> MD5.Create() :> HashAlgorithm
        | 40 -> SHA1.Create() :> HashAlgorithm
        | 64 -> SHA256.Create() :> HashAlgorithm
        | 96 -> SHA384.Create() :> HashAlgorithm
        | 128 -> SHA512.Create() :> HashAlgorithm
        | _  -> SHA512.Create() :> HashAlgorithm

let computeHash filePath (hashAlgorithm:HashAlgorithm)  =
    let fileHash = File.ReadAllBytes(filePath)|> hashAlgorithm.ComputeHash
    fileHash

let fileHashToString fileHash =
    let fileHashString = 
        BitConverter.ToString(fileHash).Replace("-", "").ToLower()
    fileHashString

let hasSameFileHash (filePath:string, crc:string, fileSize:Int64) =
    let hasSameFileHash = 
        match(System.IO.File.Exists(filePath)) with
        | true ->   
            let actualSize = (new System.IO.FileInfo(filePath)).Length
            let isSameFileSize = 
                (fileSize = actualSize)
            match isSameFileSize with        
            |true -> 
                use hashAlgorithm = getHashAlgorithmFromHashStringLength crc.Length                
                let fileHash = computeHash filePath hashAlgorithm
                let fileHashString = fileHashToString fileHash                
                (crc.ToLower() = fileHashString)                
            | false  -> false
        |false -> false
    hasSameFileHash