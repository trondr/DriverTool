namespace DriverTool

module CorFlags=
    
    open Common.Logging
    let logger = LogManager.GetLogger("DriverTool.CorFlags")

    let corFlagsExeResourceName =
        match Environment.is64BitProcess with
        |true -> "DriverTool.Tools.CorFlags.x64.CorFlags.exe"
        |false -> "DriverTool.Tools.CorFlags.x86.CorFlags.exe"
    
    let corFlagsExe = 
        System.IO.Path.Combine(System.IO.Path.GetTempPath(),"CorFlags.exe")

    let extractCorFlagsExe () =        
        EmbeddedResource.extractEmbededResouceToFile (corFlagsExeResourceName, corFlagsExe)
    
    let prefer32BitSet (assemblyFilePath:FileSystem.Path) =
        result{
            use corFlagsExe = new EmbeddedResource.ExtractedEmbeddedResource("CorFlags.exe", logger)
            let! coreFlagsExePath = corFlagsExe.FilePath            
            let! exitCodeResult = ProcessOperations.startConsoleProcess (coreFlagsExePath,sprintf "\"%s\" /32BITPREF+" (FileSystem.pathValue assemblyFilePath),null,-1,null,null,false)            
            return exitCodeResult
        }  
        
    let prefer32BitClear (assemblyFilePath:FileSystem.Path) =
        result{
            use corFlagsExe = new EmbeddedResource.ExtractedEmbeddedResource("CorFlags.exe", logger)
            let! coreFlagsExePath = corFlagsExe.FilePath            
            let! exitCodeResult = ProcessOperations.startConsoleProcess (coreFlagsExePath,sprintf "\"%s\" /32BITPREF-" (FileSystem.pathValue assemblyFilePath),null,-1,null,null,false)            
            return exitCodeResult
        }        