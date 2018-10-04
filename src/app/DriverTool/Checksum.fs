module Checksum

open System
open System.IO
open System.Security.Cryptography

let hasSameFileHash (filePath:string, crc:string) =
    match(System.IO.File.Exists(filePath)) with
    | true ->   
        use hashAlgorithm = 
            match crc.Length with
            | 32 -> MD5.Create() :> HashAlgorithm
            | 40 -> SHA1.Create() :> HashAlgorithm
            | 64 -> SHA256.Create() :> HashAlgorithm
            | 96 -> SHA384.Create() :> HashAlgorithm
            | 128 -> SHA512.Create() :> HashAlgorithm
            | _  -> SHA512.Create() :> HashAlgorithm
        let fileHash = File.ReadAllBytes(filePath)|> hashAlgorithm.ComputeHash
        let fileHashString = 
            BitConverter.ToString(fileHash).Replace("-", "").ToLower()
        (crc.ToLower() = fileHashString)        
    |false -> false
