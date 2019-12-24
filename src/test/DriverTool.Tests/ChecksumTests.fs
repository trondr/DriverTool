namespace DriverTool.Tests
open NUnit.Framework

[<TestFixture>]
module ChecksumTests =    
    open System
    open DriverTool.Checksum
    open DriverTool.Library
    
    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    [<TestCase("File hash should be the same",true,1234L,1234L,@"c:\temp\somedestinationfile.txt","ABCDE12345ABCDE12345ABCDE12345ABCDE12345","ABCDE12345ABCDE12345ABCDE12345ABCDE12345",true,true,true,true)>]
    [<TestCase("File hash should not be the same",true,1234L,1234L,@"c:\temp\somedestinationfile.txt","ABCDE12345ABCDE12345ABCDE12345ABCDE12345","99CDE12345ABCDE12345ABCDE12345ABCDE12399",false,true,true,true)>]
    [<TestCase("File hash should not be the same due file size difference.",true,0L,1234L,@"c:\temp\somedestinationfile.txt","NOTUSED","NOTUSED",false,true,true,false)>]
    [<TestCase("File hash should not be the same due destinaton file not exists.",false,1234L,1234L,@"c:\temp\somedestinationfile.txt","NOTUSED","NOTUSED",false,true,false,false)>]

    [<TestCase("File hash should be the same if source hash is empty.",true,1234,1234,@"c:\temp\somedestinationfile.txt","","NOTUSED",true,true,true,true)>]
    [<TestCase("File hash should be the same if source hash is empty and source size is 0.",true,1234L,0L,@"c:\temp\somedestinationfile.txt","","NOTUSED",true,true,true,true)>]
    
    let hasSameFileHashPartialTest (testDescription:string) (destinationFileExists:Boolean) (destinationFileSize:Int64) (sourceFileSize:Int64) (destinationFilePath:string) (sourceFileHash:string) (destinatinationFileHash:string)  (expected:Boolean) (callFileExists:Boolean) (callgetFileSize:Boolean) (callComputeFileHashFromHashLength:Boolean) =
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
        let actual = target (FileSystem.pathUnSafe destinationFilePath, sourceFileHash, sourceFileSize)
        
        //Assert
        Assert.AreEqual(expected,actual,testDescription)



