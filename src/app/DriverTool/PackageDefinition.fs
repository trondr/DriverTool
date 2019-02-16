namespace DriverTool
open System

module PackageDefinition =
    
    type ApplicationRegistryValue = {Path:string;ValueName:string;Value:string}

    let getApplicationRegistryValueBase companyName applicationName installRevision =
        {
            Path=sprintf "HKLM\\SOFTWARE\\%s\\Applications\\%s" companyName applicationName
            ValueName="InstallRevision"
            Value=installRevision
        }
    
    open DriverTool.InstallXml

    let getApplicationRegistryValue (installConfiguration:InstallConfigurationData) =
        getApplicationRegistryValueBase installConfiguration.Publisher installConfiguration.PackageName installConfiguration.PackageRevision

    type PackageDefinition =
        {
            Name:string;
            Version:string;
            Publisher:string;
            Language:string;
            InstallCommandLine:string;
            UnInstallCommandLine:string;
            RegistryValue:string
            RegistryValueIs64Bit:string
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
RegistryValue={6}
RegistryValueIs64Bit={7}
            """
        String.Format(contentFormatString,
            packageDefinition.Name,
            packageDefinition.Version,
            packageDefinition.Publisher,
            packageDefinition.Language,
            packageDefinition.InstallCommandLine,
            packageDefinition.UnInstallCommandLine,
            packageDefinition.RegistryValue,
            packageDefinition.RegistryValueIs64Bit
            )
    
    let writePackageDefinitionToFile (filePath:FileSystem.Path) (packageDefinition:PackageDefinition) =
        match FileOperations.writeContentToFile (filePath) (getPackageDefinitionContent packageDefinition) with
        |Ok p -> Result.Ok ()
        |Error ex -> Result.Error ex
    
    let getPackageDefinitionFromInstallConfiguration (installConfiguration:InstallConfigurationData) =
        let applicationRegistryValue = getApplicationRegistryValue installConfiguration
        {
            Name=installConfiguration.PackageName;
            Version=installConfiguration.PackageVersion;
            Publisher=installConfiguration.Publisher;
            Language="EN";
            InstallCommandLine = sprintf "Install.cmd > \"%s\\%s_%s_Install.cmd.log\"" installConfiguration.LogDirectory installConfiguration.PackageName installConfiguration.PackageVersion;
            UnInstallCommandLine = sprintf "UnInstall.cmd > \"%s\\%s_%s_UnInstall.cmd.log\"" installConfiguration.LogDirectory installConfiguration.PackageName  installConfiguration.PackageVersion
            RegistryValue=sprintf "[%s]%s=%s" applicationRegistryValue.Path applicationRegistryValue.ValueName applicationRegistryValue.Value
            RegistryValueIs64Bit="true"
        }
