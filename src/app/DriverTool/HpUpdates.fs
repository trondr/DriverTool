namespace DriverTool

module HpUpdates =
    open System
    open PackageXml
    open DriverTool.Web
    open DriverTool.HpCatalog
    open System.Xml.Linq
    open sdpeval.fsharp.Sdp
    open DriverTool.SdpUpdates
    open FileSystem
    open FSharp.Data
    open DriverTool.UpdatesContext
    open DriverTool.PackageXml
        
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
                |>(sdpsToPacakgeInfos context toPackageInfos)
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
                |>(sdpsToPacakgeInfos context toPackageInfos)
            let! sdpFilePaths = sdpFiles |> Array.map (fun p -> FileSystem.path p) |> toAccumulatedResult
            let! copyResult =  copySdpFilesToDownloadCache cacheFolderPath packageInfos sdpFilePaths
            return packageInfos
        }

    let getSccmDriverPackageInfo (modelCode: ModelCode, operatingSystemCode:OperatingSystemCode, cacheFolderPath:FileSystem.Path) =
        result{
            let! driverPackCatalogXmlFilePath = downloadDriverPackCatalog cacheFolderPath
            let! sccmPackageInfo = getSccmDriverPackageInfoBase (driverPackCatalogXmlFilePath, modelCode, operatingSystemCode)
            return sccmPackageInfo
        }

    let downloadSccmPackage (cacheDirectory, sccmPackage:SccmPackageInfo) =
        result{                        
            let! installerdestinationFilePath = PathOperations.combinePaths2 cacheDirectory sccmPackage.InstallerFileName
            let! installerUri = DriverTool.Web.toUri sccmPackage.InstallerUrl
            let installerDownloadInfo = { SourceUri = installerUri;SourceChecksum = sccmPackage.InstallerChecksum;SourceFileSize = 0L;DestinationFile = installerdestinationFilePath}
            let! installerInfo = Web.downloadIfDifferent (logger,installerDownloadInfo,false)
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
            let arguments = sprintf "-PDF -F \"%s\" -S -E" (FileSystem.pathValue destinationPath)
            let! exitCode = ProcessOperations.startConsoleProcess (installerPath,arguments,FileSystem.pathValue destinationPath,-1,null,null,false)
            
            let! copiedReadmeFilePath = 
                FileOperations.copyFileIfExists downloadedSccmPackage.ReadmePath destinationPath

            return (destinationPath,copiedReadmeFilePath)
        }

    let toReleaseId downloadedPackageInfo =
        sprintf "%s-%s" (downloadedPackageInfo.Package.Installer.Name.Replace(".exe","")) downloadedPackageInfo.Package.ReleaseDate

    let extractUpdate (rootDirectory:FileSystem.Path, (prefix,downloadedPackageInfo:DownloadedPackageInfo)) =
        result{
            let packageFolderName = getPackageFolderName downloadedPackageInfo.Package.Category (toReleaseId downloadedPackageInfo)
            let! packageFolderPath = DriverTool.PathOperations.combine2Paths (FileSystem.pathValue rootDirectory, prefix + "_" + packageFolderName)
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
        result
            {
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
            }
        
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
                                        let! category = getCategoryFromReadmeHtml readmeHtmlPath d.Package.Category
                                        let up = {d.Package with Category = category}
                                        let ud = {d with Package = up}
                                        return ud
                                    }
                            )
                    |>toAccumulatedResult
                let updatedUpdatesArray = updatedUpdates |> Seq.toArray
                return (updatedUpdatesArray |> Array.sortBy (fun dp -> packageInfoSortKey dp.Package))
            }