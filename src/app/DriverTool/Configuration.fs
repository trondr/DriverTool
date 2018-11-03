namespace DriverTool

open System
open System.Configuration
open System.Collections.Specialized
open FSharp.Configuration

module Configuration =
    
    let private sectionName = "DriverTool"

    let private section = 
        ConfigurationManager.GetSection(sectionName) :?> NameValueCollection
    
    let private getExpandedValue (valueName :string) =
        let value = section.[valueName]
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
    
    let getDownloadCacheDirectoryPath =
        let expandedPath = getExpandedValue "DownloadCacheDirectoryPath"
        let path = System.IO.Path.GetFullPath(expandedPath)
        path

    type Settings = AppSettings<"App.config">

    let getAppConfigFilePath = 
        Settings.ConfigFileName
        