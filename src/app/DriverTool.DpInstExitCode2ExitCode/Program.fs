// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open DriverTool

[<EntryPoint>]
let main argv =
    
    if((Array.length argv) <> 1) then
        printfn "%s" "Invalid command line."
        printfn "%s" "Usage: DriverTool.DpInstExitCode2ExitCode.exe <exit code>"
        printfn "%s" "Example: DriverTool.DpInstExitCode2ExitCode.exe 2"
        13
    else
        let dpInstexitCodeString = argv.[0]
        let exitCode =
            match(string2uInt dpInstexitCodeString) with
            |Result.Ok dpec ->
                let dpInstExitInfo = 
                    DriverTool.DpInstExitCode.toDpInstExitCodeInfo (uint dpec)
                printfn "DpInst exit code: 0x%X (%d)" dpInstExitInfo.DpInstExitCode dpInstExitInfo.DpInstExitCode
                printfn "Number of driver packages that could not be installed: %d" dpInstExitInfo.CouldNotBeInstalledCount
                printfn "Number of driver packages that have been copied to the driver store but haven't been installed on a device: %d" dpInstExitInfo.CopiedToDriverStoreCount
                printfn "Number of driver packages that have been installed on a device: %d" dpInstExitInfo.InstalledCount
                printfn "Could not be installed: %b" dpInstExitInfo.CouldNotBeInstalled
                printfn "Reboot needed: %b" dpInstExitInfo.RebootNeeded
                int dpInstExitInfo.ExitCode
            |Result.Error ex -> 
                printfn "ERROR: Failed to convert dup exit code '%s' due to: %s" dpInstexitCodeString ex.Message
                13//Invalid data
        exitCode
            
    