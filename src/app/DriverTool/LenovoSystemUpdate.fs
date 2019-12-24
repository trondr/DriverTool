namespace DriverTool

module LenovoSystemUpdate =
    open System
    open Tvsu.Engine
    open Tvsu.Beans
    open DriverTool.PackageXml
    open DriverTool.Library.F
    
    
    let logger = DriverTool.Library.Logging.getLoggerByName("LenovoSystemUpdate")

    let getLocalUpdatesUnsafe () =
        logger.Info("Getting update info from Lenovo System Update database...");
        let database = DataBase.Instance
        database.LoadUpdateData()
        let installedUpdates = database.GetUpdateWithStatus(UpdateStatus.INSTALL_SUCCESSFUL)
        let packageInfosResult = 
            installedUpdates
            |>listToSequence
            |>Seq.map(fun u ->
                            result{
                                let update =  (u :?> Update)
                                let! updateFilePath = FileSystem.path (System.IO.Path.Combine(update.LocalPath, update.FileName))
                                let downloadedPackageInfo = 
                                    {
                                        Location="" 
                                        Category="Unknown Category" 
                                        FilePath=updateFilePath
                                        BaseUrl=""
                                        CheckSum=DriverTool.Checksum.computeFileHashSha256String updateFilePath
                                    }                            
                                let packageInfo = getPackageInfoUnsafe(downloadedPackageInfo)
                                return packageInfo
                            }                            
                    )
            |>toAccumulatedResult
        let packageInfos = 
            match packageInfosResult with
            |Ok packageInfos-> packageInfos
            |Error ex -> raise (new Exception("Failed to get local update info due to: " + ex.Message, ex))
        database.ShutDownDataBase()
        logger.Info("Finished getting update info from Lenovo System Update database!");
        packageInfos        

    let getLocalUpdates () =
        tryCatch getLocalUpdatesUnsafe ()
        
        