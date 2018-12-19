namespace DriverTool

module LenovoSystemUpdateCheck =
    open System

    type LenovoSystemUpdateNotInstalledException(message : string) =
        inherit Exception(
            match String.IsNullOrWhiteSpace(message) with
            |false  -> String.Format("Lenovo System update is not installed. {1}", message)
            |true -> "Lenovo System update is not installed"
            )
        new () = LenovoSystemUpdateNotInstalledException(String.Empty)

    let programFilesFolderPathString =
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

    let systemUpdateFolderPathString = 
        System.IO.Path.Combine(programFilesFolderPathString, "Lenovo", "System Update");

    let clientLibraryPathString = 
        System.IO.Path.Combine(systemUpdateFolderPathString, "Client.dll");
    
    let commonLibraryPathString = 
        System.IO.Path.Combine(systemUpdateFolderPathString, "Common.dll");

    let requiredAssemblyFiles = 
            seq {
                yield clientLibraryPathString
                yield commonLibraryPathString
            }

    let lenovoSystemUpdateIsInstalled () =
        requiredAssemblyFiles
        |> Seq.forall(fun filePath -> System.IO.File.Exists(filePath))
    
    let ensureLenovoSystemUpdateIsInstalled () =
        match lenovoSystemUpdateIsInstalled() with
        |true -> Result.Ok true
        |false -> Result.Error (new Exception("Lenovo System Update is not installed."))
