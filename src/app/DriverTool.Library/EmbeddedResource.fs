namespace DriverTool.Library

module EmbeddedResource =
    open DriverTool.Library.Logging
    open DriverTool.Library.F    
    let logger = Logging.getLoggerByName "EmbeddedResource"
    open System
        
    type ResourceName private (resourceName:string) =
        member x.Value = resourceName
        static member private validator value = 
            match value with
            |v when (String.IsNullOrWhiteSpace(v)) -> Result.Error (new Exception("Resource name cannot be empty."))
            |v -> Result.Ok v
        static member createWithContinuation success failure value =
            match (createWithContinuationGeneric success failure ResourceName.validator value) with
            |Ok v -> Result.Ok (ResourceName v)
            |Result.Error ex -> Result.Error ex
        static member create value =
            match (createGeneric ResourceName.validator value) with
            |Ok v -> Result.Ok (ResourceName v)
            |Result.Error ex -> Result.Error ex

    open System.Reflection
           
    let debugPrintResourceNames (assembly:Assembly) =
        nullGuard assembly "assembly" |> ignore
        if(logger.IsDebugEnabled) then
            let resourceNames = assembly.GetManifestResourceNames()
            logger.Debug(sprintf "Assembly '%s' has %i embedded resources." assembly.FullName resourceNames.Length)
            resourceNames
                |> Array.toSeq
                |> Seq.map (fun rn -> logger.Debug(sprintf "Embeded resource: %s"  rn))
                |> Seq.toArray
                |> ignore
        ()

    let extractEmbeddedResourceInAssemblyToStream (resourceName:ResourceName, assembly:Assembly) =
        nullGuard assembly "assembly" |> ignore
        let resourceStream = assembly.GetManifestResourceStream(resourceName.Value)
        match resourceStream with
        |Null -> 
            debugPrintResourceNames assembly
            Result.Error (new Exception(sprintf "Failed to extract embedded resource '%s' from assembly '%s'." resourceName.Value assembly.FullName))
        |NotNull rs -> Result.Ok rs

    let extractEmbeddedResourceInAssemblyToFile (resourceName:ResourceName, assembly:Assembly, filePath:FileSystem.Path) =
        nullGuard assembly "assembly" |> ignore
        try
            use fileStream = new System.IO.FileStream(FileSystem.pathValue filePath,System.IO.FileMode.OpenOrCreate,System.IO.FileAccess.ReadWrite,System.IO.FileShare.None)
            let stream = extractEmbeddedResourceInAssemblyToStream (resourceName, assembly)
            match stream with
            | Ok s -> 
                let bufferLength = Convert.ToInt32(s.Length)
                let mutable buffer : byte[]= Array.zeroCreate bufferLength
                s.Read(buffer,0, buffer.Length) |> ignore
                fileStream.Write(buffer,0,buffer.Length) |> ignore
                s.Dispose()
                FileOperations.ensureFileExists filePath
            |Result.Error ex -> Result.Error ex    
        with
        | _ as ex -> Result.Error (toException (sprintf "Failed to extract embedded resource '%A' from assembly '%s' due to %s" resourceName assembly.FullName ex.Message) (Some ex))
        
    let resourceNameToPartialResourceNames (resourceName:string) =
        let split = resourceName.Split([|'.'|])
        let length = split.Length
        seq{
            for i in 0..(length-1) do
                let partialResourceName = System.String.Join<string>(".",split.[0..i])
                yield partialResourceName
        } 
        |> Seq.toArray
        |> Seq.rev
        
    let resourceNameToFileName (resourceName:string, dictionary: System.Collections.Generic.IDictionary<string,string>) =          
            let partialResourceNames = resourceNameToPartialResourceNames resourceName
            let directoryPartialName =
                partialResourceNames |> Seq.tryFind (fun x -> dictionary.ContainsKey(x))
            let fileName = 
                match directoryPartialName with
                | Some pn -> 
                    let fileN = System.IO.Path.Combine(dictionary.[pn],resourceName.Replace(pn,"").Trim('.'))
                    Some fileN
                | None -> None
            fileName
    
    let getAllEmbeddedResourceNamesBase (assembly:Assembly) =        
        assembly.GetManifestResourceNames()    
    
    let getAllEmbeddedResourceNames resourceAssembly =        
        getAllEmbeddedResourceNamesBase resourceAssembly

    let embeddedResourceExistsBase resourceName resourceAssembly =
        (getAllEmbeddedResourceNamesBase resourceAssembly)
        |> Seq.exists (fun rn -> rn = resourceName)

    let embeddedResourceExists resourceAssembly resourceName =        
        embeddedResourceExistsBase resourceName resourceAssembly

    let extractEmbededResouceToFile (resourceAssembly, resourceName:string , destinationFileName:string) = 
        result {
                let! resourceNameObject = ResourceName.create resourceName
                let! destinationFilePath = FileSystem.path destinationFileName
                let! parentDirectoryPath = (FileSystem.path (System.IO.Path.GetDirectoryName(FileSystem.pathValue destinationFilePath)))
                let! existingParentDirectoryPath = DirectoryOperations.ensureDirectoryExists true parentDirectoryPath
                logger.Info("Verified that directory exists:" + FileSystem.pathValue existingParentDirectoryPath)
                
                logger.Info(sprintf "Extracting resource '%s' -> '%s'" resourceName (FileSystem.pathValue destinationFilePath))
                let! fileResult = 
                    extractEmbeddedResourceInAssemblyToFile (resourceNameObject,resourceAssembly, destinationFilePath)
                return fileResult
            }

    let extractEmbeddedResourceBase (resourceName, destinationFolderPath:FileSystem.Path, destinationFileName, assembly: Assembly) =
        result {                
                let! exeResourceName = 
                    ResourceName.create resourceName
                let! exeFilePath = 
                    FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue destinationFolderPath, destinationFileName))
                let! fileResult = 
                    extractEmbeddedResourceInAssemblyToFile (exeResourceName,assembly, exeFilePath)
                return fileResult
            }

    let extractEmbeddedResource (resourceAssembly ,resourceName, destinationFolderPath:FileSystem.Path, destinationFileName) =        
        extractEmbeddedResourceBase (resourceName, destinationFolderPath, destinationFileName, resourceAssembly)
    
    let extractEmbeddedResourceByFileNameBase (fileName, destinationFolderPath:FileSystem.Path, destinationFileName, assembly:Assembly) =
        let resourceNames = 
            getAllEmbeddedResourceNamesBase assembly
            |>Seq.filter (fun rn -> rn.EndsWith(fileName))
            |>Seq.sort
            |>Seq.toArray            
        match resourceNames.Length > 0 with
        | true -> 
            extractEmbeddedResourceBase (resourceNames.[0],destinationFolderPath,destinationFileName, assembly)
        | false -> raise (new Exception(sprintf "File '%s' not found in embedded resource of assembly '%s'." fileName assembly.FullName))

    let extractEmbeddedResourceByFileName (resourceAssembly,fileName, destinationFolderPath:FileSystem.Path, destinationFileName) =        
        extractEmbeddedResourceByFileNameBase (fileName, destinationFolderPath, destinationFileName, resourceAssembly)

    let mapResourceNamesToFileNames (destinationFolderPath:FileSystem.Path, resourceNames:seq<string>,resourceNameToDirectoryDictionary)=
        let directoryLookDictionary = resourceNameToDirectoryDictionary destinationFolderPath
        resourceNames
        |> Seq.map (fun rn -> 
            let fileName = resourceNameToFileName (rn, directoryLookDictionary)
            match fileName with
            |Some fn -> Some (rn,fn)
            |None -> None
            )
        |> Seq.choose id

    [<AllowNullLiteral>]
    type ExtractedEmbeddedResourceByFileName(resourceAssembly,fileName, logger:Common.Logging.ILog) =
        let tempFolderPath = 
            result {
                let! nonExistingTempFolderPath = FileSystem.path (System.IO.Path.Combine(System.IO.Path.GetTempPath(),(Guid.NewGuid().ToString())))
                let! existingTempFolderPath = DirectoryOperations.ensureDirectoryExists true nonExistingTempFolderPath
                return existingTempFolderPath
            }

        let tempFilePath =
            result{
                let! folderPath = tempFolderPath                
                let! extractedFile = extractEmbeddedResourceByFileNameBase (fileName,folderPath,fileName,resourceAssembly)
                return extractedFile
            }

        member this.FilePath = tempFilePath
        interface IDisposable with
            member x.Dispose() = 
                if(logger.IsDebugEnabled) then ( logger.Debug(sprintf "Disposing extracted embedded resource '%A'" tempFilePath) )
                match (result{
                    let! folderPath = tempFolderPath
                    let! deleted = DirectoryOperations.deleteDirectoryIfExists folderPath
                    return deleted                                                
                }) with
                |Result.Ok v -> ()
                |Result.Error ex -> raise ex           

    [<AllowNullLiteral>]
    type ExtractedEmbeddedResource(resourceAssebly,resourceName,fileName, logger:Common.Logging.ILog) =
        let tempFolderPath = 
            result {
                let! nonExistingTempFolderPath = FileSystem.path (System.IO.Path.Combine(System.IO.Path.GetTempPath(),(Guid.NewGuid().ToString())))
                let! existingTempFolderPath = DirectoryOperations.ensureDirectoryExists true nonExistingTempFolderPath
                return existingTempFolderPath
            }

        let tempFilePath =
            result{
                let! folderPath = tempFolderPath                
                let! extractedFile = extractEmbeddedResourceBase (resourceName,folderPath,fileName,resourceAssebly)
                return extractedFile
            }

        member this.FilePath = tempFilePath
        interface IDisposable with
            member x.Dispose() = 
                if(logger.IsDebugEnabled) then ( logger.Debug(sprintf "Disposing extracted embedded resource '%A'" tempFilePath))
                match (result{
                    let! folderPath = tempFolderPath
                    let! deleted = DirectoryOperations.deleteDirectoryIfExists folderPath
                    return deleted                                                
                }) with
                |Result.Ok v -> ()
                |Result.Error ex -> raise ex