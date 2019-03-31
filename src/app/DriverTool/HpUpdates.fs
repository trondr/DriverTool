namespace DriverTool

module HpUpdates =
    open System
    open PackageXml
    open DriverTool.Web
    open DriverTool.HpCatalog
    open System.Xml.Linq
    open DriverTool.SdpCatalog
    open DriverTool.SdpUpdates
    open FileSystem
    open FSharp.Data
    open DriverTool.UpdatesContext
        
    let toPackageInfos sdp  =
            let pacakgeInfos =
                sdp.InstallableItems
                |>Seq.map(fun ii ->
                           let originFile = ii.OriginFile
                           {
                                Name = sdp.PackageId;
                                Title = sdp.Title;
                                Version = "";
                                BaseUrl = Web.getFolderNameFromUrl originFile.OriginUri
                                InstallerName = toInstallerName ii.InstallerData
                                InstallerCrc = (Checksum.base64StringToFileHash originFile.Digest)|>Checksum.fileHashToString
                                InstallerSize = originFile.Size
                                ExtractCommandLine = ""
                                InstallCommandLine = toInstallerCommandLine ii.InstallerData
                                Category = sdp.ProductName
                                ReadmeName = Web.getFileNameFromUrl sdp.MoreInfoUrl;
                                ReadmeCrc = "";
                                ReadmeSize=0L;
                                ReleaseDate= (getSdpReleaseDate sdp)|>toDateString
                                PackageXmlName=sdp.PackageId + ".sdp";
                           }
                    )
                |>Seq.toArray
            pacakgeInfos

    let downloadSdpFiles () =
        result
            {
                let! hpCatalogForSmsLatest = HpCatalog.downloadSmsSdpCatalog()
                let! hpCatalogForSmsLatestV2 = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue hpCatalogForSmsLatest,"V2"))
                let! existingHpCatalogForSmsLatestV2 = DirectoryOperations.ensureDirectoryExists false hpCatalogForSmsLatestV2
                let! sdpFiles = DirectoryOperations.findFiles false "*.sdp" existingHpCatalogForSmsLatestV2
                return sdpFiles
            }

    let getLocalUpdates (context:UpdatesRetrievalContext) =
        result{
            let! supported = validateModelAndOs context.Model context.OperatingSystem
            let! sdpFiles = downloadSdpFiles()
            let! sdps = loadSdps sdpFiles
            let packageInfos = 
                sdps                
                |>Seq.filter localUpdatesFilter
                |>Seq.toArray
                |>(sdpsToPacakgeInfos context toPackageInfos)
            let! copyResult =  copySdpFilesToDownloadCache packageInfos sdpFiles            
            return packageInfos
        }        

    let getRemoteUpdates (context:UpdatesRetrievalContext) =
        result{
            let! supported = validateModelAndOs context.Model context.OperatingSystem
            let! sdpFiles = downloadSdpFiles()
            let! sdps = loadSdps sdpFiles
            let packageInfos = 
                sdps                
                |>Seq.filter remoteUpdatesFilter
                |>Seq.toArray
                |>(sdpsToPacakgeInfos context toPackageInfos)
            
            let! copyResult =  copySdpFilesToDownloadCache packageInfos sdpFiles
            return packageInfos
        }

    let getSccmDriverPackageInfo (modelCode: ModelCode, operatingSystemCode:OperatingSystemCode) =
        result{
            let! driverPackCatalogXmlFilePath = downloadDriverPackCatalog()
            let! sccmPackageInfo = getSccmDriverPackageInfoBase (driverPackCatalogXmlFilePath, modelCode, operatingSystemCode)
            return sccmPackageInfo
        }

    let downloadSccmPackage (cacheDirectory, sccmPackage:SccmPackageInfo) =
        result{                        
            let! installerdestinationFilePath = FileSystem.path (System.IO.Path.Combine(cacheDirectory,sccmPackage.InstallerFileName))
            let! installerUri = DriverTool.Web.toUri sccmPackage.InstallerUrl
            let installerDownloadInfo = { SourceUri = installerUri;SourceChecksum = sccmPackage.InstallerChecksum;SourceFileSize = 0L;DestinationFile = installerdestinationFilePath}
            let! installerInfo = Web.downloadIfDifferent (installerDownloadInfo,false)
            let installerPath = FileSystem.pathValue installerInfo.DestinationFile

            return {
                InstallerPath = installerPath
                ReadmePath = String.Empty
                SccmPackage = sccmPackage;
            }
        } 

    let extractSccmPackage (downloadedSccmPackage:DownloadedSccmPackageInfo, destinationPath:FileSystem.Path) =
        logger.Info("Extract Sccm Driver Package ...")
        match(result{
            let! installerPath = FileSystem.path downloadedSccmPackage.InstallerPath
            let! exeFilepath = FileOperations.ensureFileExtension ".exe" installerPath
            let! existingExeFilePath = FileOperations.ensureFileExists exeFilepath  
            let arguments = sprintf "-PDF -F \"%s\" -S -E" (FileSystem.pathValue destinationPath)
            let! exitCode = ProcessOperations.startConsoleProcess (installerPath,arguments,FileSystem.pathValue destinationPath,-1,null,null,false)
            return destinationPath
        }) with
        |Ok _ -> Result.Ok destinationPath
        |Error ex -> Result.Error (new Exception("Failed to extract Sccm package. " + ex.Message, ex))

    let toReleaseId downloadedPackageInfo =
        sprintf "%s-%s" (downloadedPackageInfo.Package.InstallerName.Replace(".exe","")) downloadedPackageInfo.Package.ReleaseDate

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

    let getCategoryFromReadmeHtml readmeHtmlPath = 
        result
            {
                let! htmlDocument = HtmlHelper.loadHtmlDocument readmeHtmlPath
                let category =
                    (htmlDocument.Elements()|>Seq.head).Elements()
                    |>Seq.filter(fun l -> 
                            l.InnerText().StartsWith("CATEGORY:")
                        )
                    |>Seq.toArray
                    |>Array.head
                return category.InnerText().Replace("CATEGORY:","").Trim()
            }
        
    /// <summary>
    /// HP. Get and update category info from the update readme html file as the "category" from the SDPs product name ('Driver') is not giving info about type of driver.
    /// The readme html "CATEGORY:" line gives more information of the type of driver. Example: 'Driver-Network' or 'Driver-Keyboard,Mouse and Input Devices'
    /// </summary>
    /// <param name="downloadedUpdates"></param>
    let updateDownloadedPackageInfo (downloadedUpdates:seq<DownloadedPackageInfo>) =
        result
            {
                let! updatedUpdates = 
                    downloadedUpdates
                    |>Seq.map(fun d ->
                                result
                                    {
                                        let! readmeHtmlPath = FileSystem.path d.ReadmePath
                                        let! category = getCategoryFromReadmeHtml readmeHtmlPath
                                        let up = {d.Package with Category = category}
                                        let ud = {d with Package = up}
                                        return ud
                                    }
                            )
                    |>toAccumulatedResult
                return updatedUpdates
            }