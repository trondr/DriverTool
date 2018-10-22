namespace DriverTool
open System

module PackageDefinition =
    
    type PackageDefinition =
        {
            Name:string;
            Version:string;
            Publisher:string;
            Language:string;
            InstallCommandLine:string;
            UnInstallCommandLine:string;
        }

    let getPackageDefinitionContent (packageDefinition:PackageDefinition) =
        let contentFormatString = 
            """[PDF]
Version=2.0

[Package Definition]
Name={0}
Version={1}
Publisher={2}
Language={3}
Comment=Install Drivers
Programs=INSTALL,UNINSTALL

[INSTALL]
Name=INSTALL
CommandLine={4}
CanRunWhen=AnyUserStatus
UserInputRequired=False
AdminRightsRequired=True
UseInstallAccount=True
Run=Minimized
Icon=App.ico
Comment=

[UNINSTALL]
Name=UNINSTALL
CommandLine={5}
CanRunWhen=AnyUserStatus
UserInputRequired=False
AdminRightsRequired=True
UseInstallAccount=True
Run=Minimized
Icon=App.ico
Comment=

[DetectionMethod]
RegistryValue=
RegistryValueIs64Bit=
            """
        String.Format(contentFormatString,
            packageDefinition.Name,
            packageDefinition.Version,
            packageDefinition.Publisher,
            packageDefinition.Language,
            packageDefinition.InstallCommandLine,
            packageDefinition.UnInstallCommandLine)

    
