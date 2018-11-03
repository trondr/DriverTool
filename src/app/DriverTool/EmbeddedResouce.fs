namespace DriverTool

module EmbeddedResouce =
    let logger =
        Logging.getLoggerByName "EmbeddedResouce"
    open System
    
    type ResourceName private (resourceName:string) =
        member x.Value = resourceName
        static member private validator value = 
            match value with
            |v when (String.IsNullOrWhiteSpace(v)) -> Result.Error (new Exception("Resource name cannot be empty"))
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
        let resourceNames = assembly.GetManifestResourceNames()
        logger.DebugFormat("Assembly {0} has {1} embedded resources.", assembly.GetName(),resourceNames.Length)
        resourceNames
            |> Array.toSeq
            |> Seq.map (fun rn -> logger.Debug("Embeded resource: " + rn))
            |> ignore
        ()

    let extractEmbeddedResourceToStream (resourceName:ResourceName, assembly:Assembly) =
        nullGuard assembly "assembly" |> ignore
        let resourceStream = assembly.GetManifestResourceStream(resourceName.Value)
        match resourceStream with
        |Null -> 
            debugPrintResourceNames assembly
            Result.Error (new Exception(String.Format("Failed to extract embedded resource '{0}' from assembly '{1}'.", resourceName.Value, assembly.FullName)))
        |NotNull rs -> Result.Ok rs

    let extractEmbeddedResourceToFile (resourceName:ResourceName, assembly:Assembly, filePath:Path) =
        nullGuard assembly "assembly" |> ignore
        try
            use fileStream = new System.IO.FileStream(filePath.Value,System.IO.FileMode.OpenOrCreate,System.IO.FileAccess.ReadWrite,System.IO.FileShare.None)
            let stream = extractEmbeddedResourceToStream (resourceName, assembly)
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
        
        
        