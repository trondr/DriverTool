namespace DriverTool
open System

module PackageDefinition =
    open Common.Logging
    let logger = LogManager.GetLogger("PackageDefinition")
    
    type ApplicationRegistryValue = {Path:string;ValueName:string;Value:string}

    let getApplicationRegistryValueBase companyName applicationName applicationVersion installRevision =
        {
            Path=sprintf "HKLM\\SOFTWARE\\%s\\Applications\\%s %s" companyName applicationName applicationVersion
            ValueName="InstallRevision"
            Value=installRevision
        }
    
    open DriverTool.InstallXml

    let getApplicationRegistryValue (installConfiguration:InstallConfigurationData) =
        getApplicationRegistryValueBase installConfiguration.Publisher installConfiguration.PackageName installConfiguration.PackageVersion installConfiguration.PackageRevision

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
Name={0} {1}
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
    
    let getPackageDefinitionDismContent (packageDefinition:PackageDefinition) =
        let contentFormatString = 
            """[PDF]
Version=2.0

[Package Definition]
Name={0} {1} DISM
Version={1}
Publisher={2}
Language={3}
Comment=Insert Drivers into the offline
Programs=INSTALL-DISM

[INSTALL-DISM]
Name=INSTALL-DISM
CommandLine={4}
CanRunWhen=AnyUserStatus
UserInputRequired=False
AdminRightsRequired=True
UseInstallAccount=True
Run=Minimized
Icon=App.ico
Comment=Insert drivers into the off line operating system in the Windows PE phase using DISM.exe

            """
        String.Format(contentFormatString,
            packageDefinition.Name,
            packageDefinition.Version,
            packageDefinition.Publisher,
            packageDefinition.Language,
            packageDefinition.InstallCommandLine            
            )


    let writePackageDefinitionToFile (filePath:FileSystem.Path) (packageDefinition:PackageDefinition) =
        match FileOperations.writeContentToFile logger (filePath) (getPackageDefinitionContent packageDefinition) with
        |Ok p -> Result.Ok p
        |Error ex -> Result.Error ex
    
    let writePackageDefinitionDismToFile (filePath:FileSystem.Path) (packageDefinition:PackageDefinition) =
        match FileOperations.writeContentToFile logger (filePath) (getPackageDefinitionDismContent packageDefinition) with
        |Ok p -> Result.Ok p
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
