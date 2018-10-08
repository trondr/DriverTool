namespace DriverTool.Tests
open NUnit.Framework
open DriverTool.Checksum

[<TestFixture>]
module ChecksumTests =
    open DriverTool
    open System
    
    [<Test>]
    [<TestCase("File hash should be the same",true,1234,1234,@"c:\temp\somedestinationfile.txt","ABCDE12345ABCDE12345ABCDE12345ABCDE12345","ABCDE12345ABCDE12345ABCDE12345ABCDE12345",true,true,true,true)>]
    [<TestCase("File hash should not be the same",true,1234,1234,@"c:\temp\somedestinationfile.txt","ABCDE12345ABCDE12345ABCDE12345ABCDE12345","99CDE12345ABCDE12345ABCDE12345ABCDE12399",false,true,true,true)>]
    [<TestCase("File hash should not be the same due file size difference.",true,0,1234,@"c:\temp\somedestinationfile.txt","NOTUSED","NOTUSED",false,true,true,false)>]
    [<TestCase("File hash should not be the same due destinaton file not exists.",false,1234,1234,@"c:\temp\somedestinationfile.txt","NOTUSED","NOTUSED",false,true,false,false)>]
    
    let hasSameFileHashPartial (testDescription:string) (destinationFileExists:Boolean) (destinationFileSize:Int64) (sourceFileSize:Int64) (destinationFilePath:string) (sourceFileHash:string) (destinatinationFileHash:string)  (expected:Boolean) (callFileExists:Boolean) (callgetFileSize:Boolean) (callComputeFileHashFromHashLength:Boolean) =
        //Setup
        let fileExistsStub = (fun f ->
            Assert.IsTrue(callFileExists,"FileExists should not have been called")
            destinationFileExists
            )
        let getFileSizeStub = (fun f ->
            Assert.IsTrue(callFileExists,"getFileSize should not have been called")
            destinationFileSize
            )
        
        let computeFileHashFromHashLengthStub = (fun f l ->
            Assert.IsTrue(callComputeFileHashFromHashLength,"getFileSize should not have been called")
            destinatinationFileHash
            )
        
        //Execute
        let target =
            hasSameFileHashPartial fileExistsStub getFileSizeStub computeFileHashFromHashLengthStub
        let actual = target (destinationFilePath, sourceFileHash, sourceFileSize)
        
        //Assert
        Assert.AreEqual(expected,actual,testDescription)



