namespace DriverTool.Library

module HostMessages =
    open DriverTool.Library.Logging

    type QuitHostMessage() = class end
    type QuitConfirmedHostMessage() = class end
    type StartConfirmedHostMessage() = class end
    type ConfirmStartHostMessage() = class end


    [<CLIMutable>]
    type LenovoSccmPackageInfoRequestMessage = {SourceUri:string}
    
    type HostMessage =
        |Information of string
        |Quit of QuitHostMessage        
        |ConfirmStart of ConfirmStartHostMessage
        |LenovoSccmPackageInfoRequest of LenovoSccmPackageInfoRequestMessage        

    type ClientHostMessage =
        |Information of string
        |Quit of QuitHostMessage
        |QuitConfirmed of QuitConfirmedHostMessage
        |ConfirmStart of ConfirmStartHostMessage
        |StartConfirmed of StartConfirmedHostMessage

    let toHostMessage message =
        logger.Debug(sprintf "toHostMessage:%A" message)
        match (box message) with
        | :? System.String as s ->
            HostMessage.Information s
        | :? QuitHostMessage as m ->                    
            HostMessage.Quit m        
        | :? LenovoSccmPackageInfoRequestMessage as m ->                    
            HostMessage.LenovoSccmPackageInfoRequest m
        | :? ConfirmStartHostMessage as m ->
            HostMessage.ConfirmStart m        
        | _ -> failwith (sprintf "Unknown host request message: %s" (valueToString message))

    let toClientHostMessage message =
        logger.Debug(sprintf "toClientHostMessage:%A" message)
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
        | :? ConfirmStartHostMessage as m ->                    
            ClientHostMessage.ConfirmStart m   
        | _ -> failwith (sprintf "Unknown host request message: %s" (valueToString message))
    
    

    