namespace DriverTool.Library

//
//  //Example of setting up the assembly resolver in Program.fs:
//
//  let resolveEventHandler =
//      new ResolveEventHandler(fun s e -> DriverTool.Messages.AssemblyResolver.assemblyResolveHandler(s,e)) 
//
//  let setup() =       
//      AppDomain.CurrentDomain.add_AssemblyResolve(resolveEventHandler)
//      AppDomain.CurrentDomain.UnhandledException.AddHandler(fun _ x -> printfn "%s" (x.ExceptionObject.ToString()))
//  let teardown() =
//      AppDomain.CurrentDomain.remove_AssemblyResolve(resolveEventHandler)
//      AppDomain.CurrentDomain.UnhandledException.RemoveHandler(fun _ x -> printfn "%s" (x.ExceptionObject.ToString()))

//  [<EntryPoint>]
//  [< STAThread >]
//  let main argv =
//      setup()
//      let exitCode = runCommand argv
//      teardown()
//      exitCode // return an integer exit code
//

module AssemblyResolver=
    open System
    open System.Reflection
    open DriverTool.Library.EmbeddedResource
        
    //Add any custom search paths to this value to instruct the resolver to look for the assembly in these locations
    let assemblySearchPaths =
        [|
            
        |]

    let getAssemblyDataFromEmbeddedResource (resourceName:ResourceName, resourceAssembly: Assembly) =
        let dllStreamResult = extractEmbeddedResourceInAssemblyToStream (resourceName, resourceAssembly)
        match dllStreamResult with
        |Result.Error ex ->             
            Result.Error (new Exception(sprintf "Failed to load assembly '%s' from embedded resource '%s' due to: %s" resourceAssembly.FullName resourceName.Value ex.Message,ex))
        |Ok dllStream ->
            use assemblyStream = dllStream
            let assemblyStreamBinaryReader = (new System.IO.BinaryReader(assemblyStream))
            let assemblyData =  assemblyStreamBinaryReader.ReadBytes(int assemblyStream.Length)            
            Result.Ok assemblyData

    let loadAssemblyFromEmbeddedResource (assemblyName:AssemblyName, resourceNameString, resourceAssembly) =        
        let resourceName = ResourceName.create resourceNameString
        match resourceName with
        |Result.Error ex -> 
            #if DEBUG
            printfn "Failed to load assembly '%s' from embedded resource due to: %s" ex.Message assemblyName.Name
            #endif
            null
        |Ok rn ->
            let assemblyDataResult = getAssemblyDataFromEmbeddedResource (rn,resourceAssembly)
            match assemblyDataResult with
            |Ok assemblyData -> 
                #if DEBUG
                printfn "Assembly loaded from embedded resource: %s" assemblyName.Name
                #endif
                Assembly.Load(assemblyData)
            |Result.Error ex ->
                #if DEBUG
                printfn "Failed to load assembly '%s' from embedded resource due to: %s" ex.Message assemblyName.Name
                #endif
                null
    
    type AssemblyLoadFunc = string -> Assembly
    type FileExistsFunc = string -> bool
    
    let loadAssemblyFromSearchPathBase (assemblyLoadFunc:AssemblyLoadFunc,fileExistsFunc:FileExistsFunc,assemblySearchPaths:string[], assemblyName:AssemblyName) =        
        #if DEBUG
        printfn "Attempting to load assembly '%s' from search path..." assemblyName.Name
        #endif
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
            #if DEBUG
            printfn "Loading assembly '%s' from search path. Path: %s" assemblyName.Name assemblyFilePath
            #endif
            assemblyLoadFunc(assemblyFilePath)
        else
            #if DEBUG
            printfn "Failed to load assembly '%s' from search paths due to assembly file (.dll or .exe) not found."  assemblyName.Name
            #endif
            null
    
    let loadAssemblyFromSearchPath (assemblySearchPaths:string[], assemblyName:AssemblyName) =
        loadAssemblyFromSearchPathBase (Assembly.LoadFile,System.IO.File.Exists,assemblySearchPaths,assemblyName)

    let assemblyResolveHandlerPartial (resourceAssembly:Assembly) (resourceNameSpace:string) (assemblySearchPaths:string array) (sender:obj, resolveEventArgs:ResolveEventArgs) : Assembly =
        let assemblyName = (new AssemblyName(resolveEventArgs.Name))
        let assembly =            
            let resourceName = sprintf "%s.Libs.%s.dll" resourceNameSpace assemblyName.Name
            if(resourceName.EndsWith("XmlSerializers.dll")) then
                null
            else
                let resourceNameExists = EmbeddedResource.embeddedResourceExists resourceAssembly resourceName
                if(resourceNameExists) then
                    loadAssemblyFromEmbeddedResource (assemblyName, resourceName, resourceAssembly)                    
                else
                    loadAssemblyFromSearchPath (assemblySearchPaths, assemblyName)
        assembly