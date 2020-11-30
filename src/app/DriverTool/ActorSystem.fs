namespace DriverTool

module ActorSystem =

    open DriverTool.Library
    open DriverTool.Library.Logging
    let logger = getLoggerByName "RunHost"

    let exeAssembly() = 
        let exeAssembly = System.Reflection.Assembly.GetEntryAssembly()        
        match exeAssembly with
        |null -> None
        |_ -> Some exeAssembly

    let exePath() =
        match exeAssembly() with
        |Some assembly -> 
            FileSystem.path assembly.Location
        |None -> Result.Error (toException "Unable to get exe folder path due location of entry assembly is unknown." None)

    
    let exeFolderPath() =
        result{
            let! exePath = exePath()
            return! FileSystem.path (System.IO.Path.GetDirectoryName(FileSystem.pathValue exePath))
        }

    let x86HostPath () =
        result{
            let! exeFolderPath = exeFolderPath()
            let! x86HostPath = PathOperations.combinePaths2 exeFolderPath "DriverTool.x86.Host.exe"
            let! existingx86HostPath = FileOperations.ensureFileExists x86HostPath
            return existingx86HostPath
        }