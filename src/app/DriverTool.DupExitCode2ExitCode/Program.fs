open DriverTool.DupExitCode2ExitCode.Dup
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
            match (string2Int dupExitCodeString) with        
            |Ok dupExitCode ->                                 
                let dupExitCodeMessage = dupExitCode2Message dupExitCode
                printfn "Dup exit code: %s (%s)" (dupExitCode.ToString()) dupExitCodeMessage
                let exitCode = dupExitCode2ExitCode dupExitCode
                exitCode
            |Error ex -> 
                    printfn "ERROR: Failed to convert dup exit code '%s' due to: %s" dupExitCodeString ex.Message
                    13 //Invalid data            
        let exitCodeMessage = win32ErrorCode2Message exitCode
        printfn "Win32 exit code: %s (%s)" (exitCode.ToString()) exitCodeMessage
        exitCode