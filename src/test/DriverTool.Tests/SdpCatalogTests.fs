namespace DriverTool.Tests

open NUnit.Framework


[<TestFixture>]
module SdpCatalogTests =
    open DriverTool
    open System
    type ThisAssembly = { Empty:string;}
    

    [<Test>]
    [<TestCase(null,"N/A",false,"ProductCode cannot be null.")>]
    [<TestCase("","N/A",false,"ProductCode cannot be empty.")>]
    [<TestCase("abc-bcd","N/A",false,"Invalid product code: 'abc-bcd'")>]
    [<TestCase("{FB2B7CC0-8307-47e6-A065-11015CC96B99}","{FB2B7CC0-8307-47e6-A065-11015CC96B99}",true,"N/A")>]
    let productCodeTests (guid:string,expectedGuid:string,isSuccess:bool,expectedErrorMessage:string) =
        match (result {
            let! actual = SdpCatalog.productCode guid
            return actual
        }) with        
        |Ok v -> 
            Assert.True(isSuccess, "Expected failed but succeeded instead.")
            Assert.AreEqual(expectedGuid,(SdpCatalog.productCodeValue v),"Product code value not expected")
        |Error ex -> 
            Assert.False(isSuccess,sprintf "Expected success but failed instead: %s" ex.Message)
            Assert.IsTrue(ex.Message.Contains(expectedErrorMessage),"Error message not as expected: " + ex.Message)

        
    [<Test>]
    [<TestCase("0e6cf4ac-2853-48aa-825b-8fe28206575f.sdp","Realtek High Definition Audio Driver,6.0.1.8454,A02",true,"N/A")>]
    let loadSdpTests(sdpFileName:string,expectedValue:string,isSuccess:bool,expectedErrorMessage:string) =
        match(result {            
            let! tempDestinationFolderPath = FileSystem.path (PathOperations.getTempPath)            
            let! sdpFilePath = EmbeddedResouce.extractEmbeddedResouceByFileNameBase (sdpFileName,tempDestinationFolderPath,sdpFileName,typeof<ThisAssembly>.Assembly)
            let! sdpXDocument = SdpCatalog.loadSdpXDocument sdpFilePath
            let! sdpXElement = SdpCatalog.loadSdpXElement sdpXDocument
            let! actual = SdpCatalog.loadSdp sdpXElement
            return actual
        }) with        
        |Ok v -> 
            Assert.True(isSuccess, "Expected failed but succeeded instead.")
            Assert.AreEqual(expectedValue, v.Title,"Product code value not expected")
        |Error ex -> 
            Assert.False(isSuccess,sprintf "Expected success but failed instead: %s" ex.Message)
            Assert.IsTrue(ex.Message.Contains(expectedErrorMessage),"Error message not as expected: " + ex.Message)
