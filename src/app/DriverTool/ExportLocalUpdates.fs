namespace DriverTool


module ExportLocalUpdates =
    let logger = Logging.getLoggerByName "ExportLocalUpdates"
    open DriverTool.PackageXml

    let updateFromRemote (remotePackageInfos:seq<PackageInfo>) (packageInfos:seq<PackageInfo>) =
        let updatedPacageInfos = 
            packageInfos
            |>Seq.map(fun p -> 
                        let remotePackageInfo = 
                            remotePackageInfos
                            |> Seq.tryFind(fun rp -> rp.InstallerName = p.InstallerName)
                        let updatedPackageInfo =
                            match remotePackageInfo with
                            |Some rp ->                         
                                {p with Category=rp.Category;BaseUrl=rp.BaseUrl} 
                            |None -> p
                        updatedPackageInfo
                        )
        updatedPacageInfos
        
    open System
    
    let exportLocalUpdates (csvFilePath:Path) =
        result{
            logger.Info("Checking if Lenovo System Update is installed...")
            let! lenovoSystemUpdateIsInstalled = DriverTool.LenovoSystemUpdateCheck.ensureLenovoSystemUpdateIsInstalled ()
            logger.Info("Lenovo System Update is installed: " + lenovoSystemUpdateIsInstalled.ToString())
            logger.Info("Getting locally installed updates...")
            let! packageInfos = DriverTool.LenovoSystemUpdate.getLocalUpdates()
            let! modelCode = ModelCode.create String.Empty true
            let! operatingSystemCode = OperatingSystemCode.create String.Empty true
            let! remotePackageInfos = ExportRemoteUpdates.getRemoteUpdates (modelCode, operatingSystemCode, true)
            let! exportResult = 
                packageInfos
                |> getUnique
                |> updateFromRemote remotePackageInfos
                |> DriverTool.ExportRemoteUpdates.exportToCsv csvFilePath
            logger.Info("Locally installed updated has been exported to file: " + csvFilePath.Value)
            return exportResult            
        }

