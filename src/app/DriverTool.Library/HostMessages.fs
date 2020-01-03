namespace DriverTool.Library

module Messages =
        
    open DriverTool.Library.Logging

    type QuitHostMessage() = class end

    [<CLIMutable>]
    type LenovoSccmPackageInfoRequestMessage = {SourceUri:string}
    
    type HostMessage =
        |Information of string
        |Quit of QuitHostMessage
        |LenovoSccmPackageInfoRequest of LenovoSccmPackageInfoRequestMessage

    let toHostMessage message =
        match (box message) with
        | :? System.String as s ->
            HostMessage.Information s
        | :? QuitHostMessage as m ->                    
            HostMessage.Quit m
        | :? LenovoSccmPackageInfoRequestMessage as m ->                    
            HostMessage.LenovoSccmPackageInfoRequest m
        | _ -> failwith (sprintf "Unknown host request message: %s" (valueToString message))

    type ClientHostMessage =
        |Information of string
        |Quit of QuitHostMessage

    let toClientHostMessage message =
        match (box message) with
        | :? System.String as s ->
            ClientHostMessage.Information s
        | :? QuitHostMessage as m ->                    
            ClientHostMessage.Quit m        
        | _ -> failwith (sprintf "Unknown host request message: %s" (valueToString message))
    
    open DriverTool.Library.PackageXml

    type DownloadMessage =
        |DownloadPackage of PackageInfo
        |DownloadSccmPackage of SccmPackageInfo

    let toDownloadMessage message =
        match(box message) with
        | :? PackageInfo as p -> DownloadMessage.DownloadPackage p
        | :? SccmPackageInfo as sp -> DownloadMessage.DownloadSccmPackage sp
        | _ -> failwith (sprintf "Unknown download message: %s" (valueToString message))

    type DownloadedMessage = 
        |DownloadedPackage of DownloadedPackageInfo
        |DownloadedSccmPackage of DownloadedSccmPackageInfo

    let toDownloadedMessage message =
        match(box message) with
        | :? DownloadedPackageInfo as dp -> DownloadedMessage.DownloadedPackage dp
        | :? DownloadedSccmPackageInfo as dsp -> DownloadedMessage.DownloadedSccmPackage dsp
        | _ -> failwith (sprintf "Unknown downloaded message: %s" (valueToString message))