namespace DriverTool

module AssemblyResolver=
    open System
    open System.Reflection
    open System.Collections.Generic
    open DriverTool.EmbeddedResouce
    
    let mutable resolvedAssemblies = 
        new Dictionary<string,Assembly>()

    let loadAssemblyFromEmbeddedResource (assemblyName,resourceNameString,resourceAssembly) =
        let resourceName = ResourceName.create resourceNameString
        match resourceName with
        |Error ex -> 
            System.Console.WriteLine("Failed to load assembly from embedded resource due to: " + ex.Message)
            raise ex
        |Ok rn ->
            let dllStreamResult = extractEmbeddedResourceInAssemblyToStream (rn,resourceAssembly)
            match dllStreamResult with
            |Error ex -> 
                System.Console.WriteLine(String.Format("Failed to load assembly '{0}' from embedded resource {1} due to: {2}",assemblyName,rn.Value, ex.Message))
                raise ex
            |Ok dllStream ->
                let assemblyStreamBinaryReader =
                    (new System.IO.BinaryReader(dllStream))
                let assemblyData = 
                    assemblyStreamBinaryReader.ReadBytes(int dllStream.Length)
                dllStream.Dispose()
                let assembly = Assembly.Load(assemblyData)
                System.Console.WriteLine("Assembly loaded from embedded resource: " + assemblyName)
                assembly

    let assemblyResolveHandler (sender:obj, resolveEventArgs:ResolveEventArgs) : Assembly =
        let assemblyName = (new AssemblyName(resolveEventArgs.Name)).Name
        let assembly =
            if(resolvedAssemblies.ContainsKey(assemblyName)) then
                (resolvedAssemblies.[assemblyName])
            else
                let resourceAssembly = typeof<ThisAssembly>.Assembly
                let resourceNameSpace = typeof<ThisAssembly>.Namespace
                let resourceName = String.Format("{0}.Libs.{1}.dll",resourceNameSpace,assemblyName)
                if(resourceName.EndsWith("XmlSerializers.dll")) then
                    null
                else
                    loadAssemblyFromEmbeddedResource (assemblyName,resourceName,resourceAssembly)   
        assembly
