namespace DriverTool.Tests

open NUnit.Framework

[<TestFixture>]
[<Category(TestCategory.UnitTests)>]
module CsvOperationTests =    
    open DriverTool.Library.FileOperations    
    open DriverTool.Library.PathOperations
    open DriverTool.Library.DirectoryOperations
    open DriverTool.Library.F
    open DriverTool.Library
    
    let logger = Common.Logging.Simple.ConsoleOutLogger("CsvOperationTests",Common.Logging.LogLevel.All,true,true,true,"yyyy-MM-dd-HH-mm-ss-ms")

    type CsvData =
        {
            Col1: string
            Col2: string
        }

    [<Test>]
    let csvExportTests () =
        match(result{
            let expectedCsvContent =
                    [
                        "Col1;Col2"
                        "L1v1;L1v2"
                        "L2v1;L2v2"
                        "\"L3; v1\";L3v2"
                        ""
                    ]|> String.concat System.Environment.NewLine
            let records = [|{Col1="L1v1";Col2="L1v2"};{Col1="L2v1";Col2="L2v2"};{Col1="L3; v1";Col2="L3v2"}|]
            use temporaryFolder = new TemporaryFolder(logger)            
            let! temporaryFolderPath = temporaryFolder.FolderPath
            let! temporaryFilePath = combine2Paths ((FileSystem.pathValue temporaryFolderPath),getRandomFileName())
            let! outputCsvPath = CsvOperations.exportToCsv (temporaryFilePath,records)
            let! actualCsvContent = readContentFromFile outputCsvPath
            Assert.AreEqual(expectedCsvContent,actualCsvContent,"Csv file content is not as expected")
            return actualCsvContent
        })with
        |Result.Ok v -> Assert.IsTrue(true)
        |Result.Error ex -> Assert.Fail(ex.Message)

        
        

        ()