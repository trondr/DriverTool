namespace DriverTool

module DpInstExitCode =

    type DpInstExiCodeInfo = 
        {
            DpInstExitCode: uint
            InstalledCount: uint
            CouldNotBeInstalledCount : uint
            CopiedToDriverStoreCount : uint
            CouldNotBeInstalled : bool
            RebootNeeded: bool
            ExitCode: uint
        }


    let getInstalledCount (dpInstExitCode:uint) = 
        (uint 0x000000FF) &&& dpInstExitCode

    let getCopiedToDriverStoreCount (dpInstExitCode:uint) = 
        (uint 0x0000FF00) &&& dpInstExitCode >>> 8

    let getCouldNotBeInstalledCount (dpInstExitCode:uint) = 
        (uint 0x0000FF00) &&& dpInstExitCode >>> 16

    let getRebootNeeded (dpInstExitCode:uint) =
        (dpInstExitCode &&& (uint 0x40000000)) > 0u

    let getCouldNotBeInstalled (dpInstExitCode:uint) =
        (dpInstExitCode &&& (uint 0x80000000)) > 0u

    let toDpInstExitCodeInfo dpInstExitCode =
        {
            DpInstExitCode = dpInstExitCode
            InstalledCount = getInstalledCount dpInstExitCode
            CouldNotBeInstalledCount = getCouldNotBeInstalledCount dpInstExitCode
            CopiedToDriverStoreCount = getCopiedToDriverStoreCount dpInstExitCode
            CouldNotBeInstalled = getCouldNotBeInstalled dpInstExitCode
            RebootNeeded=getRebootNeeded dpInstExitCode
            ExitCode=
                if(getCouldNotBeInstalled dpInstExitCode) then
                    1u
                else if (getRebootNeeded dpInstExitCode) then
                    3010u
                else
                    0u        
        }



