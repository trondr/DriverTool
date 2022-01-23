namespace DriverTool.Library

module DriverUpdates =
    
    ///Load driver updates for manufacturer, model and operating system
    let loadDriverUpdates reportProgress cacheFolderPath manufacturer model operatingSystem excludeUpdateRegexPatterns =
        result{                        
            let driverUpdatesFunction = DriverTool.Updates.getDriverUpdatesFunc manufacturer            
            let! driverPackInfos = driverUpdatesFunction reportProgress cacheFolderPath model operatingSystem excludeUpdateRegexPatterns            
            return driverPackInfos
        }

