namespace DriverTool

open System
open System.Configuration
open System.Collections.Specialized
open FSharp.Configuration

module Configuration =
    
    let private sectionName = "DriverTool"

    let private section = 
        ConfigurationManager.GetSection(sectionName) :?> NameValueCollection
    
    let private getValue (valueName :string) =
        section.[valueName]
    
    let private getExpandedValue (valueName :string) =
        let value = getValue valueName
        let expandedValue = Environment.ExpandEnvironmentVariables(value)
        expandedValue

    let getLogDirectoryPath =         
        let expandedLogDirectoryPath = getExpandedValue "LogDirectoryPath"
        let logDirectoryPath = System.IO.Path.GetFullPath(expandedLogDirectoryPath)
        logDirectoryPath

    let getLogFileName =        
        let expandedLogDirectoryPath = getExpandedValue "LogFileName"
        expandedLogDirectoryPath

    let getLogFilePath =
        let logFilePath = System.IO.Path.Combine(getLogDirectoryPath, getLogFileName)
        logFilePath
    
    let getDownloadCacheDirectoryPathUnsafe =
        let expandedPath = getExpandedValue "DownloadCacheDirectoryPath"
        let path = System.IO.Path.GetFullPath(expandedPath)
        System.IO.Directory.CreateDirectory(path) |> ignore
        path

    let downloadCacheDirectoryPath =
        try
            getDownloadCacheDirectoryPathUnsafe
        with
        | _ as ex -> 
            printfn "Failed to get download cache directory due to: %s. Using TEMP path instead." ex.Message
            System.IO.Path.GetTempPath()

    let getDownloadCacheFilePath fileName = 
        System.IO.Path.Combine(downloadCacheDirectoryPath , fileName)

    type Settings = AppSettings<"App.config">

    let getAppConfigFilePath = 
        AppDomain.CurrentDomain.SetupInformation.ConfigurationFile
    
    let getDriverPackageLogDirectoryPath =
        getValue "DriverPackageLogDirectoryPath"