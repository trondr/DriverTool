namespace DriverTool.Library


module x86Host =
    
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

    let startx86HostProcess () =
        match(result{
            let! exePath = x86HostPath()
            let! existingExePath = FileOperations.ensureFileExists exePath
            let processName = ProcessOperations.getProcessName exePath            
            let isAllreadyRunning = ProcessOperations.processIsRunning processName            
            let! hostProcess =  
                match isAllreadyRunning with
                |true -> 
                    let proc = (ProcessOperations.getProcessesByName processName |> Array.last)
                    Result.Ok proc
                |false -> 
                    let procRes = ProcessOperations.startProcess existingExePath "RunHost" None false 
                    procRes
            return hostProcess
        })with
        |Result.Ok hp -> 
            logger.Info(sprintf "x86 host is running (%d)." hp.Id)            
        |Result.Error ex -> 
            logger.Error(sprintf "Failed to start x86 host due to error: %s " (getAccumulatedExceptionMessages ex))
            raise ex

    let stopx86HostProcess () =
        match(result{
            let! exePath = x86HostPath()
            let! existingExePath = FileOperations.ensureFileExists exePath
            let processName = ProcessOperations.getProcessName existingExePath                                    
            let close =
                match (ProcessOperations.processIsRunning processName) with             
                |true ->                                         
                        (ProcessOperations.getProcessesByName processName)
                        |> Array.map(fun p ->
                            logger.Info(sprintf "Closing x86 host (%d)" p.Id)
                            p.Kill()
                            )    
                        |> ignore
                        true
                |false -> 
                    logger.Info(sprintf "x86 host is not running.")
                    false
            return close
        })with
        |Result.Ok closed -> 
            logger.Info(sprintf "x86 host has been stopped (%b)." closed)
        |Result.Error ex -> 
            logger.Error(sprintf "Failed to start x86 host due to error: %s " (getAccumulatedExceptionMessages ex))
            raise ex
        