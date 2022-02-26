namespace DriverTool

open System.Globalization

module HpUpdates =
    open System
    open DriverTool.Library.PackageXml
    open DriverTool.Library.Web
    open DriverTool.HpCatalog
    open sdpeval.fsharp.Sdp
    open DriverTool.SdpUpdates
    open FSharp.Data
    open DriverTool.Library.UpdatesContext
    open DriverTool.Library.F
    open DriverTool.Library
    open DriverTool.Library.DriverPack
    open DriverTool.Library.Logging
        
    let toPackageInfos sdp  =
            let pacakgeInfos =
                sdp.InstallableItems
                |>Seq.map(fun ii ->
                           let originFile = ii.OriginFile
                           {
                                Name = sdp.PackageId;
                                Title = sdp.Title;
                                Version = "";
                                Installer =
                                    {
                                        Url = toOptionalUri originFile.OriginUri ""
                                        Name = toInstallerName ii.InstallerData
                                        Checksum = (Checksum.base64StringToFileHash originFile.Digest)|>Checksum.fileHashToString
                                        Size = originFile.Size
                                        Type = Installer
                                    }                                
                                ExtractCommandLine = ""
                                InstallCommandLine = toInstallerCommandLine ii.InstallerData
                                Category = sdp.ProductName
                                Readme =
                                    {
                                        Url = toOptionalUri sdp.MoreInfoUrl ""
                                        Name = Web.getFileNameFromUrl sdp.MoreInfoUrl
                                        Checksum = ""
                                        Size = 0L
                                        Type = Readme
                                    }
                                ReleaseDate= (getSdpReleaseDate sdp)|>toDateString
                                PackageXmlName=sdp.PackageId + ".sdp";
                                ExternalFiles = None
                           }
                    )
                |>Seq.toArray
            pacakgeInfos

    let downloadSdpFiles cacheFolderPath =
        result
            {
                let! hpCatalogForSmsLatest = HpCatalog.downloadSmsSdpCatalog cacheFolderPath
                let! hpCatalogForSmsLatestV2 = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue hpCatalogForSmsLatest,"V2"))
                let! existingHpCatalogForSmsLatestV2 = DirectoryOperations.ensureDirectoryExists false hpCatalogForSmsLatestV2
                let! sdpFiles = DirectoryOperations.findFiles false "*.sdp" existingHpCatalogForSmsLatestV2
                return sdpFiles
                            |> Seq.map (fun p -> FileSystem.pathValue p)
                            |> Seq.toArray
            }

    let getLocalUpdates logger cacheFolderPath (context:UpdatesRetrievalContext) =
        result{
            let! supported = validateModelAndOs context.Model context.OperatingSystem
            let! sdpFiles = downloadSdpFiles cacheFolderPath
            let! sdps = loadSdps sdpFiles
            let packageInfos = 
                sdps                
                |>Seq.filter localUpdatesFilter
                |>Seq.toArray
                |>(sdpsToPacakgeInfos context.ExcludeUpdateRegexPatterns toPackageInfos)
            let! sdpFilePaths = sdpFiles |> Array.map (fun p -> FileSystem.path p) |> toAccumulatedResult
            let! copyResult =  copySdpFilesToDownloadCache cacheFolderPath packageInfos sdpFilePaths            
            return packageInfos
        }        

    let getRemoteUpdates logger cacheFolderPath (context:UpdatesRetrievalContext) =
        result{
            let! supported = validateModelAndOs context.Model context.OperatingSystem
            let! sdpFiles = downloadSdpFiles cacheFolderPath
            let! sdps = loadSdps sdpFiles
            let packageInfos = 
                sdps                
                |>Seq.filter remoteUpdatesFilter
                |>Seq.toArray
                |>(sdpsToPacakgeInfos context.ExcludeUpdateRegexPatterns toPackageInfos)
            let! sdpFilePaths = sdpFiles |> Array.map (fun p -> FileSystem.path p) |> toAccumulatedResult
            let! copyResult =  copySdpFilesToDownloadCache cacheFolderPath packageInfos sdpFilePaths
            return packageInfos
        }

    let getDriverUpdates reportProgress cacheFolderPath (model:ModelCode) (operatingSystem:OperatingSystemCode) excludeUpdateRegexPatterns =
        result{
            let! supported = validateModelAndOs model operatingSystem
            let! sdpFiles = downloadSdpFiles cacheFolderPath
            let! sdps = loadSdps sdpFiles
            let packageInfos = 
                sdps                
                |>Seq.filter remoteUpdatesFilter
                |>Seq.toArray
                |>(sdpsToPacakgeInfos excludeUpdateRegexPatterns toPackageInfos)
            let! sdpFilePaths = sdpFiles |> Array.map (fun p -> FileSystem.path p) |> toAccumulatedResult
            let! copyResult =  copySdpFilesToDownloadCache cacheFolderPath packageInfos sdpFilePaths
            return packageInfos
        }

    let getSccmDriverPackageInfo (modelCode: ModelCode, operatingSystemCode:OperatingSystemCode, cacheFolderPath:FileSystem.Path, reportProgress) =
        result{
            let! driverPackCatalogXmlFilePath = downloadDriverPackCatalog cacheFolderPath reportProgress
            let osBuild = OperatingSystem.getOsBuildForCurrentSystem
            let! sccmPackageInfo = getSccmDriverPackageInfoBase (driverPackCatalogXmlFilePath, modelCode, operatingSystemCode, osBuild)
            return sccmPackageInfo
        }

    ///Parse DriverPackage xml element to DriverPackInfo
    let toDriverPackInfo (softpacs:SoftPaq[]) (softpackIdByProduct:string*ProductOSDriverPack[])  : Result<DriverPackInfo,Exception> =
        result{            
            let! driverPack =
                let (softPackId,products) = softpackIdByProduct
                match(softpacs|>Array.tryFind(fun sp -> sp.Id = softPackId))with
                |Some p ->                     
                    let (osCode,osBuild)= HpCatalog.hpOsNameToOsCodeAndOsBuild products.[0].OSName
                    let modelCodes = products|>Array.map(fun dp -> dp.SystemId.Split([|','|]) )|>Array.concat |> Array.map(fun s -> s.Trim())
                    let modelName = products|>Array.map(fun dp -> dp.SystemName) |>Array.distinct |> String.concat ", "
                    Result.Ok                             
                            {
                                Manufacturer= "HP"
                                Model=modelName
                                ModelCodes= modelCodes
                                ReadmeFile = 
                                    Some {
                                        Url = p.ReleaseNotesUrl
                                        Checksum=""
                                        FileName=Web.getFileNameFromUrl p.ReleaseNotesUrl
                                        Size=0L
                                    }
                                InstallerFile =
                                    {
                                        Url=p.Url;
                                        Checksum=p.MD5;
                                        FileName=Web.getFileNameFromUrl p.Url
                                        Size=p.Size;
                                    }
                                Released=p.DateReleased|> toHPReleaseDateTime
                                Os= osCode
                                OsBuild=osBuild
                                ModelWmiQuery=toModelCodesWqlQuery modelName (ManufacturerTypes.Manufacturer.HP "HP") modelCodes
                                ManufacturerWmiQuery=toManufacturerWqlQuery (ManufacturerTypes.Manufacturer.HP "HP")
                            }
                |None -> Result.Error (toException (sprintf "Failed to find HP sccm driver package. Found os driver product but failed to find softpaq for model '%s' and operating system '%s' and os build '%s'" products.[0].SystemName products.[0].OSName "*") None)
            return driverPack
        }
        
    ///Get CM package infos for all HP models
    let getDriverPackInfos (cacheFolderPath:FileSystem.Path) (reportProgress:reportProgressFunction) : Result<DriverPackInfo[],Exception> =
        logger.Info("Loading HP Sccm Packages...")
        result{
            let! catalogPath = downloadDriverPackCatalog cacheFolderPath reportProgress
            let! (softpacs,products) = loadCatalog catalogPath
            let! driverPackInfos = 
                products
                |> Array.filter (fun p -> isSupportedOs p.OSName)
                |> Array.groupBy(fun p -> p.SoftPaqId)
                |> Array.map (fun g -> toDriverPackInfo softpacs g)
                |> toAccumulatedResult
            logger.Warn("TODO: HP: Calculate WmiQuery from model codes.")
            logger.Info("Finished loading HP Sccm Packages!")
            return driverPackInfos |> Seq.toArray
        }

    let downloadSccmPackage (cacheFolderPath, sccmPackage:SccmPackageInfo) =
        result{                                    
            let! installerInfo = Web.downloadWebFile logger reportProgressStdOut' cacheFolderPath sccmPackage.InstallerFile
            let installerPath = FileSystem.pathValue installerInfo.DestinationFile
            return {
                InstallerPath = installerPath
                ReadmePath = String.Empty
                SccmPackage = sccmPackage;
            }
        } 

    let extractSccmPackage (downloadedSccmPackage:DownloadedSccmPackageInfo, destinationPath:FileSystem.Path) =
        logger.Info("Extracting Sccm Driver Package ...")
        result{
            let! installerPath = FileSystem.path downloadedSccmPackage.InstallerPath
            let! exeFilepath = FileOperations.ensureFileExtension ".exe" installerPath
            let! existingExeFilePath = FileOperations.ensureFileExists exeFilepath  
            let! existingDestinationPath = DirectoryOperations.ensureDirectoryExists true destinationPath
            let arguments = sprintf "-PDF -F \"%s\" -S -E" (FileSystem.pathValue destinationPath)
            let! exitCode = ProcessOperations.startConsoleProcess (installerPath,arguments,FileSystem.pathValue existingDestinationPath,-1,null,null,false)
            let! exitCode2 =
                match exitCode with
                |0 -> Result.Ok 0
                |_ ->
                    let arguments = sprintf "/s /e /f \"%s\"" (FileSystem.pathValue destinationPath)
                    ProcessOperations.startConsoleProcess (installerPath,arguments,FileSystem.pathValue existingDestinationPath,-1,null,null,false)                
            let! copiedReadmeFilePath = FileOperations.copyFileIfExists downloadedSccmPackage.ReadmePath destinationPath
            return (destinationPath,copiedReadmeFilePath)
        }

    let extractDriverPackInfo (downloadedDriverPackInfo:DownloadedDriverPackInfo) (destinationPath:FileSystem.Path) =
        logger.Info("Extracting CM Driver Package ...")
        result{
            let! installerPath = FileSystem.path downloadedDriverPackInfo.InstallerPath
            let! exeFilepath = FileOperations.ensureFileExtension ".exe" installerPath
            let! existingExeFilePath = FileOperations.ensureFileExists exeFilepath  
            let! existingDestinationPath = DirectoryOperations.ensureDirectoryExists true destinationPath
            //There are two different kinds of CM packages for HP. Trying first one set of command line arguments, if that fails try the other.
            let arguments1 = sprintf "-PDF -F \"%s\" -S -E" (FileSystem.pathValue destinationPath)
            let arguments2 = sprintf "/s /e /f \"%s\"" (FileSystem.pathValue destinationPath)
            let! exitCode = 
                ProcessOperations.startConsoleProcess' (installerPath,arguments1,FileSystem.pathValue existingDestinationPath,-1,null,null,false,[|0|])
                |>ProcessOperations.onError ProcessOperations.startConsoleProcess' (installerPath,arguments2,FileSystem.pathValue existingDestinationPath,-1,null,null,false,[|0;1168|])
            let! copiedReadmeFilePath = FileOperations.copyFileIfExists' downloadedDriverPackInfo.ReadmePath destinationPath            
            return (destinationPath)
        }

    let toReleaseId downloadedPackageInfo =
        sprintf "%s-%s" (downloadedPackageInfo.Package.Installer.Name.Replace(".exe","")) downloadedPackageInfo.Package.ReleaseDate

    let extractUpdate (rootDirectory:FileSystem.Path, (prefix,downloadedPackageInfo:DownloadedPackageInfo)) =
        result{
            let packageFolderName = getPackageFolderName downloadedPackageInfo.Package.Category (toReleaseId downloadedPackageInfo)
            let! packageFolderPath = DriverTool.Library.PathOperations.combine2Paths (FileSystem.pathValue rootDirectory, prefix + "_" + packageFolderName)
            let! existingPackageFolderPath = DirectoryOperations.ensureDirectoryExistsAndIsEmpty (packageFolderPath, true)            
            let extractInstallerResult = extractInstaller (downloadedPackageInfo, existingPackageFolderPath)
            let extractReadmeResult = extractReadme (downloadedPackageInfo, existingPackageFolderPath)
            let extractPacakgeXmlResult = extractPackageXml (downloadedPackageInfo,existingPackageFolderPath)
            let! result = 
                [|extractInstallerResult;extractReadmeResult;extractPacakgeXmlResult|]
                |> toAccumulatedResult
            return result |>Seq.toArray |> Seq.head
        }

    let getCategoryFromReadmeHtml readmeHtmlPath defaultCategory = 
        match(result{
                let! htmlDocument = HtmlHelper.loadHtmlDocument readmeHtmlPath
                let category =
                    (htmlDocument.Elements()|>Seq.head).Elements()
                    |>Seq.filter(fun l ->
                            let innerText = l.InnerText()
                            innerText.StartsWith("CATEGORY:")
                        )
                    |>Seq.toArray
                    |>Array.tryHead                
                return 
                    match category with
                    |None -> defaultCategory
                    |Some n -> n.InnerText().Replace("CATEGORY:","").Trim()
            })
            with
            |Result.Ok c -> c
            |Result.Error _ -> defaultCategory
        
    /// <summary>
    /// HP. Get and update category info from the update readme html file as the "category" from the SDPs product name ('Driver') is not giving info about type of driver.
    /// The readme html "CATEGORY:" line gives more information of the type of driver. Example: 'Driver-Network' or 'Driver-Keyboard,Mouse and Input Devices'
    /// </summary>
    /// <param name="downloadedUpdates"></param>
    let updateDownloadedPackageInfo (downloadedUpdates:array<DownloadedPackageInfo>) =
        result
            {
                let! updatedUpdates = 
                    downloadedUpdates                                        
                    |>Array.map(fun d ->
                                result
                                    {
                                        let! readmeHtmlPath = FileSystem.path d.ReadmePath
                                        let category = getCategoryFromReadmeHtml readmeHtmlPath d.Package.Category
                                        let up = {d.Package with Category = category}
                                        let ud = {d with Package = up}
                                        return ud
                                    }
                            )
                    |>toAccumulatedResult
                let updatedUpdatesArray = updatedUpdates |> Seq.toArray
                return (updatedUpdatesArray |> Array.sortBy (fun dp -> packageInfoSortKey dp.Package))
            }

    //Check if driver update is required on the current system.
    let isDriverUpdateRequired (cacheFolderPath:FileSystem.Path) (packageInfo:PackageInfo) (allPackageInfos:PackageInfo[]) : Result<bool,Exception> =
        result {
            let! sdpFilePath = DriverTool.Library.PathOperations.combinePaths2 cacheFolderPath packageInfo.PackageXmlName
            let! sdp = sdpeval.fsharp.Sdp.loadSdpFromFile (FileSystem.pathValue sdpFilePath)
            let isApplicable = DriverTool.SdpUpdates.localUpdatesFilter sdp
            let isInstalled = DriverTool.SdpUpdates.remoteUpdatesFilter sdp
            return (isApplicable && (not isInstalled))
        }