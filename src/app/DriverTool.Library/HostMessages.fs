namespace DriverTool.Library

module HostMessages =
    open DriverTool.Library.Logging

    type QuitHostMessage() = class end
    type QuitConfirmedHostMessage() = class end
    type StartConfirmedHostMessage() = class end


    [<CLIMutable>]
    type LenovoSccmPackageInfoRequestMessage = {SourceUri:string}
    
    type HostMessage =
        |Information of string
        |Quit of QuitHostMessage        
        |LenovoSccmPackageInfoRequest of LenovoSccmPackageInfoRequestMessage

    type ClientHostMessage =
        |Information of string
        |Quit of QuitHostMessage
        |QuitConfirmed of QuitConfirmedHostMessage
        |StartConfirmed of StartConfirmedHostMessage

    let toHostMessage message =
        match (box message) with
        | :? System.String as s ->
            HostMessage.Information s
        | :? QuitHostMessage as m ->                    
            HostMessage.Quit m        
        | :? LenovoSccmPackageInfoRequestMessage as m ->                    
            HostMessage.LenovoSccmPackageInfoRequest m
        | _ -> failwith (sprintf "Unknown host request message: %s" (valueToString message))

    let toClientHostMessage message =
        let boxedMessage = box message
        match (boxedMessage) with
        | :? System.String as s ->
            ClientHostMessage.Information s
        | :? QuitHostMessage as m ->                    
            ClientHostMessage.Quit m        
        | :? QuitConfirmedHostMessage as m ->                    
            ClientHostMessage.QuitConfirmed m
        | :? StartConfirmedHostMessage as m ->                    
            ClientHostMessage.StartConfirmed m
        | _ -> failwith (sprintf "Unknown host request message: %s" (valueToString message))
    
    

    