namespace DriverTool.Tests
open NUnit.Framework
open System
open DriverTool

[<TestFixture>]
module WebParsingTests  =    
   
    open DriverTool.Library.F  
    open System.Threading
    open DriverTool.WebParsing
    open DriverTool.PathOperations
    open DriverTool.FileOperations
    let logger = Common.Logging.Simple.ConsoleOutLogger("WebParsingTests",Common.Logging.LogLevel.All,true,true,true,"yyyy-MM-dd-HH-mm-ss-ms")

    [<Test>]
    [<Category(TestCategory.IntegrationTests)>]
    [<TestCase("https://support.lenovo.com/no/en/downloads/ds112090")>]    
    [<Apartment(ApartmentState.STA)>]
    let getContentFromWebPagetest (webPageUrl:string) =
        let testResult = 
            result{
                let! content = getContentFromWebPage webPageUrl
                let! filePath = FileSystem.path (getTempFile "ds112090.html")
                content |> (writeContentToFile logger filePath)|> ignore
                Assert.IsFalse(String.IsNullOrWhiteSpace(content))
                return content
            }
        match testResult with
        |Ok v -> Assert.IsTrue(true)
        |Error ex -> Assert.Fail(ex.Message)
        
    