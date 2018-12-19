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

    let assertThatModelCodeIsValid (model:ModelCode) (actualModel:ModelCode) =
        if(actualModel.Value.StartsWith(model.Value)) then
            Result.Ok true
        else
            Result.Error (new Exception(String.Format("Given model '{0}' and actual model '{1}' are not equal.",model.Value,actualModel.Value)))
    
    let asserThatOperatingSystemCodeIsValid (operatingSystemCode:OperatingSystemCode) (actualOperatingSystemCode:OperatingSystemCode) =
        if(operatingSystemCode.Value = actualOperatingSystemCode.Value) then
            Result.Ok true
        else
            Result.Error (new Exception(String.Format("Given operating system code '{0}' and actual operating system code '{1}' are not equal.",operatingSystemCode.Value,actualOperatingSystemCode.Value)))

    let getLocalUpdates (modelCode: ModelCode, operatingSystemCode: OperatingSystemCode, overwrite) =
        result{
            logger.Info("Checking if Lenovo System Update is installed...")
            let! lenovoSystemUpdateIsInstalled = DriverTool.LenovoSystemUpdateCheck.ensureLenovoSystemUpdateIsInstalled ()
            logger.Info("Lenovo System Update is installed: " + lenovoSystemUpdateIsInstalled.ToString())
            logger.Info("Getting locally installed updates...")
            let! packageInfos = DriverTool.LenovoSystemUpdate.getLocalUpdates()
            
            let! actualModelCode = ModelCode.create String.Empty true
            let! modelCodeIsValid = assertThatModelCodeIsValid modelCode actualModelCode
            logger.Info(String.Format("Model code '{0}' is valid: {1}",modelCode.Value,modelCodeIsValid))
            let! actualOperatingSystemCode = OperatingSystemCode.create String.Empty true
            let! operatingSystemCodeIsValid = asserThatOperatingSystemCodeIsValid operatingSystemCode actualOperatingSystemCode
            logger.Info(String.Format("Operating system code '{0}' is valid: {1}",operatingSystemCode.Value,operatingSystemCodeIsValid))

            let! remotePackageInfos = ExportRemoteUpdates.getRemoteUpdates (modelCode, operatingSystemCode, overwrite)
            let localUpdates = 
                packageInfos
                |> getUnique
                |> updateFromRemote remotePackageInfos                
            return localUpdates
        }
        
    open System
    
    let exportLocalUpdates (csvFilePath:Path) =
        result{       
            let! actualModelCode = ModelCode.create String.Empty true
            let! actualOperatingSystemCode = OperatingSystemCode.create String.Empty true
            let! localUpdates = getLocalUpdates (actualModelCode, actualOperatingSystemCode, true)
            let! exportResult = 
                localUpdates
                |> DriverTool.ExportRemoteUpdates.exportToCsv csvFilePath
            logger.Info("Locally installed updates have been exported to file: " + csvFilePath.Value)
            return exportResult            
        }

