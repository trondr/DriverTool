namespace DriverTool.Library

module HostMessages =
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
    
    

    