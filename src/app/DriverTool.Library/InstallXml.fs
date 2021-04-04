namespace DriverTool.Library

module InstallXml =
    
    open System.Xml.Linq
    open DriverTool.Library.XmlToolKit
    open DriverTool.Library.F
    open DriverTool.Library
    open DriverTool.Library.XmlHelper
    
    type InstallConfigurationData = {
        LogDirectory:string;
        LogFileName:string;
        PackageName:string;
        PackageVersion:string;
        PackageRevision:string;
        Publisher:string;
        ComputerVendor:string;
        ComputerModel:string;
        ComputerSystemFamiliy:string;
        OsShortName:string;
    }

    let toInstallDataConfiguration (configurationXElement:XElement) = 
        result
            {
                let getConfigValue = XmlHelper.getElementValue configurationXElement
                let! logDirectory = getConfigValue (xn "LogDirectory")
                let! logFileName = getConfigValue (xn "LogFileName")
                let! packageName = getConfigValue (xn "PackageName")
                let! packageVersion = getConfigValue (xn "PackageVersion")
                let! packageRevision = getConfigValue (xn "PackageRevision")
                let! publisher = getConfigValue (xn "Publisher")
                let! computerVendor = getConfigValue (xn "ComputerVendor")
                let! computerModel = getConfigValue (xn "ComputerModel")
                let! computerSystemFamiliy = getConfigValue (xn "ComputerSystemFamiliy")
                let! osShortName = getConfigValue (xn "OsShortName")
                return {
                    LogDirectory = logDirectory
                    LogFileName = logFileName
                    PackageName = packageName
                    PackageVersion = packageVersion
                    PackageRevision = packageRevision
                    Publisher = publisher
                    ComputerVendor = computerVendor
                    ComputerModel = computerModel
                    ComputerSystemFamiliy = computerSystemFamiliy
                    OsShortName = osShortName                
                }
            }

    let loadInstallXml (installXmlPath:FileSystem.Path) = 
        result{
            let! existingInstallXmlPath = FileOperations.ensureFileExistsWithMessage (sprintf "Install xml file '%A' not found." installXmlPath) installXmlPath
            let! xDocument = XmlHelper.loadXDocument existingInstallXmlPath
            let! installDataConfiguration = toInstallDataConfiguration xDocument.Root                
            return installDataConfiguration
        }

    let saveInstallXml (installXmlPath:FileSystem.Path, installConfigurationData:InstallConfigurationData) =
        try

            let doc =
                XDocument (XDeclaration "1.0" "UTF-8" "yes") [
                    XComment "Saved by DriverTool.InstallXml.saveInstallXml function."
                    XElement "configuration" [
                        XElement "LogDirectory" [installConfigurationData.LogDirectory]
                        XElement "LogFileName" [installConfigurationData.LogFileName]
                        XElement "PackageName" [installConfigurationData.PackageName]
                        XElement "PackageVersion" [installConfigurationData.PackageVersion]
                        XElement "PackageRevision" [installConfigurationData.PackageRevision]
                        XElement "Publisher" [installConfigurationData.Publisher]
                        XElement "ComputerVendor" [installConfigurationData.ComputerVendor]
                        XElement "ComputerModel" [installConfigurationData.ComputerModel]
                        XElement "ComputerSystemFamiliy" [installConfigurationData.ComputerSystemFamiliy]
                        XElement "OsShortName" [installConfigurationData.OsShortName]
                    ]
                ]
            doc.Save(FileSystem.pathValue installXmlPath) |> ignore
            loadInstallXml installXmlPath //Verify by loading xml back from file
        with
        | _ as ex -> Result.Error ex