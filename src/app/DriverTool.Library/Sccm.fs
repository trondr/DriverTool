namespace DriverTool.Library

module Sccm =
    open System
    open DriverTool.Library.PowerShellHelper    

    ///Get assigned SCCM site
    let getAssignedSite () =
        let script = 
                [|
                    "$SCCMSiteCode = New-Object -ComObject \"Microsoft.SMS.Client\""
                    "$SCCMSiteCode.GetAssignedSite()"
                |]|>linesToText
        getFirstValue script
    
    let getSiteServer () =
        let script = 
            [|
                "$SCCMSiteCode = New-Object -ComObject \"Microsoft.SMS.Client\""
                "$SCCMSiteCode.GetCurrentManagementPoint()"
            |]|>linesToText
        getFirstValue script

    ///Load ConfigurationManager module
    let loadCmPowerShellModule () = 
        raise (new NotImplementedException())

    ///Create SCCM package from package definition sms file
    let createPackageFromDefinition packageDefinitionSms =
        raise (new NotImplementedException())

    ///Create custom task sequence and add CM driver packages
    let createCustomTaskSequence name cmDriverPackages =
        raise (new NotImplementedException())