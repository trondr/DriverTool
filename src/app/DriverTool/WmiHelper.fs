namespace DriverTool

module WmiHelper =
    open System.Management
    open Microsoft.FSharp.Collections    
    open System

    let getWmiPropertyDefault (className : string) (propertyName : string) : Result<'T, Exception> = 
        try
            use managementClass = new ManagementClass(className)
            use managementObjectCollection = managementClass.GetInstances()
            if(managementObjectCollection.Count = 0) then                
                Result.Error (new System.Exception(sprintf "No instances of wmi class '%s' was found." className))
            else
                let value = 
                    managementObjectCollection
                    |> Seq.cast
                    |> Seq.map(fun (x: ManagementObject) -> x.GetPropertyValue(propertyName))
                    |> Seq.head
                Result.Ok (value :?> 'T)
        with
           | _ as ex -> Result.Error ex
    
    let getWmiProperty (nameSpace:string) (className : string) (propertyName : string) : Result<'T, Exception> = 
        try
            let managementPath = new ManagementPath(nameSpace)
            let scope = new ManagementScope(managementPath)
            let queryString = "SELECT * FROM " + className
            let query = new ObjectQuery(queryString)
            use searcher = new ManagementObjectSearcher(scope,query)
            use managementObjectCollection = searcher.Get()
            if(managementObjectCollection.Count = 0) then                
                Result.Error (new System.Exception(sprintf "No instances of wmi class '%s' was found." className))
            else
                let value = 
                    managementObjectCollection
                    |> Seq.cast
                    |> Seq.map(fun (x: ManagementObject) -> x.GetPropertyValue(propertyName))
                    |> Seq.head
                Result.Ok (value :?> 'T)
        with
           | _ as ex -> Result.Error ex

