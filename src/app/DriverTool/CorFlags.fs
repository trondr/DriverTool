﻿namespace DriverTool

module CorFlags=
    
    let corFlagsExeResourceName =
        match Environment.is64BitProcess with
        |true -> "DriverTool.Tools.CorFlags.x64.CorFlags.exe"
        |false -> "DriverTool.Tools.CorFlags.x86.CorFlags.exe"
    
    let corFlagsExe = 
        System.IO.Path.Combine(System.IO.Path.GetTempPath(),"CorFlags.exe")

    let extractCorFlagsExe () =        
        EmbeddedResouce.extractEmbededResouceToFile (corFlagsExeResourceName, corFlagsExe)
    
    let cleanupCorFlagsExe () =
        if(System.IO.File.Exists(corFlagsExe)) then
            System.IO.File.Delete(corFlagsExe)
            
    open System

    let prefer32BitSet (assemblyFilePath:Path) =
        result{
            let! corFlagsExe = extractCorFlagsExe ()
            let! exitCodeResult = ProcessOperations.startConsoleProcess (corFlagsExe.Value,String.Format("\"{0}\" /32BITPREF+", assemblyFilePath.Value),null,-1,null,null,false)
            cleanupCorFlagsExe()
            return exitCodeResult
        }  
        
    let prefer32BitClear (assemblyFilePath:Path) =
        result{
            let! corFlagsExe = extractCorFlagsExe ()
            let! exitCodeResult = ProcessOperations.startConsoleProcess (corFlagsExe.Value,String.Format("\"{0}\" /32BITPREF-", assemblyFilePath.Value),null,-1,null,null,false)
            cleanupCorFlagsExe()
            return exitCodeResult
        }        