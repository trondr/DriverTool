namespace DriverTool



module InstallXml =
    
    open FSharp.Data
    //type InstallConfiguration = XmlProvider<"<configuration><LogDirectory>%PUBLIC%\LogsTest</LogDirectory><LogFileName>DriverPackageInstall-%USERNAME%.log</LogFileName><PackageName>DriverPackageX</PackageName><Packageversion>1.0</Packageversion><Publisher>MyCompany</Publisher><ComputerVendor>LENOVO</ComputerVendor><ComputerModel>20EQ0022MN</ComputerModel><ComputerSystemFamiliy>ThinkPad P50</ComputerSystemFamiliy><OsShortName>Win10X64</OsShortName></configuration>">

    type InstallConfiguration = XmlProvider<Schema="""   
    <xs:schema attributeFormDefault="unqualified" elementFormDefault="qualified" xmlns:xs="http://www.w3.org/2001/XMLSchema">
      <xs:element name="configuration">
        <xs:complexType>
          <xs:sequence>
            <xs:element name="LogDirectory" type="xs:string" />
            <xs:element name="LogFileName" type="xs:string" />
            <xs:element name="PackageName" type="xs:string" />
            <xs:element name="PackageVersion" type="xs:string" />
            <xs:element name="Publisher" type="xs:string" />
            <xs:element name="ComputerVendor" type="xs:string" />
            <xs:element name="ComputerModel" type="xs:string" />
            <xs:element name="ComputerSystemFamiliy" type="xs:string" />
            <xs:element name="OsShortName" type="xs:string" />
          </xs:sequence>
        </xs:complexType>
      </xs:element>
    </xs:schema>
        """>
    
    type InstallConfigurationData = {
        LogDirectory:string;
        LogFileName:string;
        PackageName:string;
        PackageVersion:string;
        Publisher:string;
        ComputerVendor:string;
        ComputerModel:string;
        ComputerSystemFamiliy:string;
        OsShortName:string;
    }

    let loadInstallXml (installXmlPath:Path) = 
       
        try
            let installXml = 
                InstallConfiguration.Load(installXmlPath.Value)
            Result.Ok {
                LogDirectory = installXml.LogDirectory;
                LogFileName = installXml.LogFileName;
                PackageName = installXml.PackageName;
                PackageVersion = installXml.PackageVersion;
                Publisher = installXml.Publisher;
                ComputerVendor = installXml.ComputerVendor;
                ComputerModel = installXml.ComputerModel;
                ComputerSystemFamiliy = installXml.ComputerSystemFamiliy;
                OsShortName = installXml.OsShortName;
            }
        with
        | _ as ex -> Result.Error ex
        
    open DriverTool.XmlToolKit
    
    let saveInstallXml (installXmlPath:Path, installConfigurationData:InstallConfigurationData) =
        try

            let doc =
                XDocument (XDeclaration "1.0" "UTF-8" "yes") [
                    XComment "Saved by DriverTool.InstallXml.saveInstallXml function."
                    XElement "configuration" [
                        XElement "LogDirectory" [installConfigurationData.LogDirectory]
                        XElement "LogFileName" [installConfigurationData.LogFileName]
                        XElement "PackageName" [installConfigurationData.PackageName]
                        XElement "PackageVersion" [installConfigurationData.PackageVersion]
                        XElement "Publisher" [installConfigurationData.Publisher]
                        XElement "ComputerVendor" [installConfigurationData.ComputerVendor]
                        XElement "ComputerModel" [installConfigurationData.ComputerModel]
                        XElement "ComputerSystemFamiliy" [installConfigurationData.ComputerSystemFamiliy]
                        XElement "OsShortName" [installConfigurationData.OsShortName]
                    ]
                ]
            doc.Save(installXmlPath.Value) |> ignore
            loadInstallXml installXmlPath //Verify by loading xml back from file
        with
        | _ as ex -> Result.Error ex