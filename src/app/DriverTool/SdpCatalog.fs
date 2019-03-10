﻿namespace DriverTool

module SdpCatalog =
    
    open  System
    open DriverTool.FileSystem
    open System.Xml.Linq
    open System.Linq
    open System

    let logger = Logging.getLoggerByName("DriverTool.SdpCatalog")

    type ApplicabilityRule = string

    type ApplicabilityRules = 
        {
            IsInstallable:ApplicabilityRule option
            IsInstalled:ApplicabilityRule option
            IsSuperseded:ApplicabilityRule option
        }

    type InstallationResult = Failed|Succeeded|Cancelled
    
    let toInstallationResult installationResult =
        match installationResult with
        |"Failed" -> Result.Ok Failed
        |"Succeeded" -> Result.Ok Succeeded
        |"Cancelled" -> Result.Ok Cancelled        
        |_ -> Result.Error (new Exception(sprintf "Invalid InstallationResult '%s'. Valid values: [Failed|Succeeded|Cancelled]" installationResult))

    //#region ProductCode
    type ProductCode = private ProductCode of string

    let ifTrueThen success =
        function
        |true -> Some success
        |false -> None

    let (|NullOrEmpty|_|) =
        String.IsNullOrWhiteSpace 
        >> ifTrueThen NullOrEmpty

    let isInvalidGuid (guid:string) =
        match guid with        
        |Regex @"(\{[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}\})" [_] -> false
        | _ -> true

    let (|NotValidGuid|_|) =
        isInvalidGuid
        >> ifTrueThen NotValidGuid

    let productCode productCode =
        match productCode with
        |null -> Result.Error (new Exception("ProductCode cannot be null."))
        |NullOrEmpty -> Result.Error (new Exception("ProductCode cannot be empty."))        
        |NotValidGuid -> Result.Error (new Exception(sprintf "Invalid product code: '%s'" productCode))
        |g -> Result.Ok (ProductCode g)

    let productCodeValue (ProductCode productCode) = productCode
    //#endregion ProductCode

    type MsiInstallerData =
        {
            MsiFile:string
            CommandLine:string
            UninstallCommandLine:string option
            ProductCode:ProductCode
        }
    
    type CommandLineReturnCode={
        Code:int;
        Result:InstallationResult;
        Reboot:bool
    }

    type CommandLineInstallerData =
        {
            Program:string
            Arguments:string
            RebootByDefault:bool
            DefaultResult:InstallationResult
            ReturnCodes:CommandLineReturnCode[]
        }

    type InstallerData =
        |CommandLineInstallerData of CommandLineInstallerData
        |MsiInstallerData of MsiInstallerData
    
    type OriginFile =
        {
            Digest:string
            FileName:string
            Modified:DateTime
            OriginUri:string
            Size:Int64
        }
    
    type InstallImpact =
        |Normal
        |Minor
        |RequiresExclusiveHandling

    let toInstallImpact installImpact =
        match installImpact with
        |"Normal" -> Result.Ok Normal
        |"Minor" -> Result.Ok Minor
        |"RequiresExclusiveHandling" -> Result.Ok RequiresExclusiveHandling        
        |_ -> Result.Error (new Exception(sprintf "Invalid InstallImpact '%s'. Valid values: [Normal|Minor|RequiresExclusiveHandling]" installImpact))

    type InstallationRebootBehavior=    
        |NeverReboots
        |AlwaysRequiresReboot
        |CanRequestReboot

    let toInstallatioRebootBehaviour installationRebootBehavior =
        match installationRebootBehavior with
        |"NeverReboots" -> Result.Ok NeverReboots
        |"AlwaysRequiresReboot" -> Result.Ok AlwaysRequiresReboot
        |"CanRequestReboot" -> Result.Ok CanRequestReboot        
        |_ -> Result.Error (new Exception(sprintf "Invalid InstallImpact '%s'. Valid values: [NeverReboots|AlwaysRequiresReboot|CanRequestReboot]" installationRebootBehavior))
   
    type InstallProperties =
        {
            CanRequestUserInput:bool
            RequiresNetworkConnectivity:bool
            Impact:InstallImpact
            RebootBehavior:InstallationRebootBehavior
        }

    type InstallableItem =
        {
            Id:string
            ApplicabilityRules:ApplicabilityRules
            InstallProperties:InstallProperties
            UninstallProperties:InstallProperties option
            InstallerData:InstallerData
            //OriginFile:OriginFile
        }
   
    type MsrcSeverity=
        |Critical
        |Important
        |Moderate
        |Low

    let toMsrcSeverity msrcSeverity =
        match msrcSeverity with
        |"Critical" -> Result.Ok Critical
        |"Important" -> Result.Ok Important
        |"Moderate" -> Result.Ok Moderate
        |"Low" -> Result.Ok Low
        |_ -> Result.Error (new Exception(sprintf "Invalid MsRcSeverity '%s'. Valid values: [Critical|Important|Moderate|Low]" msrcSeverity))

    type UpdateClassification =
        |Updates
        |SecurityUpdates
        |FeaturePacks
        |UpdateRollups
        |ServicePacks
        |Hotfixes
        |Drivers

    let toUpdateClassification updateClassification =
        match updateClassification with
        |"Updates" -> Result.Ok Updates
        |"SecurityUpdates" -> Result.Ok SecurityUpdates
        |"FeaturePacks" -> Result.Ok FeaturePacks
        |"UpdateRollups" -> Result.Ok UpdateRollups
        |"ServicePacks" -> Result.Ok ServicePacks
        |"Hotfixes" -> Result.Ok Hotfixes
        |"Drivers" -> Result.Ok Drivers
        |_ -> Result.Error (new Exception(sprintf "Invalid UpdateClassification '%s'. Valid values: [Updates|SecurityUpdates|FeaturePacks|UpdateRollups|ServicePacks|Hotfixes|Drivers]" updateClassification))

    type UpdateSpecificData=
        {
            MsrcSeverity:MsrcSeverity
            UpdateClassification:UpdateClassification
            SecurityBulletinID:string option
            KBArticleID:string option
        }
    
    type SoftwareDistributionPackage =
        {
            PackageId:string
            Title:string
            Description:string
            ProductName:string
            UpdateSpecificData:UpdateSpecificData            
            IsInstallable:ApplicabilityRule //An applicability expression that evaluates to true if the package is even relevalant to this machine (e.g., SQL server on XP home). This rule allows the author to specify what prerequisite conditions are necessary for this package to be applicable. If not specified, assummed to be true.
            IsInstalled:ApplicabilityRule option //An applicability expression that evaluates to true if the package is actually installed. This expression is supposed to check for the exact package, not for something that supersedes it.
            InstallableItems:InstallableItem[]
        }

    type SystemManagementCatalog =
        {
            SoftwareDistributionPackage:array<SoftwareDistributionPackage>
        }

    let loadSdpXDocument (sdpFilePath:Path) : Result<XDocument,Exception> =
        result{
            let! existingSdpFilePath = FileOperations.ensureFileExists sdpFilePath
            let sdpXDocument = XDocument.Load(FileSystem.pathValue existingSdpFilePath)
            return sdpXDocument
        }
        
    let loadSdpXElement (sdpXDocument:XDocument) : Result<XElement,Exception> =
        try            
            Result.Ok sdpXDocument.Root
        with
        |ex -> Result.Error ex
    
    let sdpNs =
        XNamespace.Get("http://schemas.microsoft.com/wsus/2005/04/CorporatePublishing/SoftwareDistributionPackage.xsd")
    let cmdNs =
        XNamespace.Get("http://schemas.microsoft.com/wsus/2005/04/CorporatePublishing/Installers/CommandLineInstallation.xsd")
    let msiNs =
        XNamespace.Get("http://schemas.microsoft.com/wsus/2005/04/CorporatePublishing/Installers/MsiInstallation.xsd")

    let getOptionalAttribute (xElement:XElement) (attributeName:string) =        
        match xElement with
        | null -> None
        |_ -> 
            let attribute = xElement.Attribute(XName.Get(attributeName))            
            match attribute with
            |null -> None
            |_ -> Some attribute.Value

    let getRequiredAttribute (xElement:XElement) (attributeName:string) =
        match xElement with
        | null -> Result.Error (new Exception(sprintf "Element is null. Failed to get attribute name '%s'" attributeName))
        |_ -> 
            let attribute = xElement.Attribute(XName.Get(attributeName))            
            match attribute with
            |null -> Result.Error (new Exception(sprintf "Attribute '%s' not found on element: %A" attributeName xElement))
            |_ -> Result.Ok attribute.Value
    
    let toResult value errorMessage =
        match value with
        | null -> Result.Error (new Exception(errorMessage))
        | _ -> Result.Ok value

    let getSdpElement (parentXElement:XElement) (elementName:string) =
        toResult (parentXElement.Element(XName.Get(elementName,sdpNs.NamespaceName))) (sprintf "Element '%s' not found on parent element: '%A'" elementName parentXElement)

    let getCmdElement (parentXElement:XElement) (elementName:string) =
        toResult (parentXElement.Element(XName.Get(elementName,cmdNs.NamespaceName))) (sprintf "Element '%s' not found on parent element: '%A'" elementName parentXElement)

    let getCmdElements (parentXElement:XElement) (elementName:string) =
        toResult (parentXElement.Elements(XName.Get(elementName,cmdNs.NamespaceName))) (sprintf "Element '%s' not found on parent element: '%A'" elementName parentXElement)

    let getSdpElementValue (parentXElement:XElement) (elementName:string) =
        result{
            let! element = (getSdpElement parentXElement elementName)
            return element.Value
        }

    let innerXml (xElement:XElement) = 
        xElement.Nodes()
        |>Seq.filter(fun n -> (n.NodeType = System.Xml.XmlNodeType.Element)||(n.NodeType = System.Xml.XmlNodeType.Text))
        |>Seq.map(fun n -> n.ToString())
        |>Seq.toArray
        |>String.concat ""

    let toOptionalInnerXml (xElement:XElement option) =
        match xElement with
        |Some v -> Some (innerXml v)
        |None -> None

    let getOptionalSdpElementValue (parentXElement:XElement) (elementName:string) =
        match((getSdpElement parentXElement elementName)) with
        |Ok v -> Some v.Value
        |Error _ -> None

    let getOptionalSdpElement (parentXElement:XElement) (elementName:string) =
        match((getSdpElement parentXElement elementName)) with
        |Ok v -> Some v
        |Error _ -> None

    let resultToOption result =
        match result with
        |Ok v -> Some v
        |Error ex -> None

    let toApplicabilityRules (sdpInstallableItemElement:XElement) =
        result
            {
                let! applicabilityRulesSdpElement = (getSdpElement sdpInstallableItemElement "ApplicabilityRules")
                let isInstalledSdpElement = (getOptionalSdpElement applicabilityRulesSdpElement "IsInstalled")
                let isInstalled = toOptionalInnerXml isInstalledSdpElement
                let isInstallableSdpElement = (getOptionalSdpElement applicabilityRulesSdpElement "IsInstallable")
                let isInstallable = toOptionalInnerXml isInstallableSdpElement
                let isSupersededSdpElement = (getOptionalSdpElement applicabilityRulesSdpElement "IsSuperseded")
                let isSuperseded = toOptionalInnerXml isSupersededSdpElement

                return                 
                    {
                        IsInstalled=isInstalled
                        IsInstallable=isInstallable
                        IsSuperseded=isSuperseded
                    }
            }

    type InstallableItemProperties =
        |InstallProperties
        |UninstallProperties

    let toInstallablePropertiesName (installableItemProperties:InstallableItemProperties) =
        match installableItemProperties with
        |InstallProperties -> "InstallProperties"
        |UninstallProperties -> "UninstallProperties"

    let toBoolean booleanString =
        match booleanString with
        |"true" -> Result.Ok true
        |"false" -> Result.Ok false
        |_ -> Result.Error (new Exception(sprintf "Invalid boolean '%s'. Valid values: [true|false]" booleanString))

    let toInstallProperties (sdpInstallableItemElement:XElement) (installableItemProperties:InstallableItemProperties)  =
        result
            {
                let! sdpInstallPropertyElement = getSdpElement sdpInstallableItemElement (toInstallablePropertiesName installableItemProperties)
                let! canRequestUserInputR = getRequiredAttribute sdpInstallPropertyElement "CanRequestUserInput"
                let! canRequestUserInput = toBoolean canRequestUserInputR
                let! requiresNetworkConnectivityR = getRequiredAttribute sdpInstallPropertyElement "RequiresNetworkConnectivity"
                let! requiresNetworkConnectivity = toBoolean requiresNetworkConnectivityR
                let! installImpactR = getRequiredAttribute sdpInstallPropertyElement "Impact"
                let! installImpact = toInstallImpact installImpactR
                let! installationRebootBehaviorR = getRequiredAttribute sdpInstallPropertyElement "RebootBehavior"
                let! installationRebootBehavior = toInstallatioRebootBehaviour installationRebootBehaviorR
                return
                    {
                        CanRequestUserInput=canRequestUserInput
                        RequiresNetworkConnectivity=requiresNetworkConnectivity
                        Impact=installImpact
                        RebootBehavior=installationRebootBehavior
                    }
            }

    let toInt32 (number:string) = 
        try
            Result.Ok (Convert.ToInt32(number))
        with
        |ex -> Result.Error ex

    let toReturnCode (returnCodeElement:XElement) =
        result
            {
                let! exitCodeR = getRequiredAttribute returnCodeElement "Code"
                let! exitCode = toInt32 exitCodeR
                let! installationResultR = getRequiredAttribute returnCodeElement "Result"
                let! installationResult = toInstallationResult installationResultR
                let! rebootR = getRequiredAttribute returnCodeElement "Reboot"
                let! reboot = toBoolean rebootR
                return
                    {
                        Code=exitCode
                        Result=installationResult
                        Reboot=reboot
                    }
            }

    let toReturnCodes (commandLineInstallerDataElement:XElement) =
        result
            {
                let! returnCodeElements = getCmdElements commandLineInstallerDataElement "ReturnCode"
                let! returnCodes =
                    returnCodeElements
                    |>Seq.map toReturnCode                    
                    |>toAccumulatedResult
                    

                return returnCodes|>Seq.toArray
                    
            }

    let toCommanLineInstallerData (sdpInstallableItemElement:XElement) =
        result
            {
                let! commandLineInstallerDataElement = getCmdElement sdpInstallableItemElement "CommandLineInstallerData"
                let! program = getRequiredAttribute commandLineInstallerDataElement "Program"
                let! arguments = getRequiredAttribute commandLineInstallerDataElement "Arguments"
                let! rebootByDefaultR = getRequiredAttribute commandLineInstallerDataElement "RebootByDefault"
                let! rebootByDefault = toBoolean rebootByDefaultR
                let! defaultResultR = getRequiredAttribute commandLineInstallerDataElement "DefaultResult"
                let! defaultResult = toInstallationResult defaultResultR
                let! returnCodes = toReturnCodes commandLineInstallerDataElement
                return 
                    InstallerData.CommandLineInstallerData {
                        Program=program
                        Arguments=arguments
                        RebootByDefault=rebootByDefault
                        DefaultResult=defaultResult
                        ReturnCodes=returnCodes
                    }
            }

    let toInstallerData (sdpInstallableItemElement:XElement) =
        result
            {
                let! commandLineInstallerData = (toCommanLineInstallerData sdpInstallableItemElement)
                return commandLineInstallerData
            }

    let toInstallableItem (sdpInstallableItemXElement:XElement) =
        result
            {
                let! id = getRequiredAttribute sdpInstallableItemXElement "ID"
                let! applicabilityRules = toApplicabilityRules sdpInstallableItemXElement
                let! installProperties = toInstallProperties sdpInstallableItemXElement InstallableItemProperties.InstallProperties
                let uninstallProperties = resultToOption (toInstallProperties sdpInstallableItemXElement InstallableItemProperties.UninstallProperties)
                let! installerData = toInstallerData sdpInstallableItemXElement
                return 
                    {
                        Id = id
                        ApplicabilityRules=applicabilityRules
                        InstallProperties=installProperties
                        UninstallProperties=uninstallProperties
                        InstallerData=installerData
                    } 
            }

    let toInstallableItems (sdpXElement:XElement) = 
        sdpXElement.Elements(XName.Get("InstallableItem",sdpNs.NamespaceName))        
        |>Seq.map(fun ii -> (toInstallableItem ii))
        |>toAccumulatedResult

    let loadSdp (sdpXElement:XElement): Result<SoftwareDistributionPackage, Exception> =
        result{
            let! localizedPropertiesSdpElement = (getSdpElement sdpXElement "LocalizedProperties")
            let! title = getSdpElementValue localizedPropertiesSdpElement "Title"                        
            let! description = getSdpElementValue localizedPropertiesSdpElement "Description"
            let! propertiesSdpElement = (getSdpElement sdpXElement "Properties")
            let! packageId = getRequiredAttribute propertiesSdpElement "PackageID"
            let! productName = getSdpElementValue propertiesSdpElement "ProductName"

            let! updateSpecificDataSdpElement = (getSdpElement sdpXElement "UpdateSpecificData")
            let! msrcSeverityString = getRequiredAttribute updateSpecificDataSdpElement "MsrcSeverity"
            let! msrcSeverity = toMsrcSeverity msrcSeverityString
            let! updateClassificationString = getRequiredAttribute updateSpecificDataSdpElement "UpdateClassification"
            let! updateClassification = toUpdateClassification updateClassificationString
            let securityBulitinId = getOptionalSdpElementValue updateSpecificDataSdpElement "SecurityBulletinID"
            let kBArticleID = getOptionalSdpElementValue updateSpecificDataSdpElement "KBArticleID"

            let! isInstallableSdpElement = getSdpElement sdpXElement "IsInstallable"
            let isInstallable = innerXml isInstallableSdpElement
            let isInstalledSdpElement = getOptionalSdpElement sdpXElement "IsInstalled"
            let isInstalled = toOptionalInnerXml isInstalledSdpElement
            
            let! installableItems = toInstallableItems sdpXElement
            
            return {
                    Title = title
                    Description= description
                    ProductName = productName
                    PackageId = packageId
                    UpdateSpecificData={MsrcSeverity= msrcSeverity;UpdateClassification=updateClassification;SecurityBulletinID=securityBulitinId;KBArticleID=kBArticleID}
                    IsInstallable=isInstallable
                    IsInstalled=isInstalled
                    InstallableItems=installableItems|>Seq.toArray
                }            
        }        