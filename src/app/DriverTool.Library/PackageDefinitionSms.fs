namespace DriverTool.Library

open DriverTool.Library

module PackageDefinitionSms =
    open DriverTool.Library
    open DriverTool.Library.String32
    open DriverTool.Library.String50
    open DriverTool.Library.String127

    type SmsProgramMode = Minimized|Maximized|Hidden
        
    let smsProgramModeToString smsProgramMode =
        match smsProgramMode with
        |Minimized -> "Minimized"
        |Maximized -> "Maximized"
        |Hidden -> "Hidden"


    type SmsAfterRunningAction = SMSRestart| ProgramRestart| SMSLogoff

    let smsAfterRunningActionToString smsAfterRunningAction = 
        match smsAfterRunningAction with
        |SMSRestart -> "SMSRestart"
        |ProgramRestart -> "ProgramRestart"
        |SMSLogoff -> "SMSLogoff"

    type SmsEstimatedDiskSpace = Unknown | SizeInMb of uint

    let smsEstimatedDiskSpaceToString smsEstimatedDiskSpace =
        match smsEstimatedDiskSpace with
        |Unknown -> "Unknown"
        |SizeInMb s -> sprintf "%dMB" s

    type SmsEstimatedRuntime = Unknown | TimeInMinutes of uint

    let smsEstimatedRuntimeToString smsEstimatedRuntime =
        match smsEstimatedRuntime with
        |Unknown -> "Unknown"
        |TimeInMinutes m -> sprintf "%d" m

    type SmsCanRunWhen = UserLoggedOn | NoUserLoggedOn | AnyUserStatus

    let smsCanRunWhenToString smsCanRunWhen = 
        match smsCanRunWhen with
        |UserLoggedOn -> "UserLoggedOn"
        |NoUserLoggedOn -> "NoUserLoggedOn"
        |AnyUserStatus -> "AnyUserStatus"

    type SmsAssignment = FirstUser | EveryUser

    let smsAssignmentToString smsAssignment = 
        match smsAssignment with
        |FirstUser -> "FirstUser"
        |EveryUser -> "EvryUser"

    type SmsProgram =
        {
            Name:String50
            Icon:string option
            Comment:String127 option
            Commandline:String127
            StartIn:String127
            Run:SmsProgramMode
            AfterRunning:SmsAfterRunningAction option
            EstimatedDiskSpace:SmsEstimatedDiskSpace
            EstimatedRunTime:SmsEstimatedRuntime
            AdditionalProgramRequirements: String127 option
            CanRunWhen:SmsCanRunWhen
            UserInputRequired:bool
            AdminRightsRequired:bool
            UseInstallAccount:bool
            DriveLetterConnection:bool
            SpecifyDrive:string option
            ReconnectDriveAtLogon:bool
            DependentProgram:string
            Assignment:SmsAssignment
            Disabled:bool
        }

    let createSmsProgram name commandLine startIn canRunWhen adminRightsRequired useInstallAccount programMode comment =
        result{
            let! name50 = DriverTool.Library.String50.create name
            let! comment127 = DriverTool.Library.String127.create comment
            let! commandLine127 = DriverTool.Library.String127.create commandLine
            let! startIn127 = DriverTool.Library.String127.create startIn

            let smsProgram =
                {
                    Name=name50
                    Icon=None
                    Comment=Some comment127
                    Commandline=commandLine127
                    StartIn=startIn127
                    Run=programMode
                    AfterRunning=None
                    EstimatedDiskSpace=SmsEstimatedDiskSpace.Unknown
                    EstimatedRunTime = SmsEstimatedRuntime.Unknown
                    AdditionalProgramRequirements=None
                    CanRunWhen=canRunWhen
                    UserInputRequired=false
                    AdminRightsRequired=adminRightsRequired
                    UseInstallAccount=useInstallAccount
                    DriveLetterConnection=false
                    SpecifyDrive=None
                    ReconnectDriveAtLogon=false
                    DependentProgram=System.String.Empty
                    Assignment=SmsAssignment.FirstUser
                    Disabled=false           
                }
            return smsProgram
        }

    let validatePrograms (smsPrograms:SmsProgram[]) =
        match smsPrograms.Length with
        |0 -> Result.Error (toException "No programs defined in package definition sms." None)
        |_ -> Result.Ok smsPrograms
        

    type  SmsPackageDefinition =
        {
            Name:String50
            Version:String32 option
            Icon:string option
            Publisher:String32
            Language:String32
            Comment:String127 option
            ContainsNoFiles:bool
            Programs:SmsProgram[]
        }


    let createSmsPackageDefinition name version icon publisher language comment programs =
        result{
            let! name50 = DriverTool.Library.String50.create name
            let! version32 = DriverTool.Library.String32.create version
            let! publisher32 = DriverTool.Library.String32.create publisher
            let! language32 = DriverTool.Library.String32.create language
            let! comment127 = DriverTool.Library.String127.create comment
            let! smsPrograms = validatePrograms programs

            let smsPackageDefinition =
                {
                    Name = name50
                    Version=Some version32
                    Icon=icon
                    Publisher=publisher32
                    Language=language32
                    Comment=Some comment127
                    ContainsNoFiles=false
                    Programs=smsPrograms
                }       
            return smsPackageDefinition
        }

    open IniParser
    open IniParser.Model
    
    let smsBoolToString value =
        match value with
        |true -> "True"
        |false -> "False"

    let smsBoolFromString (value:string) =
        match (value.ToLowerInvariant()) with
        |"true" -> true
        |"false" -> false
        |_ ->             
            raise (toException ("Invalid boolean value: " + value) None)

    let toIniString (smsPackageDefinition:SmsPackageDefinition) =
        seq{
            yield "[PDF]"
            yield "Version=2.0"
            yield ""
            yield "[Package Definition]"
            yield sprintf "Name=%s" (WrappedString.value smsPackageDefinition.Name)
            match smsPackageDefinition.Version with
            |Some version ->
                yield sprintf "Version=%s" (WrappedString.value version)
            |None -> ()

            match smsPackageDefinition.Icon with
            |Some icon ->
                yield sprintf "Icon=%s" icon
            |None -> ()

            yield sprintf "Publisher=%s" (WrappedString.value smsPackageDefinition.Publisher)
            yield sprintf "Language=%s" (WrappedString.value smsPackageDefinition.Language)

            match smsPackageDefinition.ContainsNoFiles with
            |false  -> yield "ContainsNoFiles=False"
            |true   -> yield "ContainsNoFiles=True"

            match smsPackageDefinition.Comment with
            |Some comment ->
                yield sprintf "Comment=%s" (WrappedString.value comment)
            |None -> ()
            let programNames = smsPackageDefinition.Programs |> Array.map(fun p -> WrappedString.value p.Name) |> String.concat ","
            yield sprintf "Programs=%s" programNames

            for program in smsPackageDefinition.Programs do
                yield ""
                yield sprintf "[%s]" (WrappedString.value program.Name)
                yield sprintf "Name=%s" (WrappedString.value program.Name)
                yield sprintf "Commandline=%s" (WrappedString.value program.Commandline)
                yield sprintf "StartIn=%s" (WrappedString.value program.StartIn)
                yield sprintf "Assignment=%s" (smsAssignmentToString program.Assignment)
                yield sprintf "CanRunWhen=%s" (smsCanRunWhenToString program.CanRunWhen)
                yield sprintf "Run=%s" (smsProgramModeToString program.Run)
                yield sprintf "AdminRightsRequired=%s" (smsBoolToString program.AdminRightsRequired)
                yield sprintf "UserInputRequired=%s" (smsBoolToString program.UserInputRequired)
                yield sprintf "UseInstallAccount=%s" (smsBoolToString program.UseInstallAccount)

                match program.SpecifyDrive with
                |Some driverLetter -> yield sprintf "SpecifyDrive=%s" driverLetter
                |None -> ()
                yield sprintf "DriveLetterConnection=%s" (smsBoolToString program.DriveLetterConnection)
                yield sprintf "ReconnectDriveAtLogon=%s" (smsBoolToString program.ReconnectDriveAtLogon)
                yield sprintf "DependentProgram=%s" program.DependentProgram
                yield sprintf "Disabled=%s" (smsBoolToString program.Disabled)

                match program.AfterRunning with
                |Some ar -> yield sprintf "AfterRunning=%s" (smsAfterRunningActionToString ar)
                |None -> ()

                yield sprintf "EstimatedDiskSpace=%s" (smsEstimatedDiskSpaceToString program.EstimatedDiskSpace)
                yield sprintf "EstimatedRunTime=%s" (smsEstimatedRuntimeToString program.EstimatedRunTime)

                match program.AdditionalProgramRequirements with
                |Some additionalProgramRequirements -> yield sprintf "AdditionalProgramRequirements=%s" (WrappedString.value additionalProgramRequirements)
                |None -> ()

                match program.Icon with
                |Some icon -> yield sprintf "Icon=%s" icon
                |None -> ()

                match program.Comment with
                |Some comment -> yield sprintf "Comment=%s" (WrappedString.value comment)
                |None -> ()                
                    
        } |> Seq.toArray |> linesToText
            
    ///Write package definition to file
    let writeToFile logger filePath smsPackageDefinition =
        smsPackageDefinition
        |>toIniString
        |>FileOperations.writeContentToFile logger filePath        