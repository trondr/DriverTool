namespace DriverTool

module Path =

    open System    
    open Paths

    type _T = Path of string

    let createWithContinuation success failure (path:string) : Result<'Path, 'Exception> =
        match path with
        | null -> failure (new ArgumentNullException("Path value cannot be null") :> Exception)
        | "" -> failure (new ArgumentException("Path value cannot be empty") :> Exception)
        | path when not (IsValidPath path)  -> failure (new ArgumentException(System.String.Format ("The path '{0}' is not a valid path", path)) :> Exception)
        | _ -> success (Path (System.IO.Path.GetFullPath(path)))
            
    let create (path:string) =
        let success e = Result.Ok e
        let failure ex = Result.Error ex
        createWithContinuation success failure path
