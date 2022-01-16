namespace DriverTool.Library

open DriverTool.Library

module PackageDefinitionSms =
    open System
    open System.Text
    open DriverTool.Library.Logging
    open DriverTool.Library.String32
    open DriverTool.Library.String50
    open DriverTool.Library.String64
    open DriverTool.Library.String100
    open DriverTool.Library.String127
    open DriverTool.Library.String255
    open DriverTool.Library.String512
    open DriverTool.Library.DriverPack

    let logger = Logger.Logger()

    type SmsProgramMode = Minimized|Maximized|Hidden|Normal
        
    let smsProgramModeToString smsProgramMode =
        match smsProgramMode with
        |Minimized -> "Minimized"
        |Maximized -> "Maximized"
        |Hidden -> "Hidden"
        |Normal -> "Normal"

    let smsProgramModeFromString smsProgramMode =
        let value = F.toOptionalString smsProgramMode        
        match value with
        |Some pm ->         
            match pm with
            |"Minimized" -> Some SmsProgramMode.Minimized
            |"Maximized" -> Some SmsProgramMode.Maximized
            |"Hidden" -> Some SmsProgramMode.Hidden
            |"Normal" -> Some SmsProgramMode.Normal
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

    let createSmsProgram name commandLine startIn canRunWhen adminRightsRequired useInstallAccount userInputRequired programMode comment =
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
                    UserInputRequired=userInputRequired
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
            ManufacturerWmiQuery:DriverPack.WmiQuery
            ModelWmiQuery:DriverPack.WmiQuery
            SourcePath:string option
        }


    let createSmsPackageDefinition name version icon publisher language containsNoFiles comment programs manufacturerWmiQuery modelWmiQuery=
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
                    ManufacturerWmiQuery=manufacturerWmiQuery
                    ModelWmiQuery=modelWmiQuery
                    SourcePath=None
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
        match value with
        |null -> defaultValue
        |_ ->
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
                yield sprintf "CommandLine=%s" (WrappedString.value program.Commandline)
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
            
            yield ""
            yield "[ManufacturerWmiQuery]"
            yield (sprintf "Name=%s" smsPackageDefinition.ManufacturerWmiQuery.Name)
            yield (sprintf "NameSpace=%s" smsPackageDefinition.ManufacturerWmiQuery.NameSpace)
            yield (sprintf "Query=%s" smsPackageDefinition.ManufacturerWmiQuery.Query)

            yield ""
            yield "[ModelWmiQuery]"
            yield (sprintf "Name=%s" smsPackageDefinition.ModelWmiQuery.Name)
            yield (sprintf "NameSpace=%s" smsPackageDefinition.ModelWmiQuery.NameSpace)
            yield (sprintf "Query=%s" smsPackageDefinition.ModelWmiQuery.Query)
            
        } |> Seq.toArray |>String.concat Environment.NewLine
            
    open IniParser

    let readIniValue' (iniData:IniData) sectionName valueName defaultValue =
        let section = iniData.Sections.[sectionName]
        match section with
        |null -> 
            logger.Warn(sprintf "Section '[%s]' does not exist." sectionName)
            defaultValue
        |_ ->
            let value = section.[valueName]
            match value with
            |null -> 
                logger.Warn(sprintf "Section value '[%s]%s' does not exist." sectionName valueName)
                defaultValue
            |v -> v

    let readIniValue iniData sectionName valueName defaultValue =
        tryCatch4 (Some (sprintf "Failed to read ini value: [%s]%s" sectionName valueName)) readIniValue' iniData sectionName valueName defaultValue

    let toSmsProgram (iniData:IniData) programSectionName =
        result{            
            let! name = readIniValue iniData programSectionName "Name" String.Empty
            let! name100 = String100.create name
            let! commandLine = readIniValue iniData programSectionName "CommandLine" String.Empty
            let! commandLine255 = String255.create commandLine
            let! comment = readIniValue iniData programSectionName "Comment" String.Empty
            let! comment255 = String255.create comment
            let! startIn = readIniValue iniData programSectionName "StartIn" String.Empty
            let! startIn255 = String255.create startIn
            let! icon = readIniValue iniData programSectionName "Icon" String.Empty
            let! additionalProgramRequirements = readIniValue iniData programSectionName "AdditionalProgramRequirements" String.Empty
            let! additionalProgramRequirements127 = String127.create additionalProgramRequirements
            let! run = readIniValue iniData programSectionName "Run" String.Empty
            let! afterRunning = readIniValue iniData programSectionName "AfterRunning" String.Empty
            let! estimatedDiskSpace = readIniValue iniData programSectionName "EstimatedDiskSpace" String.Empty
            let! estimatedRunTime = readIniValue iniData programSectionName "EstimatedRunTime" String.Empty
            let! canRunWhen = readIniValue iniData programSectionName "CanRunWhen" String.Empty
            let! userInputRequired = readIniValue iniData programSectionName "UserInputRequired" String.Empty
            let! adminRightsRequired = readIniValue iniData programSectionName "AdminRightsRequired" String.Empty
            let! useInstallAccount = readIniValue iniData programSectionName "UseInstallAccount" String.Empty
            let! driveLetterConnection = readIniValue iniData programSectionName "DriveLetterConnection" String.Empty
            let! specifyDrive = readIniValue iniData programSectionName "SpecifyDrive" String.Empty
            let! reconnectDriveAtLogon = readIniValue iniData programSectionName "ReconnectDriveAtLogon" String.Empty
            let! dependentProgram = readIniValue iniData programSectionName "DependentProgram" String.Empty
            let! assignment = readIniValue iniData programSectionName "Assignment" String.Empty
            let! disabled = readIniValue iniData programSectionName "Disabled" String.Empty
            let smsProgram =
              {
                Name=name100
                Icon=F.toOptionalString icon
                Comment=String255.toOptionalString comment255
                Commandline=commandLine255
                StartIn=startIn255
                Run=smsProgramModeFromString run
                AfterRunning=smsAfterRunningActionFromString afterRunning
                EstimatedDiskSpace=smsEstimatedDiskSpaceFromString estimatedDiskSpace
                EstimatedRunTime = smsEstimatedRuntimeFromString estimatedRunTime
                AdditionalProgramRequirements=(String127.toOptionalString additionalProgramRequirements127)
                CanRunWhen=smsCanRunWhenFromString canRunWhen
                UserInputRequired=smsBoolFromString userInputRequired false
                AdminRightsRequired=smsBoolFromString adminRightsRequired false
                UseInstallAccount=smsBoolFromString useInstallAccount false
                DriveLetterConnection=smsBoolFromString driveLetterConnection false
                SpecifyDrive=F.toOptionalString specifyDrive
                ReconnectDriveAtLogon=smsBoolFromString reconnectDriveAtLogon false
                DependentProgram=dependentProgram
                Assignment=smsAssignmentFromString assignment
                Disabled=smsBoolFromString disabled false
                }
            return smsProgram
        }
    open DriverTool.Library.PackageXml

    ///Parse package definition ini, throw exception in case of invalid data.
    let fromIniStringUnsafe (packageDefinitonIniString:string) : SmsPackageDefinition =        
        let iniDataParser = new Parser.IniDataParser()
        let iniData = iniDataParser.Parse(packageDefinitonIniString)
        let packageDefinitionSectionName = "Package Definition"        
        match(result{        
            let! programSectionNames' = readIniValue iniData packageDefinitionSectionName "Programs" String.Empty
            let programSectionNames = programSectionNames'.Split([|','|])|>Array.map(fun s -> s.Trim())
            
            let! smsPrograms =
                programSectionNames
                |>Array.map(fun psn ->                     
                    let smsProgram = toSmsProgram iniData psn
                    smsProgram
                )|>toAccumulatedResult
            
            let! name = readIniValue iniData packageDefinitionSectionName "Name" String.Empty
            let! version = readIniValue iniData packageDefinitionSectionName "Version" String.Empty
            let! icon = readIniValue iniData packageDefinitionSectionName "Icon" String.Empty
            let! publisher = readIniValue iniData packageDefinitionSectionName "Publisher" String.Empty
            let! language = readIniValue iniData packageDefinitionSectionName "Language" String.Empty
            let! containsNoFiles' = readIniValue iniData packageDefinitionSectionName "ContainsNoFiles" String.Empty
            let containsNoFiles = smsBoolFromString containsNoFiles' false
            let! comment = readIniValue iniData packageDefinitionSectionName "Comment" String.Empty
            
            let manufacturerWmiQuerySectionName = "ManufacturerWmiQuery"
            let! manufacturerWmiQueryName = readIniValue iniData manufacturerWmiQuerySectionName "Name" String.Empty
            let! manufacturerWmiQueryNameSpace = readIniValue iniData manufacturerWmiQuerySectionName "NameSpace" String.Empty
            let! manufacturerWmiQueryQuery = readIniValue iniData manufacturerWmiQuerySectionName "Query" String.Empty
            let manufacturerWmiQuery =
                {
                    Name = manufacturerWmiQueryName
                    NameSpace = manufacturerWmiQueryNameSpace
                    Query = manufacturerWmiQueryQuery
                }

            let manufacturerWmiQuerySectionName = "ModelWmiQuery"
            let! modelWmiQueryName = readIniValue iniData manufacturerWmiQuerySectionName "Name" String.Empty
            let! modelWmiQueryNameSpace = readIniValue iniData manufacturerWmiQuerySectionName "NameSpace" String.Empty
            let! modelWmiQueryQuery = readIniValue iniData manufacturerWmiQuerySectionName "Query" String.Empty

            let modelWmiQuery =
                {
                    Name = modelWmiQueryName
                    NameSpace = modelWmiQueryNameSpace
                    Query = modelWmiQueryQuery
                }
            let! smsPackageDefinition = createSmsPackageDefinition name version (F.toOptionalString icon) publisher language containsNoFiles comment (smsPrograms|>Seq.toArray) manufacturerWmiQuery modelWmiQuery
            return smsPackageDefinition
        })with
        |Result.Ok pd -> pd
        |Result.Error ex -> raise ex

    ///Parse package definition ini, return package definition result
    let fromIniString (packageDefinitonIniString:string) =
        let message = sprintf "Failed to parse ini data: %s%s" Environment.NewLine packageDefinitonIniString
        tryCatch (Some message) fromIniStringUnsafe packageDefinitonIniString

    ///Write package definition to file
    let writeToFile logger filePath smsPackageDefinition =
        smsPackageDefinition
        |>toIniString
        |>FileOperations.writeContentToFile logger filePath        

    ///Read package definition from file
    let readFromFile filePath =
        result{
            let! uncFilePath = PathOperations.toUncPath true filePath
            let uncFilePathValue = FileSystem.pathValue uncFilePath
            let! iniData = FileOperations.readContentFromFile uncFilePath
            let! packageDefinition = fromIniString iniData
            return {packageDefinition with SourcePath=(Some uncFilePathValue)}
        }

    ///Read package definition from file, throws exception on error.
    let readFromFileUnsafe file =                  
        let filePath = DriverTool.Library.FileSystem.pathUnSafe file        
        resultToValueUnsafe (readFromFile filePath)
