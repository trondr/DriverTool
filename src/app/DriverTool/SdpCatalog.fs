﻿namespace DriverTool

module SdpCatalog =
    
    open  System
    open DriverTool.FileSystem
    open System.Xml.Linq
    open System.Linq

    let logger = Logging.getLoggerByName("DriverTool.SdpCatalog")

    type ApplicabilityRule = string

    type ApplicabilityRules = 
        {
            IsInstallable:ApplicabilityRule
            IsInstalled:ApplicabilityRule
            IsSuperseded:ApplicabilityRule
        }

    type InstallationResult = Failed|Succeeded|Cancelled
    
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

    type InstallationRebootBehavior=    
        |NeverReboots
        |AlwaysRequiresReboot
        |CanRequestReboot
   
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
            OriginFile:OriginFile
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
            //IsInstallable:ApplicabilityRule //An applicability expression that evaluates to true if the package is even relevalant to this machine (e.g., SQL server on XP home). This rule allows the author to specify what prerequisite conditions are necessary for this package to be applicable. If not specified, assummed to be true.
            //IsInstalled:ApplicabilityRule //An applicability expression that evaluates to true if the package is actually installed. This expression is supposed to check for the exact package, not for something that supersedes it.
            //InstallableItem:array<InstallableItem>
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

    let getSdpElementValue (parentXElement:XElement) (elementName:string) =
        result{
            let! element = (getSdpElement parentXElement elementName)
            return element.Value
        }

    let getOptionalSdpElementValue (parentXElement:XElement) (elementName:string) =
        match((getSdpElement parentXElement elementName)) with
        |Ok v -> Some v.Value
        |Error _ -> None

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
            return {
                    Title = title
                    Description= description
                    ProductName = productName
                    PackageId = packageId
                    UpdateSpecificData={MsrcSeverity= msrcSeverity;UpdateClassification=updateClassification;SecurityBulletinID=securityBulitinId;KBArticleID=kBArticleID}
                }            
        }        