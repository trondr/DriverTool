namespace DriverTool.Library

open DriverTool.Library

module PackageDefinitionSms =
    open System
    open System.Text
    open DriverTool.Library
    open DriverTool.Library.String32
    open DriverTool.Library.String50
    open DriverTool.Library.String64
    open DriverTool.Library.String100
    open DriverTool.Library.String127
    open DriverTool.Library.String255
    open DriverTool.Library.String512


    type SmsProgramMode = Minimized|Maximized|Hidden
        
    let smsProgramModeToString smsProgramMode =
        match smsProgramMode with
        |Minimized -> "Minimized"
        |Maximized -> "Maximized"
        |Hidden -> "Hidden"

    let smsProgramModeFromString smsProgramMode =
        let value = F.toOptionalString smsProgramMode        
        match value with
        |Some pm ->         
            match pm with
            |"Minimized" -> Some SmsProgramMode.Minimized
            |"Maximized" -> Some SmsProgramMode.Maximized
            |"Hidden" -> Some SmsProgramMode.Hidden
            |_ -> raise (toException ("Invalid ProgramMode: " + pm) None)
        |None -> None

    type SmsAfterRunningAction = SMSRestart| ProgramRestart| SMSLogoff

    let smsAfterRunningActionToString smsAfterRunningAction = 
        match smsAfterRunningAction with
        |SMSRestart -> "SMSRestart"
        |ProgramRestart -> "ProgramRestart"
        |SMSLogoff -> "SMSLogoff"

    let smsAfterRunningActionFromString smsAfterRunningAction = 
        let value = F.toOptionalString smsAfterRunningAction
        match value with
        |Some v ->
            match v with
            |"SMSRestart" -> Some SmsAfterRunningAction.SMSRestart        
            |"ProgramRestart" -> Some SmsAfterRunningAction.ProgramRestart
            |"SMSLogoff" -> Some SmsAfterRunningAction.SMSLogoff
            |_ -> raise (toException ("Invalid AfterRunningAction: " + v) None)
        |None -> None


    type SmsEstimatedDiskSpace = Unknown | SizeInMb of uint

    let smsEstimatedDiskSpaceToString smsEstimatedDiskSpace =
        match smsEstimatedDiskSpace with
        |Unknown -> "Unknown"
        |SizeInMb s -> sprintf "%dMB" s

    let smsEstimatedDiskSpaceFromString smsEstimatedDiskSpace =
        let value = F.toOptionalString smsEstimatedDiskSpace
        match value with
        |Some v ->
            match v with
            |"Uknown" -> Some SmsEstimatedDiskSpace.Unknown
            |Regex @"(\d+)MB" [size] -> Some (SmsEstimatedDiskSpace.SizeInMb (size|>uint))
            |_ -> raise (toException ("Invalid EstimatedDiskSpace: " + v) None)
        |None -> None

    type SmsEstimatedRuntime = Unknown | TimeInMinutes of uint

    let smsEstimatedRuntimeToString smsEstimatedRuntime =
        match smsEstimatedRuntime with
        |Unknown -> "Unknown"
        |TimeInMinutes m -> sprintf "%d" m

    let smsEstimatedRuntimeFromString smsEstimatedRuntime =
        let value = F.toOptionalString smsEstimatedRuntime
        match value with
        |Some v ->
            match v with
            |"Unknown" -> Some SmsEstimatedRuntime.Unknown
            |Regex @"(\d+)" [minutes] -> Some (SmsEstimatedRuntime.TimeInMinutes (minutes|>uint))
            |_ -> raise (toException ("Invalid EstimatedRuntime: " + v) None)
        |None -> None

    type SmsCanRunWhen = UserLoggedOn | NoUserLoggedOn | AnyUserStatus

    let smsCanRunWhenToString smsCanRunWhen = 
        match smsCanRunWhen with
        |UserLoggedOn -> "UserLoggedOn"
        |NoUserLoggedOn -> "NoUserLoggedOn"
        |AnyUserStatus -> "AnyUserStatus"

    let smsCanRunWhenFromString smsCanRunWhen = 
        let value = F.toOptionalString smsCanRunWhen
        match value with
        |Some v ->
            match v with        
            |"UserLoggedOn" -> SmsCanRunWhen.UserLoggedOn
            |"NoUserLoggedOn" -> SmsCanRunWhen.NoUserLoggedOn
            |"AnyUserStatus" -> SmsCanRunWhen.AnyUserStatus
            |_ -> raise (toException ("Invalid CanRunWhen: " + v) None)
        |None -> SmsCanRunWhen.UserLoggedOn

    type SmsAssignment = FirstUser | EveryUser

    let smsAssignmentToString smsAssignment = 
        match smsAssignment with
        |FirstUser -> "FirstUser"
        |EveryUser -> "EvryUser"

    let smsAssignmentFromString smsAssignment = 
        let value = F.toOptionalString smsAssignment
        match value with
        |Some v -> 
            match v with
            |"FirstUser" -> SmsAssignment.FirstUser
            |"EveryUser" -> SmsAssignment.EveryUser
            |_ -> raise (toException ("Invalid Assignment: " + v) None)
        |None -> SmsAssignment.FirstUser

    type SmsProgram =
        {
            Name:String100
            Icon:string option
            Comment:String255 option
            Commandline:String255
            StartIn:String255
            Run:SmsProgramMode option
            AfterRunning:SmsAfterRunningAction option
            EstimatedDiskSpace:SmsEstimatedDiskSpace option
            EstimatedRunTime:SmsEstimatedRuntime option
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
            let! name100 = DriverTool.Library.String100.create name
            let! comment255 = DriverTool.Library.String255.create comment
            let! commandLine255 = DriverTool.Library.String255.create commandLine
            let! startIn255 = DriverTool.Library.String255.create startIn

            let smsProgram =
                {
                    Name=name100
                    Icon=None
                    Comment=Some comment255
                    Commandline=commandLine255
                    StartIn=startIn255
                    Run=programMode
                    AfterRunning=None
                    EstimatedDiskSpace=None
                    EstimatedRunTime = None
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
            Name:String255
            Version:String64 option
            Icon:string option
            Publisher:String64
            Language:String64
            Comment:String512
            ContainsNoFiles:bool
            Programs:SmsProgram[]
        }


    let createSmsPackageDefinition name version icon publisher language containsNoFiles comment programs =
        result{
            let! name255 = DriverTool.Library.String255.create name
            let! version64 = DriverTool.Library.String64.create version
            let! publisher64 = DriverTool.Library.String64.create publisher
            let! language64 = DriverTool.Library.String64.create language
            let! comment512 = DriverTool.Library.String512.create comment
            let! smsPrograms = validatePrograms programs

            let smsPackageDefinition =
                {
                    Name = name255
                    Version=String64.toOptionalString version64
                    Icon=icon
                    Publisher=publisher64
                    Language=language64
                    Comment=comment512
                    ContainsNoFiles=containsNoFiles
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

    let smsBoolFromString (value:string) defaultValue =
        match (value.ToLowerInvariant()) with
        |"true" -> true
        |"false" -> false
        |_ ->             
            logger.Warn("Invalid boolean value: " + value)
            defaultValue            

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

            yield sprintf "Comment=%s" (WrappedString.value smsPackageDefinition.Comment)
            
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
                
                match program.Run with
                |Some pm -> 
                    yield sprintf "Run=%s" (smsProgramModeToString pm)
                |None -> ()

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

                match program.EstimatedDiskSpace with
                |Some eds ->
                    yield sprintf "EstimatedDiskSpace=%s" (smsEstimatedDiskSpaceToString eds)
                |None -> ()


                match program.EstimatedRunTime with
                |Some ert -> 
                    yield sprintf "EstimatedRunTime=%s" (smsEstimatedRuntimeToString ert)
                |None -> ()

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
            
    open IniParser

    let readSectionValue (section:KeyDataCollection) valueName defaultValue =
        let value = section.[valueName]
        match value with
        |null -> defaultValue
        |_ -> value

    let toSmsProgram (programSection:KeyDataCollection) =
        result{
            let! name100 = String100.create (programSection.["Name"])
            let! commandLine255 = String255.create (programSection.["Commandline"])
            let! comment255 = String255.create (readSectionValue programSection "Comment" String.Empty)
            let! startIn255 = String255.create (readSectionValue programSection "StartIn" String.Empty)            
            let! additionalProgramRequirements127 = String127.create (readSectionValue programSection "AdditionalProgramRequirements" String.Empty)
            let smsProgram =
              {
                Name=name100
                Icon=F.toOptionalString (programSection.["Icon"])
                Comment=String255.toOptionalString comment255
                Commandline=commandLine255
                StartIn=startIn255
                Run=smsProgramModeFromString (programSection.["Run"])
                AfterRunning=smsAfterRunningActionFromString (programSection.["AfterRunning"])
                EstimatedDiskSpace=smsEstimatedDiskSpaceFromString (programSection.["EstimatedDiskSpace"])
                EstimatedRunTime = smsEstimatedRuntimeFromString (programSection.["EstimatedRunTime"])
                AdditionalProgramRequirements=(String127.toOptionalString additionalProgramRequirements127)
                CanRunWhen=smsCanRunWhenFromString (programSection.["CanRunWhen"])
                UserInputRequired=smsBoolFromString (programSection.["UserInputRequired"]) false
                AdminRightsRequired=smsBoolFromString (programSection.["AdminRightsRequired"]) false
                UseInstallAccount=smsBoolFromString (programSection.["UseInstallAccount"]) false
                DriveLetterConnection=smsBoolFromString (programSection.["DriveLetterConnection"]) false
                SpecifyDrive=F.toOptionalString (programSection.["SpecifyDrive"])
                ReconnectDriveAtLogon=smsBoolFromString (programSection.["ReconnectDriveAtLogon"]) false
                DependentProgram=programSection.["DependentProgram"]
                Assignment=smsAssignmentFromString (programSection.["Assignment"])
                Disabled=smsBoolFromString (programSection.["Disabled"]) false
                }
            return smsProgram
        }

    let fromIniStringUnsafe (packageDefinitonIniString:string) : SmsPackageDefinition =        
        let iniDataParser = new Parser.IniDataParser()
        let iniData = iniDataParser.Parse(packageDefinitonIniString)
        let packageDefinitionSection = iniData.Sections.["Package Definition"]
        let programSectionNames = packageDefinitionSection.["Programs"].Split([|','|])|>Array.map(fun s -> s.Trim())        
        match(result
        {        
            let! smsPrograms =
                programSectionNames
                |>Array.map(fun psn -> 
                    let programSection = iniData.Sections.[psn]
                    let smsProgram = toSmsProgram programSection
                    smsProgram
                )|>toAccumulatedResult
            let name = packageDefinitionSection.["Name"]
            let version = packageDefinitionSection.["Version"]
            let icon = F.toOptionalString (packageDefinitionSection.["Icon"])
            let publisher = packageDefinitionSection.["Publisher"]
            let language = packageDefinitionSection.["Language"]
            let containsNoFiles = smsBoolFromString packageDefinitionSection.["ContainsNoFiles"] false
            let comment = packageDefinitionSection.["Comment"]        
            let! smsPackageDefinition = createSmsPackageDefinition name version icon publisher language containsNoFiles comment (smsPrograms|>Seq.toArray)
            return smsPackageDefinition
        })with
        |Result.Ok pd -> pd
        |Result.Error ex -> raise ex

    let fromIniString (packageDefinitonIniString:string) =
        let message = sprintf "Failed to parse ini data: %s%s" Environment.NewLine packageDefinitonIniString
        tryCatch (Some message) fromIniStringUnsafe packageDefinitonIniString

    ///Write package definition to file
    let writeToFile logger filePath smsPackageDefinition =
        smsPackageDefinition
        |>toIniString
        |>FileOperations.writeContentToFile logger filePath        

    let readFromFile filePath =
        result{
            let! iniData = FileOperations.readContentFromFile filePath
            let! packageDefinition = fromIniString iniData
            return packageDefinition
        }
        