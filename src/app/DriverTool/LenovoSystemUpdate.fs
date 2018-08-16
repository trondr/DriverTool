namespace DriverTool

module LenovoSystemUpdate =
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

    let isLenovoSystemUpdateInstalled = 
        match System.IO.File.Exists(clientLibraryPathString) with
        |true -> 
            System.Reflection.Assembly.LoadFile(commonLibraryPathString) |> ignore
            System.Reflection.Assembly.LoadFile(clientLibraryPathString) |> ignore
            true
        |false -> false
        
    
    
