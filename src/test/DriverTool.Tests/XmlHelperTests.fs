namespace DriverTool.Tests

open NUnit.Framework

[<TestFixture>]
[<Category(TestCategory.UnitTests)>]
module XmlHelperTests=
    open DriverTool.Library
    open DriverTool.Library.XmlHelper
    open DriverTool.Tests.Init

    [<SetUp>]    
    let setup () =
        DriverTool.Library.Logging.configureConsoleLogging()

    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    let loadXDocumentTest_ExitingXmlDocument () =
        match(result{
                use temporaryCacheFolder = new DirectoryOperations.TemporaryFolder(logger)                
                let! temproaryCacheFolderPath = temporaryCacheFolder.FolderPath
                let fileName = "TestDataXmlHelper.xml"
                let! testXmlFile = EmbeddedResource.extractEmbeddedResourceByFileNameBase (fileName,temproaryCacheFolderPath,fileName,typeof<ThisTestAssembly>.Assembly)
                let! existingTestXmlFile = FileOperations.ensureFileExistsWithMessage "Test xml file does not exist" testXmlFile
                let! actual = DriverTool.Library.XmlHelper.loadXDocument existingTestXmlFile
            return actual
        })with
        |Result.Ok a -> Assert.IsTrue(true)
        |Result.Error ex -> Assert.Fail(ex.Message)

    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    let loadXDocumentTest_NonExitingXmlDocument () =
        match(result{
            use temporaryCacheFolder = new DirectoryOperations.TemporaryFolder(logger)                
            let! temproaryCacheFolderPath = temporaryCacheFolder.FolderPath
            let fileName = "TestDataXmlHelperDoesNotExist.xml"
            let! testXmlFile = PathOperations.combinePaths2 temproaryCacheFolderPath fileName
            let! nonExistingTestXmlFile = FileOperations.ensureFileDoesNotExistWithMessage "Test xml exists when it should not." false testXmlFile
            let! actual = DriverTool.Library.XmlHelper.loadXDocument nonExistingTestXmlFile
            return actual
        })with
        |Result.Ok a -> Assert.IsTrue(false,"Succeded but expected to fail")
        |Result.Error ex -> Assert.IsTrue(ex.Message.Contains("Failed to load xml file"),sprintf "Error message not expected: %s" ex.Message)


    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    let getElementValueTest_ExistingValue () =
        match(result{
                use temporaryCacheFolder = new DirectoryOperations.TemporaryFolder(logger)                
                let! temproaryCacheFolderPath = temporaryCacheFolder.FolderPath
                let fileName = "TestDataXmlHelper.xml"
                let! testXmlFile = EmbeddedResource.extractEmbeddedResourceByFileNameBase (fileName,temproaryCacheFolderPath,fileName,typeof<ThisTestAssembly>.Assembly)
                let! existingTestXmlFile = FileOperations.ensureFileExistsWithMessage "Test xml file does not exist" testXmlFile
                let! actual = DriverTool.Library.XmlHelper.loadXDocument existingTestXmlFile
                let! value = DriverTool.Library.XmlHelper.getElementValue actual.Root (xn "LogDirectory")
                Assert.AreEqual("%public%\Logs",value,"Was not able to read LogDirectory element from xml file.")
                return actual
        })with
        |Result.Ok a -> Assert.IsTrue(true)
        |Result.Error ex -> Assert.Fail(ex.Message)

    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    let getElementValueTest_NonExistingValue () =
        match(result{
                use temporaryCacheFolder = new DirectoryOperations.TemporaryFolder(logger)                
                let! temproaryCacheFolderPath = temporaryCacheFolder.FolderPath
                let fileName = "TestDataXmlHelper.xml"
                let! testXmlFile = EmbeddedResource.extractEmbeddedResourceByFileNameBase (fileName,temproaryCacheFolderPath,fileName,typeof<ThisTestAssembly>.Assembly)
                let! existingTestXmlFile = FileOperations.ensureFileExistsWithMessage "Test xml file does not exist" testXmlFile
                let! actual = DriverTool.Library.XmlHelper.loadXDocument existingTestXmlFile
                let! value = DriverTool.Library.XmlHelper.getElementValue actual.Root (xn "LogDirectory2")                
                return actual
        })with
        |Result.Ok a -> Assert.IsTrue(true)
        |Result.Error ex -> Assert.IsTrue(true,ex.Message)

    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    let getElementValueTest_ParentIsNull () =
        match(result{                
                let! actual = DriverTool.Library.XmlHelper.getElementValue null (xn "LogDirectory2")                
                return actual
        })with
        |Result.Ok a -> Assert.IsTrue(true)
        |Result.Error ex -> Assert.IsTrue(true,ex.Message)

    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    let getRequiredAttributeTest_ExistingValue () =
        match(result{
                use temporaryCacheFolder = new DirectoryOperations.TemporaryFolder(logger)                
                let! temproaryCacheFolderPath = temporaryCacheFolder.FolderPath
                let fileName = "TestDataXmlHelper.xml"
                let! testXmlFile = EmbeddedResource.extractEmbeddedResourceByFileNameBase (fileName,temproaryCacheFolderPath,fileName,typeof<ThisTestAssembly>.Assembly)
                let! existingTestXmlFile = FileOperations.ensureFileExistsWithMessage "Test xml file does not exist" testXmlFile
                let! actual = DriverTool.Library.XmlHelper.loadXDocument existingTestXmlFile
                let! value = DriverTool.Library.XmlHelper.getRequiredAttribute (actual.Root.Element(xn "LogDirectory")) (xn "Description")
                Assert.AreEqual("Store logs in this directory",value,"Was not able to read LogDirectory element from xml file.")
                return actual
        })with
        |Result.Ok a -> Assert.IsTrue(true)
        |Result.Error ex -> Assert.Fail(ex.Message)

    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    let getRequiredAttributeTest_NonExistingValue () =
        match(result{
                use temporaryCacheFolder = new DirectoryOperations.TemporaryFolder(logger)                
                let! temproaryCacheFolderPath = temporaryCacheFolder.FolderPath
                let fileName = "TestDataXmlHelper.xml"
                let! testXmlFile = EmbeddedResource.extractEmbeddedResourceByFileNameBase (fileName,temproaryCacheFolderPath,fileName,typeof<ThisTestAssembly>.Assembly)
                let! existingTestXmlFile = FileOperations.ensureFileExistsWithMessage "Test xml file does not exist" testXmlFile
                let! actual = DriverTool.Library.XmlHelper.loadXDocument existingTestXmlFile
                let! value = DriverTool.Library.XmlHelper.getRequiredAttribute (actual.Root.Element(xn "LogDirectory")) (xn "Description2")                
                return actual
        })with
        |Result.Ok a -> Assert.IsTrue(true)
        |Result.Error ex -> Assert.IsTrue(true,ex.Message)

    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    let getRequiredAttributeTest_ParentIsNull () =
        match(result{                
                let! actual = DriverTool.Library.XmlHelper.getRequiredAttribute null (xn "Description")                
                return actual
        })with
        |Result.Ok a -> Assert.IsTrue(true)
        |Result.Error ex -> Assert.IsTrue(true,ex.Message)


    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    let getOptionalAttributeTest_ExistingValue () =
        match(result{
                use temporaryCacheFolder = new DirectoryOperations.TemporaryFolder(logger)                
                let! temproaryCacheFolderPath = temporaryCacheFolder.FolderPath
                let fileName = "TestDataXmlHelper.xml"
                let! testXmlFile = EmbeddedResource.extractEmbeddedResourceByFileNameBase (fileName,temproaryCacheFolderPath,fileName,typeof<ThisTestAssembly>.Assembly)
                let! existingTestXmlFile = FileOperations.ensureFileExistsWithMessage "Test xml file does not exist" testXmlFile
                let! doc = DriverTool.Library.XmlHelper.loadXDocument existingTestXmlFile
                let value = DriverTool.Library.XmlHelper.getOptionalAttribute (doc.Root.Element(xn "LogDirectory")) (xn "Description")
                let! actual =
                    match value with                
                    |Some v ->
                        Assert.AreEqual("Store logs in this directory",v,"Was not able to read LogDirectory element from xml file.")
                        Result.Ok v
                    |None ->
                        Result.Error (toException "Did not expect None." None)
                return actual
        })with
        |Result.Ok a -> Assert.IsTrue(true)
        |Result.Error ex -> Assert.Fail(ex.Message)

    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    let getOptionalAttributeTest_NonExistingValue () =
        match(result{
                use temporaryCacheFolder = new DirectoryOperations.TemporaryFolder(logger)                
                let! temproaryCacheFolderPath = temporaryCacheFolder.FolderPath
                let fileName = "TestDataXmlHelper.xml"
                let! testXmlFile = EmbeddedResource.extractEmbeddedResourceByFileNameBase (fileName,temproaryCacheFolderPath,fileName,typeof<ThisTestAssembly>.Assembly)
                let! existingTestXmlFile = FileOperations.ensureFileExistsWithMessage "Test xml file does not exist" testXmlFile
                let! actual = DriverTool.Library.XmlHelper.loadXDocument existingTestXmlFile
                let value = DriverTool.Library.XmlHelper.getOptionalAttribute (actual.Root.Element(xn "LogDirectory")) (xn "Description2")
                let! actual =
                    match value with                
                    |Some v ->                        
                        Result.Error (toException "Did not expect None." None)
                    |None ->
                        Result.Ok "Returned None as expected"
                return actual
        })with
        |Result.Ok a -> Assert.IsTrue(true)
        |Result.Error ex -> Assert.IsTrue(true,ex.Message)

    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    let getOptionalAttributeTest_ParentIsNull_None () =
        let actual = DriverTool.Library.XmlHelper.getOptionalAttribute null (xn "Description")
        Assert.AreEqual(None,actual,"Did expect None")
        