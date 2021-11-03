namespace DriverTool.Library

open System
open System.Configuration
open System.Collections.Specialized
open FSharp.Configuration
open System.Xml
open System.IO

module Configuration =
    
    let private sectionName = "DriverTool"

    let private getSection() = 
        let section = ConfigurationManager.GetSection(sectionName) :?> NameValueCollection                
        if(section = null) then
            let configFile = System.Reflection.Assembly.GetCallingAssembly().Location+ ".config"
            let configFileMap = new ExeConfigurationFileMap()
            configFileMap.ExeConfigFilename <- configFile
            let config = ConfigurationManager.OpenMappedExeConfiguration(configFileMap,ConfigurationUserLevel.None)
            let configSection = config.GetSection(sectionName)
            let xml = configSection.SectionInformation.GetRawXml()
            let xmlDoc = new XmlDocument()
            use xmlReader = XmlReader.Create(new StringReader(xml))
            xmlDoc.Load(xmlReader)
            let sectionType = configSection.SectionInformation.Type;
            let assemblyName = typeof<IConfigurationSectionHandler>.Assembly.GetName().FullName;
            let configSectionHandlerHandle = Activator.CreateInstance(assemblyName, sectionType);
            if(configSectionHandlerHandle <> null) then
                let handler = configSectionHandlerHandle.Unwrap() :?> IConfigurationSectionHandler
                handler.Create(null,null,xmlDoc.DocumentElement) :?> NameValueCollection
            else
                null
        else
            null
    
    let private getValue (valueName :string) =
        let section = getSection()
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
    
    let getLogLevel =
        let value = getValue "LogLevel"
        value

    let getDownloadCacheDirectoryPath' () =
        let expandedPath = getExpandedValue "DownloadCacheDirectoryPath"
        let path = System.IO.Path.GetFullPath(expandedPath)        
        path

    let getDownloadCacheDirectoryPath () =
        try
            getDownloadCacheDirectoryPath' ()
        with
        | _ as ex -> 
            printfn "Failed to get download cache directory due to: %s. Using TEMP path instead." ex.Message
            System.IO.Path.GetTempPath()

    let getDownloadCacheFilePath cacheFolder fileName = 
        System.IO.Path.Combine(cacheFolder , fileName)

    type Settings = AppSettings<"App.config">

    let getAppConfigFilePath = 
        AppDomain.CurrentDomain.SetupInformation.ConfigurationFile
    
    let getDriverPackageLogDirectoryPath =
        getValue "DriverPackageLogDirectoryPath"

    let getWebProxyUrl =
        getValue "WebProxyUrl"

    let getWebProxyByPassOnLocal =
        let value = getValue "WebProxyByPassOnLocal"
        match value with
        | "True" -> true
        | "False" -> false
        | "true" -> true
        | "false" -> false
        | "1" -> true
        | "0" -> false
        | _ -> false
    
    let getWebProxyByPassList : String[] =
        let value = getValue "WebProxyByPassList"
        match value with
        | "" -> [||]
        | null -> [||]
        | v -> 
            v.Split([|';'|])
            |> Array.filter (fun s -> not (String.IsNullOrWhiteSpace(s)))
