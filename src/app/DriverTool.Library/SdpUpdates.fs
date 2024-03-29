﻿namespace DriverTool

module SdpUpdates =    
    open System
    open sdpeval.fsharp.Sdp
    open DriverTool.Library.PackageXml    
    open DriverTool.Library.UpdatesContext
    open DriverTool.Library.Logging
    open DriverTool.Library.F
    open DriverTool.Library
    let logger = getLoggerByName("SdpUpdates")

    /// <summary>
    /// Load sdp files
    /// </summary>
    /// <param name="sdpFiles"></param>
    let loadSdps sdpFiles =
        result
            {
                let! sdps =
                    sdpFiles
                    |>Seq.map sdpeval.fsharp.Sdp.loadSdpFromFile                        
                    |>toAccumulatedResult
                return sdps
            }

    /// <summary>
    /// Find sdp file name and package info that corresponds to each other
    /// </summary>
    /// <param name="packageInfos"></param>
    /// <param name="sdpFilePath"></param>
    let sdpFileFilter packageInfos sdpFilePath =
        let sdpfileName = FileOperations.toFileName sdpFilePath
        packageInfos
        |>Array.tryFind(fun p -> p.PackageXmlName.ToLowerInvariant() = sdpfileName.ToLowerInvariant())
        |>optionToBoolean

    /// <summary>
    /// Copy sdp file to the download cache. The sdp file will later be copied from the download cache to the final driver package.
    /// </summary>
    /// <param name="cachedFolderPath"></param>
    /// <param name="sourceFilePath"></param>
    let copyFileToDownloadCacheDirectoryPath cachedFolderPath sourceFilePath =
        result
            {                
                let fileName = FileOperations.toFileName sourceFilePath
                let! destinationFilePath = PathOperations.combinePaths2 cachedFolderPath fileName
                let! copyResult = FileOperations.copyFile true sourceFilePath destinationFilePath
                return copyResult
            }
     
    /// <summary>
    /// Copy sdp files to download cache. The sdp files will later be copied from the download cache to the final driver package.
    /// </summary>
    /// <param name="cachedFolderPath"></param>
    /// <param name="packageInfos"></param>
    /// <param name="sourceFilePaths"></param>
    let copySdpFilesToDownloadCache cachedFolderPath packageInfos sourceFilePaths =
        sourceFilePaths
            |>Seq.filter (sdpFileFilter packageInfos)
            |>Seq.map (copyFileToDownloadCacheDirectoryPath cachedFolderPath)
            |>toAccumulatedResult

    /// <summary>
    /// Evaluate applicability rule
    /// </summary>
    /// <param name="applicabilityRule"></param>
    /// <param name="defaultValue"></param>
    let evaluateSdpApplicabilityRule (applicabilityRule:ApplicabilityRule option) defaultValue =
        match applicabilityRule with
        |Some r ->             
            sdpeval.Sdp.EvaluateApplicabilityXmlWithLogging logger r
        |None -> defaultValue


    /// <summary>
    /// Filter on update that are installable and also allready installed locally
    /// </summary>
    /// <param name="sdp"></param>
    let localUpdatesFilter sdp =
        sdp.InstallableItems
        |>Seq.tryFind(fun ii ->                 
                if(logger.IsDebugEnabled) then ( logger.Debug(sprintf "Evaluating 'IsInstalled' applicability rule for sdp '%s' " sdp.PackageId) )
                let isInstalled =  evaluateSdpApplicabilityRule ii.ApplicabilityRules.IsInstalled false
                isInstalled
            )
        |> optionToBoolean


    /// <summary>
    /// Filter on update that are installable
    /// </summary>
    /// <param name="sdp"></param>
    let remoteUpdatesFilter sdp =
        sdp.InstallableItems
        |>Seq.tryFind(fun ii -> 
                if(logger.IsDebugEnabled) then ( logger.Debug(sprintf "Evaluating 'IsInstallable' applicability rule for sdp '%s' " sdp.PackageId))
                let isInstallable = evaluateSdpApplicabilityRule ii.ApplicabilityRules.IsInstallable false                
                isInstallable
            )
        |> optionToBoolean

    /// <summary>
    /// Signal exclution of package if any of the exclude patterns match
    /// </summary>
    /// <param name="excludeUpdateRegexPatterns"></param>
    /// <param name="packageInfo"></param>
    let packageExcludeFilter excludeUpdateRegexPatterns (packageInfo:PackageInfo) =
        let exclude =
            (not (RegExp.matchAny excludeUpdateRegexPatterns packageInfo.Category)) 
                &&                             
            (not (RegExp.matchAny excludeUpdateRegexPatterns packageInfo.Title))
        exclude

    /// <summary>
    /// Convert Sdps (SoftwareDistributionPackage's) to PackagInfo's
    /// </summary>
    /// <param name="excludeUpdateRegexPatterns"></param>
    /// <param name="toPackageInfos"></param>
    /// <param name="sdps"></param>
    let sdpsToPacakgeInfos excludeUpdateRegexPatterns toPackageInfos (sdps:seq<SoftwareDistributionPackage>) : PackageInfo[] =
        let packageInfos =
            sdps
            |>Seq.map toPackageInfos                
            |>Seq.concat
            |>Seq.filter (packageExcludeFilter excludeUpdateRegexPatterns)
            |>Seq.toArray
        packageInfos
    
    /// <summary>
    /// Validate model and operating system. On HP and Dell only creating driver package for the current system is supported as SDP processing is dependent on live query of the underlying hardware.
    /// </summary>
    /// <param name="modelCode"></param>
    /// <param name="operatingSystemCode"></param>
    let validateModelAndOs (modelCode: ModelCode) (operatingSystemCode: OperatingSystemCode) =
        result{
            let! currentModelCode = ModelCode.create "" true
            let!supportedModelCode = 
                if(modelCode.Value <> currentModelCode.Value) then
                    Result.Error (new Exception(sprintf "DriverTool can only create driver package for the current model '%s' as filtering is done with live queries on current system." currentModelCode.Value))
                else
                    Result.Ok currentModelCode
            let! currentOperatingSystem = OperatingSystemCode.create "" true            
            let!supporteOperatingSystemCode = 
                if(operatingSystemCode.Value <> currentOperatingSystem.Value) then
                    Result.Error (new Exception(sprintf "DriverTool can only create driver package for the running operating system '%s' as filtering is done with live queries on current system." currentOperatingSystem.Value))
                else
                    Result.Ok currentOperatingSystem
            return (supportedModelCode, supporteOperatingSystemCode)
        }

    /// <summary>
    /// Exapand SMS Sdp cab file
    /// </summary>
    /// <param name="cabFilePath"></param>
    /// <param name="destinationFolderPath"></param>
    let expandSmsSdpCabFile (cabFilePath:FileSystem.Path, destinationFolderPath:FileSystem.Path) =
        result{
            let! expandExePath = FileSystem.path DriverTool.Library.Cab.expandExe
            let arguments = sprintf "\"%s\" -F:* \"%s\"" (FileSystem.pathValue cabFilePath) (FileSystem.pathValue destinationFolderPath)
            let workingDirectory =  FileSystem.pathValue destinationFolderPath
            let! expandExitCode = ProcessOperations.startConsoleProcess (expandExePath, arguments, workingDirectory,-1,null,null,false)
            let! expandResult = DriverTool.Library.Cab.expandExeExitCodeToResult cabFilePath expandExitCode
            return expandResult
        }

    /// <summary>
    /// Installer data to installer name (execurtable name)
    /// </summary>
    /// <param name="installerData"></param>
    let toInstallerName installerData = 
        match installerData with
        |CommandLineInstallerData d -> d.Program
        |MsiInstallerData d -> d.MsiFile
    
    /// <summary>
    /// Installer data to installer command line arguments
    /// </summary>
    /// <param name="installerData"></param>
    let toInstallerCommandLine installerData = 
        match installerData with
        |CommandLineInstallerData d -> sprintf "\"%s\" %s" (toInstallerName installerData) d.Arguments
        |MsiInstallerData d -> (sprintf "\"%s\" /i \"%s\" /quiet /qn /norestart %s" "msiexec.exe" d.MsiFile d.CommandLine)
