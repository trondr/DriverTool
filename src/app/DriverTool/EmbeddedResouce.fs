namespace DriverTool

module EmbeddedResouce =
    let logger = Logging.getLoggerByName "EmbeddedResouce"
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
            |Error ex -> Result.Error ex
        static member create value =
            match (createGeneric ResourceName.validator value) with
            |Ok v -> Result.Ok (ResourceName v)
            |Error ex -> Result.Error ex

    open System.Reflection
           
    let debugPrintResourceNames (assembly:Assembly) =
        nullGuard assembly "assembly" |> ignore
        if(logger.IsDebugEnabled) then
            let resourceNames = assembly.GetManifestResourceNames()
            logger.DebugFormat("Assembly {0} has {1} embedded resources.", assembly.GetName(),resourceNames.Length)
            resourceNames
                |> Array.toSeq
                |> Seq.map (fun rn -> logger.Debug("Embeded resource: " + rn))
                |> Seq.toArray
                |> ignore
        ()

    let extractEmbeddedResourceInAssemblyToStream (resourceName:ResourceName, assembly:Assembly) =
        nullGuard assembly "assembly" |> ignore
        let resourceStream = assembly.GetManifestResourceStream(resourceName.Value)
        match resourceStream with
        |Null -> 
            debugPrintResourceNames assembly
            Result.Error (new Exception(String.Format("Failed to extract embedded resource '{0}' from assembly '{1}'.", resourceName.Value, assembly.FullName)))
        |NotNull rs -> Result.Ok rs

    let extractEmbeddedResourceInAssemblyToFile (resourceName:ResourceName, assembly:Assembly, filePath:Path) =
        nullGuard assembly "assembly" |> ignore
        try
            use fileStream = new System.IO.FileStream(filePath.Value,System.IO.FileMode.OpenOrCreate,System.IO.FileAccess.ReadWrite,System.IO.FileShare.None)
            let stream = extractEmbeddedResourceInAssemblyToStream (resourceName, assembly)
            match stream with
            | Ok s -> 
                let bufferLength = Convert.ToInt32(s.Length)
                let mutable buffer : byte[]= Array.zeroCreate bufferLength
                s.Read(buffer,0, buffer.Length) |> ignore
                fileStream.Write(buffer,0,buffer.Length) |> ignore
                s.Dispose()
                FileOperations.ensureFileExists filePath
            |Error ex -> Result.Error ex    
        with
        | _ as ex -> Result.Error ex
        
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
    
    let getAllEmbeddedResourceNames =
        let resourceAssembly = typeof<ThisAssembly>.Assembly
        getAllEmbeddedResourceNamesBase resourceAssembly

    let embeddedResourceExistsBase resourceName resourceAssembly =
        (getAllEmbeddedResourceNamesBase resourceAssembly)
        |> Seq.exists (fun rn -> rn = resourceName)

    let embeddedResourceExists resourceName =
        let resourceAssembly = typeof<ThisAssembly>.Assembly
        embeddedResourceExistsBase resourceName resourceAssembly

    let extractEmbededResouceToFile (resourceName:string , destinationFileName:string) = 
        result {
                
                let! resourceNameObject = 
                    ResourceName.create resourceName
                let! destinationFilePath = 
                    Path.create destinationFileName
                let! parentDirectoryPath = (Path.create (System.IO.Path.GetDirectoryName(destinationFilePath.Value)))
                let! existingParentDirectoryPath = DirectoryOperations.ensureDirectoryExists (parentDirectoryPath, true)
                logger.Info("Verified that directory exists:" + existingParentDirectoryPath.Value)
                let assembly = destinationFilePath.GetType().Assembly
                logger.Info(String.Format("Extracting resource '{0}' -> '{1}'",resourceName, destinationFilePath.Value))
                let! fileResult = 
                    extractEmbeddedResourceInAssemblyToFile (resourceNameObject,assembly, destinationFilePath)
                return fileResult
            }

    let extractEmbeddedResourceBase (resourceName, destinationFolderPath:Path, destinationFileName, assembly: Assembly) =
        result {                
                let! exeResourceName = 
                    ResourceName.create resourceName
                let! exeFilePath = 
                    Path.create (System.IO.Path.Combine(destinationFolderPath.Value, destinationFileName))
                let! fileResult = 
                    extractEmbeddedResourceInAssemblyToFile (exeResourceName,assembly, exeFilePath)
                return fileResult
            }

    let extractEmbeddedResource (resourceName, destinationFolderPath:Path, destinationFileName) =
        let assembly = typeof<ThisAssembly>.Assembly
        extractEmbeddedResourceBase (resourceName, destinationFolderPath, destinationFileName, assembly)
    
    let extractEmbeddedResouceByFileNameBase (fileName, destinationFolderPath:Path, destinationFileName, assembly:Assembly) =
        let resourceNames = 
            getAllEmbeddedResourceNamesBase assembly
            |> Seq.filter (fun rn -> rn.EndsWith(fileName))
            |> Seq.toArray
        match resourceNames.Length with
        | 1 -> 
            extractEmbeddedResourceBase (resourceNames.[0],destinationFolderPath,destinationFileName, assembly)
        | _ -> raise (new Exception("File not found in embedded resource: " + fileName))

    let extractEmbeddedResouceByFileName (fileName, destinationFolderPath:Path, destinationFileName) =
        let assembly = typeof<ThisAssembly>.Assembly
        extractEmbeddedResouceByFileNameBase (fileName, destinationFolderPath, destinationFileName, assembly)

    let mapResourceNamesToFileNames (destinationFolderPath:Path, resourceNames:seq<string>,resourceNameToDirectoryDictionary)=
        let directoryLookDictionary = resourceNameToDirectoryDictionary destinationFolderPath
        resourceNames
        |> Seq.map (fun rn -> 
            let fileName = resourceNameToFileName (rn, directoryLookDictionary)
            match fileName with
            |Some fn -> Some (rn,fn)
            |None -> None
            )
        |> Seq.choose id