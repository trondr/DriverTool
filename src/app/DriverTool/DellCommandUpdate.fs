namespace DriverTool

module DellCommandUpdate =
    open System
    open PackageXml
    open System.Xml.Linq
    open DellSettings

    let dellCommandUpdateProgramDataFolder =
        System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData),"Dell","CommandUpdate")

    let dellCommandUpdateActityLogFile = 
        System.IO.Path.Combine(dellCommandUpdateProgramDataFolder,"ActivityLog.xml")

    let getElement (xElement:XElement, elementName:string) =
        xElement
            .Element(XName.Get(elementName))            
    
    let getElementValue (xElement:XElement, elementName:string) =
        match (getElement (xElement, elementName)) with
        |null -> None
        |v -> Some v.Value

    let getDownloadedFilesBase (activityLogPath:Path) =
        result{
            let! existingActivityLogPath = FileOperations.ensureFileExistsWithMessage (sprintf "Dell Activity log '%s' not found. Please install and run Dell Command|Update." activityLogPath.Value) activityLogPath
            let actitivyLogXDocument = XDocument.Load(existingActivityLogPath.Value)                        
            let logEntries = actitivyLogXDocument.Descendants(XName.Get("LogEntry"))
            let downloadedFiles = 
                logEntries
                |>Seq.filter (fun le -> 
                                let message = getElementValue (le,"message")
                                match message with
                                |None -> false
                                |Some m -> 
                                    m = "Download Execution Complete"
                            )
                |>Seq.map (fun le -> 
                            let data = getElement (le,"data")
                            match data with
                            |null -> None
                            |_ -> 
                                let file = getElementValue (data, "File")
                                file
                          )
                |>Seq.choose (fun f -> f)
                |>Seq.filter(fun f -> 
                                f.StartsWith(downloadsHost)                                
                            )
                |>Seq.map(fun f -> f.Replace(downloadsHost,downloadsBaseUrl))
                |>Seq.toArray
            return downloadedFiles
        }
        
    let getDownloadedFiles () =
        result{
            let! activityLogPath = Path.create dellCommandUpdateActityLogFile            
            let! downloadedFiles = getDownloadedFilesBase activityLogPath
            return downloadedFiles
        }
    
    /// <summary>
    /// Split file url in base url and file name and return result as tuple. Example: http://downloads.dell.com/FOLDER05171783M/1/ASMedia-USB-Extended-Host-Controller-Driver_JCDN0_WIN_1.16.54.1_A10.EXE ->  ("http://downloads.dell.com/FOLDER05171783M/1","ASMedia-USB-Extended-Host-Controller-Driver_JCDN0_WIN_1.16.54.1_A10.EXE")
    /// </summary>
    /// <param name="fileUrl">Example: http://downloads.dell.com/FOLDER05171783M/1/ASMedia-USB-Extended-Host-Controller-Driver_JCDN0_WIN_1.16.54.1_A10.EXE</param>    
    let fileUrlToBaseUrlAndFileName fileUrl = 
        let uri = new Uri(fileUrl)
        let fileName = uri.Segments.[uri.Segments.Length-1]
        let baseUrl = fileUrl.Replace(fileName,"").Trim([|'/'|])
        (baseUrl,fileName)
        

    let getLocalUpdatesBase (downloadedFiles:string[]) (remoteUpdates:PackageInfo[]) =
        result{
            let localUpdates =
                remoteUpdates
                |>Seq.filter(fun p -> 
                                downloadedFiles
                                |>Seq.tryFind(fun df -> 
                                    let (baseUrl,fileName) = fileUrlToBaseUrlAndFileName df
                                    (p.BaseUrl = baseUrl) && (p.InstallerName = fileName)
                                )|>optionToBoolean
                            )
                |>Seq.toArray
           return localUpdates        
        }
        
    let getLocalUpdates (modelCode:ModelCode, operatingSystemCode:OperatingSystemCode, remoteUpdates:PackageInfo[]) =
        result{
            let! downloadedFiles = getDownloadedFiles ()            
            let! localUpdates = getLocalUpdatesBase downloadedFiles remoteUpdates
            return localUpdates
        }
    