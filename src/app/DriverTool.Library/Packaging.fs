namespace DriverTool

module Packaging=
    open System.Text
    open System.Text.RegularExpressions
    open DriverTool.Library.F
    open DriverTool.Library
    open DriverTool.Library.ManufacturerTypes
    open DriverTool.Library.PackageXml
    open DriverTool.Library.PathOperations
    open DriverTool.Library.PackageDefinition
    open DriverTool.Library.DirectoryOperations


    let createUnInstallScriptFileContent () =
        let sw = StringBuilder()
        sw.AppendLine("Set ExitCode=0")|>ignore
        sw.AppendLine("pushd \"%~dp0\"")|>ignore
        sw.AppendLine("@Echo Uninstall is not supported")|>ignore
        sw.AppendLine("popd")|>ignore
        sw.AppendLine("EXIT /B %ExitCode%")|>ignore
        sw.ToString()

    let writeTextToFileUnsafe (filePath:FileSystem.Path, text:string) =
        use sw = new System.IO.StreamWriter(FileSystem.pathValue filePath)
        sw.Write(text)
        filePath
    
    let writeTextToFile (filePath:FileSystem.Path) (text:string) =
        tryCatch None writeTextToFileUnsafe (filePath, text)

    let createInstallScriptFileContent (packageIsUsingDpInst:bool, installCommandLine:string,manufacturer:Manufacturer, logDirectory:FileSystem.Path) =
        let sb = new StringBuilder()
        sb.AppendLine("Set ExitCode=0")|>ignore
        sb.AppendLine("pushd \"%~dp0\"")|>ignore
        sb.AppendLine("")|>ignore
        let logDirectoryLine = sprintf "IF NOT EXIST \"%s\" md \"%s\"" (FileSystem.pathValue logDirectory) (FileSystem.pathValue logDirectory)
        sb.Append(logDirectoryLine).AppendLine(System.String.Empty)|>ignore
        sb.AppendLine(installCommandLine)|>ignore
        if (packageIsUsingDpInst) then
            sb.AppendLine("")|>ignore
            sb.AppendLine("Set DpInstExitCode=%errorlevel%")|>ignore
            sb.AppendLine("\"%~dp0..\\DpInstExitCode2ExitCode.exe\" %DpInstExitCode%")|>ignore
        else
            sb.AppendLine("")|>ignore
            sb.AppendLine("REM Set DpInstExitCode=%errorlevel%")|>ignore
            sb.AppendLine("REM \"%~dp0..\\DpInstExitCode2ExitCode.exe\" %DpInstExitCode%")|>ignore
        match manufacturer with
        |Manufacturer.Dell _ ->
            sb.AppendLine("")|>ignore
            sb.AppendLine("Set DupExitCode=%errorlevel%")|>ignore
            sb.AppendLine("\"%~dp0..\\DriverTool.DupExitCode2ExitCode.exe\" %DupExitCode%")|>ignore
        |Manufacturer.Lenovo _ -> 
            sb.AppendLine("")|>ignore
        |Manufacturer.HP _ -> 
            sb.AppendLine("")|>ignore
        sb.AppendLine("")|>ignore
        sb.AppendLine("Set ExitCode=%errorlevel%")|>ignore
        sb.AppendLine("popd")|>ignore
        sb.AppendLine("EXIT /B %ExitCode%")|>ignore
        sb.ToString()
    
    let createSccmInstallScriptFileContent (installCommandLine:string) =
        let sb = new StringBuilder()
        sb.AppendLine("Set ExitCode=0")|>ignore
        sb.AppendLine("pushd \"%~dp0\"")|>ignore
        sb.AppendLine("")|>ignore
        sb.AppendLine(installCommandLine)|>ignore            
        sb.AppendLine("")|>ignore
        sb.AppendLine("Set ExitCode=%errorlevel%")|>ignore
        sb.AppendLine("popd")|>ignore
        sb.AppendLine("EXIT /B %ExitCode%")|>ignore
        sb.ToString()

    let dtInstallPackageCmd = "DT-Install-Package.cmd"
    let dtUnInstallPackageCmd = "DT-UnInstall-Package.cmd"

    let directoryContainsDpInst directoryPath =
        let dpinstFilesCount = 
            getFiles' directoryPath
            |> Seq.filter (fun fp -> System.Text.RegularExpressions.Regex.Match((new System.IO.FileInfo(FileSystem.longPathValue fp)).Name,"dpinst",RegexOptions.IgnoreCase).Success)
            |> Seq.toList
            |> Seq.length            
        (dpinstFilesCount > 0)

    let packageIsUsingDpInstDuringInstall (installScriptPath, installCommandLine:string) = 
        match (installCommandLine.StartsWith("dpinst.exe", true, System.Globalization.CultureInfo.InvariantCulture)) with
        | true -> 
            true
        | false -> 
            directoryContainsDpInst (getParentFolderPathUnsafe installScriptPath)

    let createInstallScript (extractedUpdate:ExtractedPackageInfo,manufacturer:Manufacturer,logDirectory:FileSystem.Path) =
        result{
            let! installScriptPath = PathOperations.combine2Paths(extractedUpdate.ExtractedDirectoryPath,dtInstallPackageCmd)
            let installCommandLine = extractedUpdate.DownloadedPackage.Package.InstallCommandLine.Replace("%PACKAGEPATH%\\TMP","%TEMP%").Replace("%PACKAGEPATH%\\","")            
            let packageIsUsingDpInst = packageIsUsingDpInstDuringInstall (installScriptPath, installCommandLine)

            let installScriptContent = (createInstallScriptFileContent (packageIsUsingDpInst,installCommandLine,manufacturer,logDirectory))
            let! installScript = writeTextToFile installScriptPath installScriptContent
            
            let! unInstallScriptPath = PathOperations.combine2Paths(extractedUpdate.ExtractedDirectoryPath,dtUnInstallPackageCmd)
            let! unInstallScript = writeTextToFile unInstallScriptPath (createUnInstallScriptFileContent())
            
            return installScript
        }

    let createSccmPackageInstallScript (extractedSccmPackagePath:FileSystem.Path) =
        result{
            let! existingextractedSccmPackagePath = DirectoryOperations.ensureDirectoryExists true extractedSccmPackagePath
            let! installScriptPath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue existingextractedSccmPackagePath,"DT-Install-Package.cmd"))
            let installCommandLine = "pnputil.exe /add-driver *.inf /install /subdirs"            
            let createSccmInstallScriptFileContent = createSccmInstallScriptFileContent installCommandLine
            let! installScript = writeTextToFile installScriptPath createSccmInstallScriptFileContent
            let! unInstallScriptPath = 
                FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue existingextractedSccmPackagePath,"DT-UnInstall-Package.cmd"))
            let! unInstallScriptResult = writeTextToFile unInstallScriptPath (createUnInstallScriptFileContent())
            return installScript
        }    
        
    let createPackageDefinitionFile (logDirectory, extractedUpdate:ExtractedPackageInfo, packagePublisher) = 
        result{
            let! packageDefinitonSmsPath = combine2Paths (extractedUpdate.ExtractedDirectoryPath,"PackageDefinition.sms")   
            let installLogFileName = toValidDirectoryName ("Install_" + extractedUpdate.DownloadedPackage.Package.Title + "_" + extractedUpdate.DownloadedPackage.Package.Version + ".log")
            let unInstallLogFileName = toValidDirectoryName ("UnInstall_" + extractedUpdate.DownloadedPackage.Package.Title + "_" + extractedUpdate.DownloadedPackage.Package.Version + ".log")
            let installLogFile = System.IO.Path.Combine(FileSystem.pathValue logDirectory,installLogFileName)
            let unInstallLogFile = System.IO.Path.Combine(FileSystem.pathValue logDirectory,unInstallLogFileName)
            let packageDefinition =
                {
                    Name = extractedUpdate.DownloadedPackage.Package.Title;
                    Version = extractedUpdate.DownloadedPackage.Package.Version;
                    Language = "EN";
                    Publisher = packagePublisher;
                    InstallCommandLine = dtInstallPackageCmd + sprintf " > \"%s\"" installLogFile;
                    UnInstallCommandLine = dtUnInstallPackageCmd + sprintf " > \"%s\"" unInstallLogFile;
                    RegistryValue="";
                    RegistryValueIs64Bit="";
                }
            let writeTextToFileResult = writeTextToFile packageDefinitonSmsPath (getPackageDefinitionContent packageDefinition)                
            return! writeTextToFileResult
        }

    let sccmPackageFolderName (releaseDate:System.DateTime) = 
        "005_Sccm_Package_" + releaseDate.ToString("yyyy_MM_dd")