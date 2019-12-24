namespace DriverTool

module HtmlHelper =
    open FSharp.Data
    open DriverTool.Library.F0
    open DriverTool.Library.F
    
    let loadHtmlDocument htmlFilePath =
        result
            {
                let! existinghtmlFilePath = FileOperations.ensureFileExistsWithMessage (sprintf "Html file '%s' not found." (FileSystem.pathValue htmlFilePath)) htmlFilePath
                let! htmlContent = FileOperations.readContentFromFile existinghtmlFilePath
                let htmlStream = stringToStream htmlContent
                return! 
                    try                    
                        Result.Ok (HtmlDocument.Load(htmlStream))
                    with
                    |ex -> 
                        toErrorResult (sprintf "Failed to load html document '%A'." htmlFilePath) (Some ex)                        
            }

