﻿module Checksum

open System
open System.IO
open System.Security.Cryptography
open DriverTool

let getHashAlgorithmFromHashStringLength hashStringLength = 
    match hashStringLength with
        | 32 -> MD5.Create() :> HashAlgorithm
        | 40 -> SHA1.Create() :> HashAlgorithm
        | 64 -> SHA256.Create() :> HashAlgorithm
        | 96 -> SHA384.Create() :> HashAlgorithm
        | 128 -> SHA512.Create() :> HashAlgorithm
        | _  -> SHA512.Create() :> HashAlgorithm

let computeFileHash filePath (hashAlgorithm:HashAlgorithm)  =
    File.ReadAllBytes(filePath)
    |> hashAlgorithm.ComputeHash

let fileHashToString fileHash =
    BitConverter.ToString(fileHash).Replace("-", "").ToLower()

let computeFileHashFromHashLength filePath hashLength =
    (getHashAlgorithmFromHashStringLength hashLength)                
    |> computeFileHash filePath
    |> fileHashToString
 
let fileExists filePath =
    System.IO.File.Exists(filePath)

let getFileSize filePath =
    (new System.IO.FileInfo(filePath)).Length
 
let hasSameFileHashPartial fileExists getFileSize computeFileHashFromHashLength (destinationFilePath:string, sourceFileHash:string, sourceFileSize:Int64) =
        match(fileExists destinationFilePath) with
        | true ->   
            let destinationFileSize = getFileSize destinationFilePath
            let isSameFileSize = 
                (sourceFileSize = destinationFileSize)
            match isSameFileSize with        
            |true ->              
                let destinationFileHash = computeFileHashFromHashLength destinationFilePath sourceFileHash.Length                
                (sourceFileHash.ToLower() = destinationFileHash.ToString().ToLower())                
            | false  -> false
        |false -> false

let hasSameFileHashPlain (filePath:string, sourceFileHash:string, fileSize:Int64) =
    hasSameFileHashPartial fileExists getFileSize computeFileHashFromHashLength (filePath, sourceFileHash, fileSize)

let hasSameFileHash (filePath:string, crc:string, fileSize:Int64) =
    Logging.debugLogger hasSameFileHashPlain (filePath, crc, fileSize)