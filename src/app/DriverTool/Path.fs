namespace DriverTool

open System    
open Paths

type InvalidPathException(path:string, message : string) =
        inherit Exception(
            match String.IsNullOrWhiteSpace(message) with
            |false  -> String.Format("The path '{0}' is not valid. {1}", path, message)
            |true -> String.Format("The path '{0}' is not valid.", path)
            )

type Path private (path : string) = 
    member x.Value = path
    static member createWithContinuation success failure (path:string) : Result<Path, Exception> =
        match path with
        | null -> failure (new InvalidPathException("","Path cannot be null.") :> Exception)
        | "" -> failure (new InvalidPathException("","Path cannot be empty.") :> Exception)
        | path when not (IsValidPath path)  -> failure ((new InvalidPathException(path,"")) :> Exception)
        | _ -> success (Path (System.IO.Path.GetFullPath(path)))
    
    static member create (path : string) =
        let success e = Result.Ok e
        let failure ex = Result.Error ex
        Path.createWithContinuation success failure path
    
    override x.GetHashCode() =
        hash (path)
    
    override x.Equals(b) =
        match b with
        | :? Path as p -> (path) = (p.Value)
        | _ -> false