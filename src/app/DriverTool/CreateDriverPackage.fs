namespace DriverTool
open Microsoft.FSharp.Collections
open F

module CreateDriverPackage =
    let logger = Logging.getLoggerByName("CreateDriverPackage")
    
    open System
    open DriverTool
    open System.Net
    open FSharp.Collections.ParallelSeq
    open Checksum

    let validateExportCreateDriverPackageParameters (modelCode:Result<ModelCode,Exception>, operatingSystemCode:Result<OperatingSystemCode,Exception>) = 
        
        let validationResult = 
            match modelCode with
                    |Ok m ->
                        match operatingSystemCode with
                        |Ok os -> Result.Ok (m, os)                            
                        |Error ex -> Result.Error ex
                    |Error ex -> Result.Error ex
        match validationResult with
        |Ok _ -> validationResult
        |Error _ -> 
            //Accumulate all non-empty error messages into an array
            let errorMessages = 
                [|
                    (match modelCode with
                    |Error ex -> ex.Message
                    |Ok _-> String.Empty);

                    (match operatingSystemCode with
                    |Error ex -> ex.Message
                    |Ok _-> String.Empty);
                |] |> Array.filter (fun m -> (not (String.IsNullOrWhiteSpace(m)) ) )            
            Result.Error (new Exception(String.Format("Failed to validate one or more input parameters.{0}{1}",Environment.NewLine, String.Join(Environment.NewLine, errorMessages))))
    
    
    let getUniqueUpdates packageInfos = 
        let uniqueUpdates = 
            packageInfos
            |> Seq.groupBy (fun p -> p.InstallerName)
            |> Seq.map (fun (k,v) -> v |>Seq.head)
        uniqueUpdates

    let getUniqueUpdatesR (updatesResult : Result<seq<PackageInfo>,Exception>) : Result<seq<PackageInfo>,Exception> =
        match updatesResult with
        |Error ex -> Result.Error ex
        |Ok u ->             
            Seq.groupBy (fun p -> p.InstallerName) u            
            |> Seq.map (fun (k,v) -> v |>Seq.head)
            |> Result.Ok
    
    let verifyDownload downloadJob verificationWarningOnly =
        match (hasSameFileHash (downloadJob.DestinationFile, downloadJob.Checksum, downloadJob.Size)) with
        |true  -> Result.Ok downloadJob
        |false -> 
            let msg = String.Format("Destination file ('{0}') hash does not match source file ('{1}') hash.",downloadJob.DestinationFile,downloadJob.SourceUri.OriginalString)
            match verificationWarningOnly with
            |true ->
                Logging.getLoggerByName("verifyDownload").Warn(msg)
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

    let downloadUpdatesR destinationDirectory (packageInfos : Result<seq<PackageInfo>,Exception>) =        
        match packageInfos with
        | Ok ps -> downloadUpdates destinationDirectory ps            
        | Error ex -> Result.Error ex
    
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
    open DriverTool

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

    let extractUpdateToPackageFolder downloadJob packageFolder =
        Result.Error "Not implemented"

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

    let extractUpdate (rootDirectory:Path, (downloadedPackageInfo:DownloadedPackageInfo)) =
        result{
            let packageFolderName = getPackageFolderName downloadedPackageInfo.Package
            let! packageFolderPath = DriverTool.PathOperations.combine2Paths (rootDirectory.Value, packageFolderName)
            let! existingPackageFolderPath = DirectoryOperations.ensureDirectoryExistsAndIsEmpty (packageFolderPath, true)
            let! extractReadmeResult = extractReadme (downloadedPackageInfo, existingPackageFolderPath)
            let! extractInstallerResult = extractInstaller (downloadedPackageInfo, existingPackageFolderPath)
            return extractInstallerResult
        }

    let extractUpdates rootDirectory downloadedPackageInfos = 
        downloadedPackageInfos
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
                sw.WriteLine("%~dp0\\..\\..\\Tools\\DpInstExitCode2ExitCode.exe %DpInstExitCode%")            
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
                Path.create (System.IO.Path.Combine(extractedUpdate.ExtractedDirectoryPath,"Install-Package.cmd"))
            let installCommandLine = 
                extractedUpdate.DownloadedPackage.Package.InstallCommandLine.Replace("%PACKAGEPATH%\\","")
            return! (createInstallScriptFile (installScriptPath,installCommandLine))          
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
    open System.Reflection

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

    let createDriverPackageSimple ((model: ModelCode), (operatingSystem:OperatingSystemCode), (destinationFolderPath: Path)) = 
        let driversResult =
            result {
                let! packageInfos = ExportRemoteUpdates.getRemoteUpdates (model, operatingSystem, true)
                let uniquePackageInfos = packageInfos |> Seq.distinct
                let uniqueUpdates = uniquePackageInfos |> getUniqueUpdates
                let! updates = downloadUpdates (System.IO.Path.GetTempPath()) uniqueUpdates
                let! driversPath = combine2Paths (destinationFolderPath.Value, "Drivers")
                let! existingDriversPath = DirectoryOperations.ensureDirectoryExists (driversPath, true)
                let! extractedUpdates = extractUpdates existingDriversPath updates
                let extractedUpdates2 = 
                    createInstallScripts (extractedUpdates)
                    
                return! extractedUpdates2
            }
        let toolsExtractResult =
            result{
                let! toolsPath = combine2Paths (destinationFolderPath.Value, "Tools")
                let! existingToolsPath = DirectoryOperations.ensureDirectoryExists (toolsPath, true)
                let dpInstToExitCodeExe = extractDpInstExitCodeToExitCodeExe existingToolsPath
                return! dpInstToExitCodeExe
            }
        [|driversResult;toolsExtractResult|]
        |>toAccumulatedResult
    
    let createDriverPackage ((modelCode: ModelCode), (operatingSystem:OperatingSystemCode),(destinationFolderPath: Path)) =
        Logging.debugLoggerResult createDriverPackageSimple (modelCode, operatingSystem, destinationFolderPath)

        