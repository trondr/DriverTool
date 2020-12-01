namespace DriverTool.Tests
open NUnit.Framework

[<TestFixture>]
module ChecksumTests =    
    open System
    open DriverTool.Library.Checksum
    open DriverTool.Library
    
    module DestinationFileExists = 
        [<Literal>]
        let True = true
        [<Literal>]
        let False = false

    module Expected = 
        [<Literal>]
        let True = true
        [<Literal>]
        let False = false

    module CallFileExists = 
        [<Literal>]
        let True = true
        [<Literal>]
        let False = false

    module CallGetFileSize = 
        [<Literal>]
        let True = true
        [<Literal>]
        let False = false

    module CallComputeFileHashFromHashLength = 
        [<Literal>]
        let True = true
        [<Literal>]
        let False = false

    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    [<TestCase("File hash should be the same",DestinationFileExists.True,1234L,1234L,@"c:\temp\somedestinationfile.txt","ABCDE12345ABCDE12345ABCDE12345ABCDE12345","ABCDE12345ABCDE12345ABCDE12345ABCDE12345",Expected.True,CallFileExists.True,CallGetFileSize.True,CallComputeFileHashFromHashLength.True)>]
    [<TestCase("File hash should not be the same",DestinationFileExists.True,1234L,1234L,@"c:\temp\somedestinationfile.txt","ABCDE12345ABCDE12345ABCDE12345ABCDE12345","99CDE12345ABCDE12345ABCDE12345ABCDE12399",Expected.False,CallFileExists.True,CallGetFileSize.True,CallComputeFileHashFromHashLength.True)>]
    [<TestCase("File hash should not be the same due file size difference.",DestinationFileExists.True,0L,1234L,@"c:\temp\somedestinationfile.txt","NOTUSED","NOTUSED",Expected.False,CallFileExists.True,CallGetFileSize.True,CallComputeFileHashFromHashLength.False)>]
    [<TestCase("File hash should not be the same due destinaton file not exists.",DestinationFileExists.False,1234L,1234L,@"c:\temp\somedestinationfile.txt","NOTUSED","NOTUSED",Expected.False,CallFileExists.True,CallGetFileSize.False,CallComputeFileHashFromHashLength.False)>]
    [<TestCase("File hash should be the same if source hash is empty.",DestinationFileExists.True,1234,1234,@"c:\temp\somedestinationfile.txt","","NOTUSED",Expected.True,CallFileExists.True,CallGetFileSize.True,CallComputeFileHashFromHashLength.True)>]
    [<TestCase("File hash should be the same if source hash is empty and source size is 0.",DestinationFileExists.True,1234L,0L,@"c:\temp\somedestinationfile.txt","","NOTUSED",Expected.True,CallFileExists.True,CallGetFileSize.True,CallComputeFileHashFromHashLength.True)>]
    
    let hasSameFileHashPartialTest (testDescription:string) (destinationFileExists:Boolean) (destinationFileSize:Int64) (sourceFileSize:Int64) (destinationFilePath:string) (sourceFileHash:string) (destinatinationFileHash:string)  (expected:Boolean) (callFileExists:Boolean) (callgetFileSize:Boolean) (callComputeFileHashFromHashLength:Boolean) =
        //Setup
        let fileExistsStub = (fun f ->
            Assert.IsTrue(callFileExists,"FileExists should not have been called")
            destinationFileExists
            )
        let getFileSizeStub = (fun f ->
            Assert.IsTrue(callgetFileSize,"getFileSize should not have been called")
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



