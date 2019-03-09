﻿namespace DriverTool.Tests

open NUnit.Framework


[<TestFixture>]
module SdpCatalogTests =
    open DriverTool
    open System
    open DriverTool.SdpCatalog

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

    type internal SdpTestData ={SdpTestFile:string;IsSuccess:bool;Expected:SoftwareDistributionPackage;ExpectedErrorMessage:string}

    let internal sdpTestData =
        [
            yield {SdpTestFile="0e6cf4ac-2853-48aa-825b-8fe28206575f.sdp";IsSuccess=true;Expected={Title="Realtek High Definition Audio Driver,6.0.1.8454,A02";Description="This package contains the driver for Realtek High-Definition audio codec and is supported on Dell products that run the Windows 10 operating system. Audio driver is the software that helps your operating system to communicate effectively with audio devices such as sound cards and speakers. The package supports Windows 10 Fall Creators Update.";ProductName="Drivers and Applications";PackageId="0e6cf4ac-2853-48aa-825b-8fe28206575f";UpdateSpecificData={MsrcSeverity= MsrcSeverity.Important;UpdateClassification=UpdateClassification.Updates;SecurityBulletinID=Some "99DDD";KBArticleID=Some "99DDD"}};ExpectedErrorMessage="N/A"}
        ]

    [<Test>]
    [<TestCaseSource("sdpTestData")>]
    //[<TestCase("0e6cf4ac-2853-48aa-825b-8fe28206575f.sdp","Realtek High Definition Audio Driver,6.0.1.8454,A02",true,"N/A")>]
    let loadSdpTests(sdpTestData:obj) =
        let sdpTestDataR = (sdpTestData:?>SdpTestData)
        match(result {            
            let! tempDestinationFolderPath = FileSystem.path (PathOperations.getTempPath)            
            let! sdpFilePath = EmbeddedResouce.extractEmbeddedResouceByFileNameBase (sdpTestDataR.SdpTestFile,tempDestinationFolderPath,sdpTestDataR.SdpTestFile,typeof<ThisAssembly>.Assembly)
            let! sdpXDocument = SdpCatalog.loadSdpXDocument sdpFilePath
            let! sdpXElement = SdpCatalog.loadSdpXElement sdpXDocument
            let! actual = SdpCatalog.loadSdp sdpXElement
            return actual
        }) with        
        |Ok v -> 
            Assert.True(sdpTestDataR.IsSuccess, "Expected failed but succeeded instead.")
            Assert.AreEqual(sdpTestDataR.Expected.Title, v.Title,"Title")
            Assert.AreEqual(sdpTestDataR.Expected.Description, v.Description,"Description")
            Assert.AreEqual(sdpTestDataR.Expected.ProductName, v.ProductName,"ProductName")
            Assert.AreEqual(sdpTestDataR.Expected.PackageId, v.PackageId,"PackageId")
            Assert.AreEqual(sdpTestDataR.Expected.UpdateSpecificData.KBArticleID, v.UpdateSpecificData.KBArticleID,"KBArticleID")
            Assert.AreEqual(sdpTestDataR.Expected.UpdateSpecificData.SecurityBulletinID, v.UpdateSpecificData.SecurityBulletinID,"SecurityBulletinID")
            Assert.AreEqual(sdpTestDataR.Expected.UpdateSpecificData.UpdateClassification, v.UpdateSpecificData.UpdateClassification,"UpdateClassification")
            Assert.AreEqual(sdpTestDataR.Expected.UpdateSpecificData.MsrcSeverity, v.UpdateSpecificData.MsrcSeverity,"MsrcSeverity")
        |Error ex -> 
            Assert.False(sdpTestDataR.IsSuccess,sprintf "Expected success but failed instead: %s" ex.Message)
            Assert.IsTrue(ex.Message.Contains(sdpTestDataR.ExpectedErrorMessage),"Error message not as expected: " + ex.Message)
