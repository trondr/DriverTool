namespace DriverTool.Library

module Sccm =
    open System
    open System.Collections.Generic
    open DriverTool.Library.PowerShellHelper 

    ///Get assigned SCCM site
    let getAssignedSite () =
        let script = 
                [|
                    "$SCCMSiteCode = New-Object -ComObject \"Microsoft.SMS.Client\""
                    "$SCCMSiteCode.GetAssignedSite()"
                |]|>linesToText
        getFirstStringValue script
    
    let getSiteServer () =
        let script = 
            [|
                "$SCCMSiteCode = New-Object -ComObject \"Microsoft.SMS.Client\""
                "$SCCMSiteCode.GetCurrentManagementPoint()"
            |]|>linesToText
        getFirstStringValue script

    ///Load ConfigurationManager module
    let loadCmPowerShellModule () = 
        raise (new NotImplementedException())

    ///Load ConfigurationManager module and run CM powershell script
    let runCmPowerShellScript script siteCode siteServer =
        result{
            let cmScript = 
                seq{             
                    yield sprintf "$SiteCode=\"%s\"" siteCode
                    yield sprintf "$SiteServer=\"%s\"" siteServer
                    yield "$initParams = @{}"
                    if(logger.IsDebugEnabled) then
                        yield "$initParams.Add(\"Verbose\", $true)"
                        yield "$initParams.Add(\"ErrorAction\", \"Stop\")"
                    yield "if((Get-Module ConfigurationManager) -eq $null) {"
                    yield "    Import-Module \"$($ENV:SMS_ADMIN_UI_PATH)\..\ConfigurationManager.psd1\" @initParams"
                    yield "}"
                    yield "if((Get-PSDrive -Name $SiteCode -PSProvider CMSite -ErrorAction SilentlyContinue) -eq $null) {"
                    yield "New-PSDrive -Name $SiteCode -PSProvider CMSite -Root $SiteServer @initParams"
                    yield "}"
                    yield "Set-Location \"$($SiteCode):\\\" @initParams"
                    yield script
                }
                |>Seq.toArray
                |> linesToText
            let variables = new Dictionary<string, obj>()
            let! out = PowerShellHelper.runPowerShellScript cmScript variables
            return out        
        }

    ///Create SCCM package from package definition sms file
    let createPackageFromDefinition packageDefinitionSms sourceFolderPath =
        result{
            let script = 
                [|
                    sprintf "New-CMPackage -FromDefinition -PackageDefinitionName \"%s\" -SourceFileType AlwaysObtainSourceFile -SourceFolderPath \"%s\" -SourceFolderPathType UncNetworkPath" packageDefinitionSms sourceFolderPath
                |]|>linesToText
            let! siteCode = getAssignedSite()
            let! siteServer = getSiteServer()
            let! out = runCmPowerShellScript script siteCode siteServer
            return out        
        }
        

    ///Create custom task sequence and add CM driver packages
    let createCustomTaskSequence name cmDriverPackages =
        raise (new NotImplementedException())