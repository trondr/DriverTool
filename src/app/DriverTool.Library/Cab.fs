namespace DriverTool.Library

module Cab = 

    open System
    open DriverTool.Library.Environment

    /// <summary>
    /// Path to expand.exe
    /// </summary>
    let expandExe =
        System.IO.Path.Combine(nativeSystemFolder,"expand.exe")

    let expandExeExitCodeToResult cabFilePath exitCode =
        let expandResult =
                if(exitCode = 0) then
                    Result.Ok exitCode
                else                    
                    toErrorResultEx (sprintf "Failed to expand CAB file '%A'. Expand.exe exited with error code: %i. Suggestion: Delete the CAB file and any extracted files, and try again." cabFilePath exitCode)
        expandResult

