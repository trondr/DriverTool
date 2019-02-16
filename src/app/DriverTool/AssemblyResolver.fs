namespace DriverTool

module AssemblyResolver=
    open System
    open System.Reflection
    open DriverTool.EmbeddedResouce
    
    let programFilesFolderX86 = 
        System.Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
    
    let lenovoSystemUpdateFolder=
        System.IO.Path.Combine(programFilesFolderX86,"Lenovo","System Update")

    let assemblySearchPaths =
        [|
            lenovoSystemUpdateFolder        
        |]

    let getAssemblyDataFromEmbeddedResource (resourceName:ResourceName, resourceAssembly: Assembly) =
        let dllStreamResult = extractEmbeddedResourceInAssemblyToStream (resourceName, resourceAssembly)
        match dllStreamResult with
        |Error ex ->             
            Result.Error (new Exception(sprintf "Failed to load assembly '%s' from embedded resource '%s' due to: %s" resourceAssembly.FullName resourceName.Value ex.Message,ex))
        |Ok dllStream ->
            use assemblyStream = dllStream
            let assemblyStreamBinaryReader = (new System.IO.BinaryReader(assemblyStream))
            let assemblyData =  assemblyStreamBinaryReader.ReadBytes(int assemblyStream.Length)            
            Result.Ok assemblyData

    let loadAssemblyFromEmbeddedResource (assemblyName:AssemblyName, resourceNameString, resourceAssembly) =
        let resourceName = ResourceName.create resourceNameString
        match resourceName with
        |Error ex -> 
            printf "Failed to load assembly '%s' from embedded resource due to: %s" ex.Message assemblyName.Name
            null
        |Ok rn ->
            let assemblyDataResult = getAssemblyDataFromEmbeddedResource (rn,resourceAssembly)
            match assemblyDataResult with
            |Ok assemblyData -> 
                printf "Assembly loaded from embedded resource: %s" assemblyName.Name
                Assembly.Load(assemblyData)
            |Error ex ->
                printf "Failed to load assembly '%s' from embedded resource due to: %s" ex.Message assemblyName.Name
                null
    
    type AssemblyLoadFunc = string -> Assembly
    type FileExistsFunc = string -> bool
    
    let loadAssemblyFromSearchPathBase (assemblyLoadFunc:AssemblyLoadFunc,fileExistsFunc:FileExistsFunc,assemblySearchPaths:string[], assemblyName:AssemblyName) =
        printf "Attempting to load assembly '%s' from search path..." assemblyName.Name
        let existingAssemblyFilePaths = 
            assemblySearchPaths            
            |>Seq.map(fun searchPath ->                     
                    seq{
                        yield (System.IO.Path.Combine(searchPath, assemblyName.Name + ".dll"))
                        yield (System.IO.Path.Combine(searchPath, assemblyName.Name + ".exe"))
                    }
                )        
            |>Seq.collect id
            |>Seq.filter(fun ap -> fileExistsFunc(ap))
            |>Seq.toArray
        if(existingAssemblyFilePaths.Length > 0) then
            let assemblyFilePath = existingAssemblyFilePaths.[0]
            printfn "Loading assembly '%s' from search path. Path: %s" assemblyName.Name assemblyFilePath
            assemblyLoadFunc(assemblyFilePath)
        else
            printf "Failed to load assembly '%s' from search paths due to assembly file (.dll or .exe) not found."  assemblyName.Name
            null
    
    let loadAssemblyFromSearchPath (assemblySearchPaths:string[], assemblyName:AssemblyName) =
        loadAssemblyFromSearchPathBase (Assembly.LoadFile,System.IO.File.Exists,assemblySearchPaths,assemblyName)

    let assemblyResolveHandler (sender:obj, resolveEventArgs:ResolveEventArgs) : Assembly =
        let assemblyName = (new AssemblyName(resolveEventArgs.Name))
        let assembly =            
            let resourceAssembly = typeof<ThisAssembly>.Assembly
            let resourceNameSpace = typeof<ThisAssembly>.Namespace
            let resourceName = sprintf "%s.Libs.%s.dll" resourceNameSpace assemblyName.Name
            if(resourceName.EndsWith("XmlSerializers.dll")) then
                null
            else
                let resourceNameExists = EmbeddedResouce.embeddedResourceExists resourceName
                if(resourceNameExists) then
                    loadAssemblyFromEmbeddedResource (assemblyName, resourceName, resourceAssembly)                    
                else
                    loadAssemblyFromSearchPath (assemblySearchPaths, assemblyName)
        assembly
