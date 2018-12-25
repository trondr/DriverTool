﻿open DriverTool.DupExitCode2ExitCode.Dup
open DriverTool
open F

[<EntryPoint>]
let main argv = 
    
    let argvLength = Array.length argv    
    if(argvLength <> 1) then
        printfn "%s" "Invalid command line."
        printfn "%s" "Usage: DriverTool.DupExitCode2ExitCode.exe <exit code>"
        printfn "%s" "Example: DriverTool.DupExitCode2ExitCode.exe 2"
        13
    else
        let dupExitCodeString = argv.[0]
        let exitCode =
            let exitCodeResult =            
                result{            
                    let! dupExitCode = string2Int dupExitCodeString
                    let dupExitCodeMessage = dupExitCode2Message dupExitCode
                    printfn "Dup exit code: %s (%s)" dupExitCodeString dupExitCodeMessage
                    let exitCode = dupExitCode2ExitCode dupExitCode
                    return exitCode
                }                
            match(exitCodeResult) with
            |Error ex -> 
                printfn "ERROR: Failed to convert dup exit code '%s' due to: %s" dupExitCodeString ex.Message
                13 //Invalid data
            |Ok ec ->                 
                ec
        let exitCodeMessage = win32ErrorCode2Message exitCode
        printfn "Win32 exit code: %s (%s)" (exitCode.ToString()) exitCodeMessage
        exitCode