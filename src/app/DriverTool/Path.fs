namespace DriverTool

open System    
open Paths

type Path private (path : string) = 
    member x.path = path
    static member createWithContinuation success failure (path:string) : Result<'Path, 'Exception> =
        match path with
        | null -> failure (new ArgumentNullException("Path value cannot be null") :> Exception)
        | "" -> failure (new ArgumentException("Path value cannot be empty") :> Exception)
        | path when not (IsValidPath path)  -> failure (new ArgumentException(System.String.Format ("The path '{0}' is not a valid path", path)) :> Exception)
        | _ -> success (Path (System.IO.Path.GetFullPath(path)))
    
    static member create (path : string) =
        let success e = Result.Ok e
        let failure ex = Result.Error ex
        Path.createWithContinuation success failure path
    
    override x.GetHashCode() =
        hash (path)
    
    override x.Equals(b) =
        match b with
        | :? Path as p -> (path) = (p.path)
        | _ -> false