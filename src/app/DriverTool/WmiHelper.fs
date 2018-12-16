namespace DriverTool

module WmiHelper =
    open System.Management
    open Microsoft.FSharp.Collections    
    open System

    let getWmiProperty (className : string) (propertyName : string) : Result<'T, Exception> = 
        try
            use managementClass = new ManagementClass(className)
            use managementObjectCollection = managementClass.GetInstances()
            if(managementObjectCollection.Count = 0) then                
                Result.Error (new System.Exception(String.Format("No instances of wmi class '{0}' was found.", className)))
            else
                let value = 
                    managementObjectCollection
                    |> Seq.cast
                    |> Seq.map(fun (x: ManagementObject) -> x.GetPropertyValue(propertyName))
                    |> Seq.head
                Result.Ok (value :?> 'T)
        with
           | _ as ex -> Result.Error ex

