namespace DriverTool.DupExitCode2ExitCode

module Dup =
    open System.ComponentModel
    open DriverTool

    let win32ErrorCode2Message (errorCode:int) =
        (new Win32Exception(errorCode)).Message
    
    let dupExitCode2Message dupExitCode =
        match dupExitCode with
        |0 -> "The update was successful and a reboot is unnecessary." //Success
        |1 -> "The update failed because an error occurred during operation. The administrator should refer to the log files to understand why the update failed and take appropriate action if necessary to fix the problem." // Fatal error
        |2 -> "The update was successful and a reboot is required to make the update active. This code is returned only if administrators choose not to provide the /r switch." // Reboot required
        |3 -> "The update was not successful because the target system already had the updated version or a lower version of the BIOS, driver, or firmware provided by the update package. To avoid receiving this error, administrators can provide the /f option." // Another version of this product is already installed. 
        |4 -> "The update was unsuccessful because the server did not meet BIOS, driver, or firmware prerequisites for the update to be applied, or because no supported device was found on the target system. The Dell Update Package enforces this check and blocks an update from   being applied if the prerequisite is not met, preventing the server from reaching an invalid configuration state. The prerequisite can be met by applying another Dell Update Package, if available. In this case, the other package should be applied before the current one so that both updates can succeed. A DEP_HARD_ERROR cannot be suppressed by using the /f switch." // Install rejected 
        |5 -> "The package is not applicable to the system. This error can occur if the package does not support the system, or if the device it is trying to update is not present in the system. This exit code cannot be suppressed by using the /f switch." // Platform unsupported
        |6 -> "The package requires a reboot after the update is applied if the /r option has been provided. The system reboots soon after the package is returned." //Reboot in progress
        |_ -> "Unknown error." //Uknown error

    let dupExitCode2ExitCode dupExitCode =
        match dupExitCode with
        |0 -> 0
        |1 -> 1603 // Fatal error
        |2 -> 3010 // Reboot required
        |3 -> 0 // Another version of this product is already installed. 
        |4 -> 0 // Install rejected 
        |5 -> 0 // Platform unsupported
        |6 -> 1641 // Reboot in progress
        |_ -> 1 //Uknown error
    