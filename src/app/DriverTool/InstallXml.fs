namespace DriverTool

module InstallXml =
    
    open System.Xml.Linq
    open DriverTool.Library.XmlToolKit
    open DriverTool.Library.F
    open DriverTool.Library
    
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
                let! logDirectory = XmlHelper.getElementValue configurationXElement "LogDirectory"
                let! logFileName = XmlHelper.getElementValue configurationXElement "LogFileName"
                let! packageName = XmlHelper.getElementValue configurationXElement "PackageName"
                let! packageVersion = XmlHelper.getElementValue configurationXElement "PackageVersion"
                let! packageRevision = XmlHelper.getElementValue configurationXElement "PackageRevision"
                let! publisher = XmlHelper.getElementValue configurationXElement "Publisher"
                let! computerVendor = XmlHelper.getElementValue configurationXElement "ComputerVendor"
                let! computerModel = XmlHelper.getElementValue configurationXElement "ComputerModel"
                let! computerSystemFamiliy = XmlHelper.getElementValue configurationXElement "ComputerSystemFamiliy"
                let! osShortName = XmlHelper.getElementValue configurationXElement "OsShortName"
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