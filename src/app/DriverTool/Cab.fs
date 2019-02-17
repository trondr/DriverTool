namespace DriverTool

module Cab = 

    open System

    let expandExeExitCodeToResult cabFilePath exitCode =
        let expandResult =
                if(exitCode = 0) then
                    Result.Ok exitCode
                else
                    Result.Error (new Exception(sprintf "Failed to expand CAB file '%A'. Expand.exe exited with error code: %i. Suggestion: Delete the CAB file and any extracted files, and try again." cabFilePath exitCode))
        expandResult

