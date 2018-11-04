namespace DriverTool
open Microsoft.FSharp.Collections
open F

module CreateDriverPackage =
    let logger = Logging.getLoggerByName("CreateDriverPackage")
    
    open System
    open DriverTool
    open FSharp.Collections.ParallelSeq
    open Checksum

    let getUniqueUpdates packageInfos = 
        let uniqueUpdates = 
            packageInfos
            |> Seq.groupBy (fun p -> p.InstallerName)
            |> Seq.map (fun (k,v) -> v |>Seq.head)
        uniqueUpdates

    let verifyDownload downloadJob verificationWarningOnly =
        match (hasSameFileHash (downloadJob.DestinationFile, downloadJob.Checksum, downloadJob.Size)) with
        |true  -> Result.Ok downloadJob
        |false -> 
            let msg = String.Format("Destination file ('{0}') hash does not match source file ('{1}') hash.",downloadJob.DestinationFile,downloadJob.SourceUri.OriginalString)
            match verificationWarningOnly with
            |true ->
                logger.Warn(msg)
                Result.Ok downloadJob
            |false->Result.Error (new Exception(msg))
 
    open DriverTool.Web
    
    let downloadUpdatePlain (downloadInfo:DownloadInfo, ignoreVerificationErrors) =
        downloadIfDifferent (downloadInfo, ignoreVerificationErrors)         

    let downloadUpdate (downloadJob, ignoreVerificationErrors) =
        Logging.debugLoggerResult downloadUpdatePlain (downloadJob, ignoreVerificationErrors)

    let packageInfosToDownloadedPackageInfos destinationDirectory packageInfos =
        packageInfos
        |> Seq.map (fun p -> 
                        {
                            InstallerPath = getDestinationInstallerPath destinationDirectory p;
                            ReadmePath = getDestinationReadmePath destinationDirectory p;
                            PackageXmlPath = getDestinationPackageXmlPath destinationDirectory p;
                            Package = p;
                        }
                    )
    
    let (|TextFile|_|) (input:string) = if input.ToLower().EndsWith(".txt") then Some(input) else None

    let ignoreVerificationErrors downloadInfo =
        match downloadInfo.DestinationFile.Value with
        | TextFile x -> true
        | _ -> false
    
    let downloadUpdates destinationDirectory packageInfos = 
        let downloadJobs = 
            packageInfos 
            |> packageInfosToDownloadJobs destinationDirectory
            |> PSeq.map (fun dj -> downloadUpdate (dj,ignoreVerificationErrors dj))
            |> PSeq.toArray
            |> Seq.ofArray
            |> toAccumulatedResult
        match downloadJobs with
        |Ok _ -> 
            Result.Ok (packageInfosToDownloadedPackageInfos destinationDirectory packageInfos)
        |Error ex -> 
            Result.Error ex

    let toTitlePostFix (title:string) (version:string) (releaseDate:string) = 
        nullOrWhiteSpaceGuard title "title"
        let parts = title.Split('-');
        let titlePostfix = 
            match parts.Length with
            | 0 -> String.Empty
            | _ -> parts.[parts.Length - 1]
        toValidDirectoryName (String.Format("{0}_{1}_{2}",titlePostfix,version,releaseDate))
    
    open System.Linq
    open ExistingPath
    
    let toTitlePrefix (title:string) (category:string) (postFixLength: int) = 
        nullOrWhiteSpaceGuard title "title"
        nullGuard category "category"
        let parts = title.Split('-');
        let partsString =
            (parts.[0]).AsEnumerable().Take(57 - postFixLength - category.Length).ToArray()
        let titlePrefix = 
            category + "_" + new String(partsString);
        toValidDirectoryName titlePrefix    

    let getPackageFolderName (packageInfo:PackageInfo) =
        let validDirectoryName = 
            toValidDirectoryName packageInfo.Title
        let postfix = 
            toTitlePostFix validDirectoryName packageInfo.Version packageInfo.ReleaseDate
        let prefix = 
            toTitlePrefix validDirectoryName (packageInfo.Category |? String.Empty) postfix.Length
        let packageFolderName = 
            String.Format("{0}_{1}",prefix,postfix).Replace("__", "_").Replace("__", "_");
        packageFolderName

    let downloadedPackageInfoToExtractedPackageInfo (packageFolderPath:Path,downloadedPackageInfo) =
        {
            ExtractedDirectoryPath = packageFolderPath.Value;
            DownloadedPackage = downloadedPackageInfo;
        }

    let copyFile (sourceFilePath, destinationFilePath) =
        try
            System.IO.File.Copy(sourceFilePath, destinationFilePath, true)
            Result.Ok destinationFilePath
        with
        | ex -> Result.Error (new Exception(String.Format("Failed to copy file '{0}'->'{1}'.", sourceFilePath, destinationFilePath), ex))
    
    let extractPackageXml (downloadedPackageInfo, packageFolderPath:Path)  =
        let destinationFilePath = System.IO.Path.Combine(packageFolderPath.Value,downloadedPackageInfo.Package.PackageXmlName)
        match ExistingFilePath.New downloadedPackageInfo.PackageXmlPath with
        |Ok filePath -> 
            match (copyFile (filePath.Value, destinationFilePath)) with
            |Ok _ -> 
                Result.Ok (downloadedPackageInfoToExtractedPackageInfo (packageFolderPath,downloadedPackageInfo))
            |Error ex -> Result.Error ex
        |Error ex -> Result.Error ex

    let extractReadme (downloadedPackageInfo, packageFolderPath:Path)  =
        let destinationReadmeFilePath = System.IO.Path.Combine(packageFolderPath.Value,downloadedPackageInfo.Package.ReadmeName)
        match ExistingFilePath.New downloadedPackageInfo.ReadmePath with
        |Ok readmeFilePath -> 
            match (copyFile (readmeFilePath.Value, destinationReadmeFilePath)) with
            |Ok _ -> 
                Result.Ok (downloadedPackageInfoToExtractedPackageInfo (packageFolderPath,downloadedPackageInfo))
            |Error ex -> Result.Error ex
        |Error ex -> Result.Error ex

    let getFileNameFromCommandLine (commandLine:string) = 
        let fileName = commandLine.Split(' ').[0];
        fileName;

    let extractInstaller (downloadedPackageInfo, packageFolderPath:Path) =
        if(String.IsNullOrWhiteSpace(downloadedPackageInfo.Package.ExtractCommandLine)) then
            //Installer does not support extraction, copy the installer directly to package folder...
           let destinationInstallerFilePath = System.IO.Path.Combine(packageFolderPath.Value,downloadedPackageInfo.Package.InstallerName)
           match ExistingFilePath.New downloadedPackageInfo.InstallerPath with
           |Ok installerPath -> 
                match copyFile (installerPath.Value, destinationInstallerFilePath) with
                |Ok _ -> 
                    Result.Ok (downloadedPackageInfoToExtractedPackageInfo (packageFolderPath,downloadedPackageInfo))
                |Error ex -> Result.Error ex
           |Error ex -> 
                Result.Error ex
        else
            //Installer supports extraction
            let extractCommandLine = downloadedPackageInfo.Package.ExtractCommandLine.Replace("%PACKAGEPATH%",String.Format("\"{0}\"",packageFolderPath.Value))
            let fileName = getFileNameFromCommandLine extractCommandLine
            let arguments = extractCommandLine.Replace(fileName,"")
            match (ExistingFilePath.New downloadedPackageInfo.InstallerPath) with
            |Ok fp -> 
                match DriverTool.ProcessOperations.startProcess (fp.Value, arguments) with
                |Ok _ -> Result.Ok (downloadedPackageInfoToExtractedPackageInfo (packageFolderPath,downloadedPackageInfo))
                |Error ex -> Result.Error ex
            |Error ex -> Result.Error ex

    let extractUpdate (rootDirectory:Path, (prefix,downloadedPackageInfo:DownloadedPackageInfo)) =
        result{
            let packageFolderName = getPackageFolderName downloadedPackageInfo.Package
            let! packageFolderPath = DriverTool.PathOperations.combine2Paths (rootDirectory.Value, prefix + "_" + packageFolderName)
            let! existingPackageFolderPath = DirectoryOperations.ensureDirectoryExistsAndIsEmpty (packageFolderPath, true)
            let extractReadmeResult = extractReadme (downloadedPackageInfo, existingPackageFolderPath)
            let extractPackageXmlResult = extractPackageXml (downloadedPackageInfo, existingPackageFolderPath)
            let extractInstallerResult = extractInstaller (downloadedPackageInfo, existingPackageFolderPath)
            let result = 
                [|extractReadmeResult;extractPackageXmlResult;extractInstallerResult|]
                |> toAccumulatedResult
            let res = 
                match result with 
                | Ok r -> extractInstallerResult
                | Error ex -> Result.Error ex
            return! res
        }

    let getPrefixes count =
        Array.init count (fun index -> ((index+1)*10).ToString("D3"))
    
    let extractUpdates (rootDirectory, downloadedPackageInfos:seq<DownloadedPackageInfo>) = 
        let downloadedPackageInfosList = downloadedPackageInfos.ToList()
        let prefixes = getPrefixes downloadedPackageInfosList.Count
        downloadedPackageInfosList
        |> Seq.zip prefixes
        |> PSeq.map (fun dp -> extractUpdate (rootDirectory, dp))
        |> PSeq.toArray
        |> Seq.ofArray
        |> toAccumulatedResult
     
    open System.Text.RegularExpressions
        
    let directoryContainsDpInst (directoryPath:string) =
        let dpinstFilesCount = 
            System.IO.Directory.GetFiles(directoryPath,"*.*")
            |> Seq.filter (fun fp -> System.Text.RegularExpressions.Regex.Match((new System.IO.FileInfo(fp)).Name,"dpinst",RegexOptions.IgnoreCase).Success)
            |> Seq.toList
            |> Seq.length            
        (dpinstFilesCount > 0)

    let packageIsUsingDpInstDuringInstall (installScriptPath:Path, installCommandLine:string) = 
        match (installCommandLine.StartsWith("dpinst.exe", true, System.Globalization.CultureInfo.InvariantCulture)) with
        | true -> 
            true
        | false -> 
            directoryContainsDpInst ((new System.IO.FileInfo(installScriptPath.Value)).Directory.FullName)

    let createUnInstallScriptFile (installScriptPath: Path) =
        try
            use sw = new System.IO.StreamWriter(installScriptPath.Value)
            sw.WriteLine("Set ExitCode=0")
            sw.WriteLine("pushd \"%~dp0\"")
            sw.WriteLine("@Echo Uninstall is not supported")
            sw.WriteLine("popd")
            sw.WriteLine("EXIT /B %ExitCode%")
            Result.Ok installScriptPath
        with
        | _ as ex -> Result.Error ex

    let createInstallScriptFile (installScriptPath: Path, installCommandLine:string) =
        try
            use sw = new System.IO.StreamWriter(installScriptPath.Value)
            sw.WriteLine("Set ExitCode=0")
            sw.WriteLine("pushd \"%~dp0\"")
            sw.WriteLine("")
            sw.WriteLine(installCommandLine)
            if (packageIsUsingDpInstDuringInstall (installScriptPath, installCommandLine)) then
                sw.WriteLine("")
                sw.WriteLine("Set DpInstExitCode=%errorlevel%")
                sw.WriteLine("%~dp0..\\DpInstExitCode2ExitCode.exe %DpInstExitCode%")
            else
                sw.WriteLine("")
                sw.WriteLine("REM Set DpInstExitCode=%errorlevel%")
                sw.WriteLine("REM %~dp0..\\DpInstExitCode2ExitCode.exe %DpInstExitCode%")
            sw.WriteLine("")
            sw.WriteLine("Set ExitCode=%errorlevel%")
            sw.WriteLine("popd")
            sw.WriteLine("EXIT /B %ExitCode%")
            Result.Ok installScriptPath
        with
        | _ as ex -> Result.Error ex

    let createInstallScript (extractedUpdate:ExtractedPackageInfo) =
        result{
            let! installScriptPath = 
                Path.create (System.IO.Path.Combine(extractedUpdate.ExtractedDirectoryPath,"DT-Install-Package.cmd"))
            let installCommandLine = 
                extractedUpdate.DownloadedPackage.Package.InstallCommandLine.Replace("%PACKAGEPATH%\\","")
            let installScriptResult = (createInstallScriptFile (installScriptPath,installCommandLine))
            let! unInstallScriptPath = 
                Path.create (System.IO.Path.Combine(extractedUpdate.ExtractedDirectoryPath,"DT-UnInstall-Package.cmd"))
            let unInstallScriptResult = (createUnInstallScriptFile (unInstallScriptPath))
            
            let createInstallScriptResult = 
                match ([|installScriptResult;unInstallScriptResult|]|> toAccumulatedResult) with
                |Error ex -> Result.Error ex
                |Ok _ -> installScriptResult

            return! createInstallScriptResult
        }

    let createInstallScripts (extractedUpdates:seq<ExtractedPackageInfo>) =
        let extractedUpdatesList = 
            extractedUpdates.ToList()
        logger.InfoFormat("Creating install script for {0} packages...",extractedUpdatesList.Count)
        let installScripts = 
            extractedUpdatesList 
            |> PSeq.map (fun u -> (createInstallScript u) )       
            |> PSeq.toArray
            |> Seq.ofArray
            |> toAccumulatedResult
        installScripts
    
    open EmbeddedResouce

    let extractEmbededResouceToFile (resourceName:string , destinationFileName:string) = 
        result {
                
                let! resourceNameObject = 
                    ResourceName.create resourceName
                let! destinationFilePath = 
                    Path.create destinationFileName
                let! parentDirectoryPath = (Path.create (System.IO.Path.GetDirectoryName(destinationFilePath.Value)))
                let! existingParentDirectoryPath = DirectoryOperations.ensureDirectoryExists (parentDirectoryPath, true)
                logger.Info("Verified that directory exists:" + existingParentDirectoryPath.Value)
                let assembly = destinationFilePath.GetType().Assembly
                logger.Info(String.Format("Extracting resource '{0}' -> '{1}'",resourceName, destinationFilePath.Value))
                let! fileResult = 
                    EmbeddedResouce.extractEmbeddedResourceToFile (resourceNameObject,assembly, destinationFilePath)
                return fileResult
            }

    let extractEmbeddedResource (resourceName, destinationFolderPath:Path, destinationFileName) =
        result {
                let assembly = destinationFolderPath.GetType().Assembly
                let! exeResourceName = 
                    ResourceName.create resourceName
                let! exeFilePath = 
                    Path.create (System.IO.Path.Combine(destinationFolderPath.Value, destinationFileName))
                let! fileResult = 
                    EmbeddedResouce.extractEmbeddedResourceToFile (exeResourceName,assembly, exeFilePath)
                return fileResult
            }

    let extractDpInstExitCodeToExitCodeExe (toolsPath:Path) =
        let exeResult = 
            extractEmbeddedResource ("DriverTool.Tools.DpInstExitCode2ExitCode.exe", toolsPath,"DpInstExitCode2ExitCode.exe")
        let configResult =
            extractEmbeddedResource ("DriverTool.Tools.DpInstExitCode2ExitCode.exe.config",toolsPath,"DpInstExitCode2ExitCode.exe.config")
        [|exeResult;configResult|]
        |> toAccumulatedResult
    
    open DriverTool.PathOperations

    let getLastestReleaseDate (updates:seq<DownloadedPackageInfo>) =
        updates
        |> Seq.map (fun p -> p.Package.ReleaseDate)
        |> Seq.max
    
    let writeTextToFile (text:string, filePath:Path) =
        try
            use sw = new System.IO.StreamWriter(filePath.Value)
            sw.Write(text)
            Result.Ok filePath
        with
        | ex -> Result.Error (new Exception(String.Format("Failed to write text to file '{0}'.", filePath.Value), ex))

    open PackageDefinition
    open System.Resources

    let createPackageDefinitionFile (logDirectory, extractedUpdate:ExtractedPackageInfo) = 
        result{
            let! packageDefinitonSmsPath = combine2Paths (extractedUpdate.ExtractedDirectoryPath,"PackageDefinition.sms")   
            let installLogFileName = toValidDirectoryName ("Install_" + extractedUpdate.DownloadedPackage.Package.Title + "_" + extractedUpdate.DownloadedPackage.Package.Version + ".log")
            let unInstallLogFileName = toValidDirectoryName ("UnInstall_" + extractedUpdate.DownloadedPackage.Package.Title + "_" + extractedUpdate.DownloadedPackage.Package.Version + ".log")
            let installLogFile = System.IO.Path.Combine(logDirectory,installLogFileName)
            let unInstallLogFile = System.IO.Path.Combine(logDirectory,unInstallLogFileName)
            let packageDefinition =
                {
                    Name = extractedUpdate.DownloadedPackage.Package.Title;
                    Version = extractedUpdate.DownloadedPackage.Package.Version;
                    Language = "EN";
                    Publisher = "LENOVO";
                    InstallCommandLine = String.Format("Install-Package.cmd > \"{0}\"",installLogFile);
                    UnInstallCommandLine = String.Format("UnInstall-Package.cmd > \"{0}\"",unInstallLogFile);
                }
            let writeTextToFileResult = writeTextToFile ((getPackageDefinitionContent packageDefinition), packageDefinitonSmsPath)                
            return! writeTextToFileResult
        }
    
    let createPackageDefinitionFiles (extractedUpdates:seq<ExtractedPackageInfo>, logDirectory:string) =
        extractedUpdates
        |> PSeq.map (fun u -> (createPackageDefinitionFile (logDirectory, u)))
        |> PSeq.toArray
        |> Seq.ofArray
        |> toAccumulatedResult
    

    let resourceNameToDirectoryDictionary (destinationFolderPath:Path) = 
        dict[
        "DriverTool.PackageTemplate", destinationFolderPath.Value;
        "DriverTool.PackageTemplate.Functions", System.IO.Path.Combine(destinationFolderPath.Value,"Functions");
        "DriverTool.PackageTemplate.Functions.Util", System.IO.Path.Combine(destinationFolderPath.Value,"Functions","Util");
        "DriverTool.PackageTemplate.Functions.Util.7Zip", System.IO.Path.Combine(destinationFolderPath.Value,"Functions","Util","7Zip");
        "DriverTool.PackageTemplate.Functions.Util.BitLocker", System.IO.Path.Combine(destinationFolderPath.Value,"Functions","Util","BitLocker");
        "DriverTool.PackageTemplate.Functions.Util.INIFileParser", System.IO.Path.Combine(destinationFolderPath.Value,"Functions","Util","INIFileParser");
        "DriverTool.PackageTemplate.Drivers", System.IO.Path.Combine(destinationFolderPath.Value,"Drivers");
        "DriverTool.PackageTemplate.Drivers_Example", System.IO.Path.Combine(destinationFolderPath.Value,"Drivers_Example");
        "DriverTool.PackageTemplate.Drivers_Example._020_Audio_Realtek_Audio_Driver_10_64_6._0._1._8224_2017_08_23", System.IO.Path.Combine(destinationFolderPath.Value, "Drivers_Example", "_020_Audio_Realtek_Audio_Driver_10_64_6._0._1._8224_2017_08_23");        
        "DriverTool.PackageTemplate.Drivers_Example._040_Camera_and_Card_Reader_Re_10_64_10._0._16299._21304_2018_03_29", System.IO.Path.Combine(destinationFolderPath.Value, "Drivers_Example", "_040_Camera_and_Card_Reader_Re_10_64_10._0._16299._21304_2018_03_29");
        ]

    let resourceNameToPartialResourceNames (resourceName:string) =
        let split = resourceName.Split(".")
        let length = split.Length
        seq{
            for i in 0..(length-1) do
                let partialResourceName = System.String.Join(".",split.[0..i])
                yield partialResourceName
        } 
        |> Seq.toArray
        |> Seq.rev
        
    let resourceNameToFileName (resourceName:string, dictionary: System.Collections.Generic.IDictionary<string,string>) =          
            let partialResourceNames = resourceNameToPartialResourceNames resourceName
            let directoryPartialName =
                partialResourceNames |> Seq.tryFind (fun x -> dictionary.ContainsKey(x))
            let fileName = 
                match directoryPartialName with
                | Some pn -> 
                    let fileN = System.IO.Path.Combine(dictionary.[pn],resourceName.Replace(pn,"").Trim('.'))
                    Some fileN
                | None -> None
            fileName

    let getPackageTemplateEmbeddedResourceNames =
        let assembly = System.Reflection.Assembly.GetExecutingAssembly()
        let embededResourceNames = 
                assembly.GetManifestResourceNames()
                |> Seq.filter (fun x -> x.StartsWith("DriverTool.PackageTemplate"))
        embededResourceNames

    let mapResourceNamesToFileNames (destinationFolderPath:Path, resourceNames:seq<string>)=
        let directoryLookDictionary = resourceNameToDirectoryDictionary destinationFolderPath
        resourceNames
        |> Seq.map (fun rn -> 
            let fileName = resourceNameToFileName (rn, directoryLookDictionary)
            match fileName with
            |Some fn -> Some (rn,fn)
            |None -> None
            )
        |> Seq.choose id
        
    let extractPackageTemplate (destinationFolderPath:Path) =
        result {
            let! emptyDestinationFolderPath = DriverTool.DirectoryOperations.ensureDirectoryExistsAndIsEmpty (destinationFolderPath, true)
            let resourceNamesVsDestinationFilesMap = mapResourceNamesToFileNames (emptyDestinationFolderPath,getPackageTemplateEmbeddedResourceNames)
            let extractResult =
                resourceNamesVsDestinationFilesMap
                |> Seq.map (fun (resourceName, fileName) ->
                        extractEmbededResouceToFile (resourceName, fileName)
                    )
            return! (extractResult |> toAccumulatedResult)
        }


    open DriverTool.Util

    let createDriverPackageSimple ((model: ModelCode), (operatingSystem:OperatingSystemCode), (destinationFolderPath: Path), logDirectory) = 
            let x = new Class1()
            logger.Info(x.X)
            
            result {
                let! packageInfos = ExportRemoteUpdates.getRemoteUpdates (model, operatingSystem, true)
                let uniquePackageInfos = packageInfos |> Seq.distinct
                let uniqueUpdates = uniquePackageInfos |> getUniqueUpdates
                
                let! updates = downloadUpdates (DriverTool.Configuration.getDownloadCacheDirectoryPath) uniqueUpdates
                let latestRelaseDate = getLastestReleaseDate updates
                let! versionedPackagePath = combine2Paths (destinationFolderPath.Value,latestRelaseDate)
                
                let! extractedPackagePaths = extractPackageTemplate versionedPackagePath
                logger.InfoFormat("Package template was extracted successfully from embedded resource. Number of files extracted: {0}", extractedPackagePaths.Count())

                let! driversPath = combine2Paths (versionedPackagePath.Value, "Drivers")
                logger.InfoFormat("Extracting drivers to folder '{0}'...", versionedPackagePath.Value)
                let! existingDriversPath = DirectoryOperations.ensureDirectoryExists (driversPath, true)
                let! extractedUpdates = extractUpdates (existingDriversPath, updates)
                
                let installScriptResults = createInstallScripts (extractedUpdates)
                               
                let packageSmsResults = createPackageDefinitionFiles (extractedUpdates, logDirectory)

                let! installXmlPath = Path.create (System.IO.Path.Combine(versionedPackagePath.Value,"Install.xml"))
                let! existingInstallXmlPath = FileOperations.ensureFileExists installXmlPath
                let! installConfiguration = DriverTool.InstallXml.loadInstallXml existingInstallXmlPath
                let updatedInstallConfiguration = { installConfiguration with LogDirectory = (DriverTool.Environment.unExpandEnironmentVariables logDirectory)}
                let! savedInstallConfiguration = DriverTool.InstallXml.saveInstallXml (existingInstallXmlPath, updatedInstallConfiguration)
                logger.InfoFormat("Saved install configuration to '{0}'. Value:", existingInstallXmlPath.Value, (Logging.valueToString savedInstallConfiguration))
                let res = 
                    match ([|installScriptResults;packageSmsResults|] |> toAccumulatedResult) with
                    |Ok _ -> Result.Ok ()
                    |Error ex -> Result.Error ex  
                return! res
            }
    
    let createDriverPackage (modelCode: ModelCode, operatingSystem:OperatingSystemCode, destinationFolderPath: Path, logDirectory) =
        Logging.debugLoggerResult createDriverPackageSimple (modelCode, operatingSystem, destinationFolderPath, logDirectory)

        