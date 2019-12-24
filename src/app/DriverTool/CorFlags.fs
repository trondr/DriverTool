namespace DriverTool

module CorFlags=
    
    open Common.Logging
    open DriverTool.Library.F

    let logger = LogManager.GetLogger("CorFlags")

    let corFlagsExeResourceName =
        match Environment.is64BitOperatingSystem with
        |true -> "DriverTool.Tools.CorFlags.x64.CorFlags.exe"
        |false -> "DriverTool.Tools.CorFlags.x86.CorFlags.exe"
    
    let corFlagsExe = 
        System.IO.Path.Combine(System.IO.Path.GetTempPath(),"CorFlags.exe")

    let prefer32BitSet (assemblyFilePath:FileSystem.Path) =
        result{
            use corFlagsExe = new DriverTool.EmbeddedResource.ExtractedEmbeddedResource(corFlagsExeResourceName, "CorFlags.exe", logger)
            let! coreFlagsExePath = corFlagsExe.FilePath            
            let! exitCodeResult = ProcessOperations.startConsoleProcess (coreFlagsExePath,sprintf "\"%s\" /32BITPREF+" (FileSystem.pathValue assemblyFilePath),null,-1,null,null,false)            
            return exitCodeResult
        }  
        
    let prefer32BitClear (assemblyFilePath:FileSystem.Path) =
        result{
            use corFlagsExe = new DriverTool.EmbeddedResource.ExtractedEmbeddedResource(corFlagsExeResourceName,"CorFlags.exe", logger)
            let! coreFlagsExePath = corFlagsExe.FilePath            
            let! exitCodeResult = ProcessOperations.startConsoleProcess (coreFlagsExePath,sprintf "\"%s\" /32BITPREF-" (FileSystem.pathValue assemblyFilePath),null,-1,null,null,false)            
            return exitCodeResult
        }        