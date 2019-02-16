namespace DriverTool.Tests

open NUnit.Framework

[<TestFixture>]
[<Category(TestCategory.UnitTests)>]
module CompressionTests=
    open DriverTool
    open log4net
    open DriverTool
        
    let logger = LogManager.GetLogger("CompressionTests")
    
    let genRandomNumbers count =
        let rnd = System.Random()
        List.init count (fun _ -> rnd.Next (1,100))
    
    let createRandomTestFile folderPath =
        result {
            let! existingFolderPath = DirectoryOperations.ensureDirectoryExists false folderPath
            let! randomFilePath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue existingFolderPath, System.IO.Path.GetRandomFileName()))
            let! writeResult = FileOperations.writeContentToFile randomFilePath (System.Guid.NewGuid().ToString())
            return randomFilePath
        }

    let createRandomDirectory parentFolderPath =
        result {
            let! existingFolderPath = DirectoryOperations.ensureDirectoryExists false parentFolderPath
            let! randomDirectoryPath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue existingFolderPath, System.IO.Path.GetRandomFileName()))
            let! directoryPath = DirectoryOperations.createDirectory randomDirectoryPath
            let! randomFile = createRandomTestFile directoryPath
            return directoryPath
        }
    
    let createNRandomDirectories parentFolderPath directoryCount =
        result{
            let! listOfDirectories = 
                List.init directoryCount (fun _ -> createRandomDirectory parentFolderPath)
                |>List.map(fun dp -> resultToOption logger dp)
                |>List.choose(fun dp -> dp)
                |>List.map DirectoryOperations.createDirectory
                |>toAccumulatedResult                
            return listOfDirectories|>Seq.toList
        }

    let createRandomFolderAndFileStructure folderPath =
        result{
            let! existingFolderPath = DirectoryOperations.createDirectory folderPath
            let listOfDirectoryCounts = genRandomNumbers 10
            let! listOfDirectories =
                listOfDirectoryCounts
                |>List.map (fun d-> createNRandomDirectories existingFolderPath d)
                |>toAccumulatedResult                
            return existingFolderPath
        }

    let createTestFile filePath =
        FileOperations.writeContentToFile filePath (System.Guid.NewGuid().ToString())

    let createTestFolder shouldExist folderName =
        result{
            let! testSourceFolderPath = FileSystem.path (System.IO.Path.Combine(System.IO.Path.GetTempPath(), folderName))
            let! preparedTestSourceFolderPath =    
                match shouldExist with
                |false ->
                    DirectoryOperations.deleteDirectory true testSourceFolderPath |> ignore
                    Result.Ok testSourceFolderPath
                |true ->
                    DirectoryOperations.deleteDirectory true testSourceFolderPath |> ignore
                    createRandomFolderAndFileStructure testSourceFolderPath
            return preparedTestSourceFolderPath
        }

    let getTestZipFilePath zipFileExists =
        result{
            let! testZipFile = FileSystem.path (System.IO.Path.Combine(System.IO.Path.GetTempPath(),"CompressionTests_SourceFolder.zip"))                    
            let! preparedTestZipFile =
                match zipFileExists with
                |true -> createTestFile testZipFile
                |false-> FileOperations.deleteFile testZipFile
            return preparedTestZipFile
        }
        
        
    [<Test>]
    [<TestCase(true,false,true,"<No Error>",false,true,false,"Test1")>]
    [<TestCase(true,true,false,"Zip file allready exists",false,true,false,"Test2")>]
    [<TestCase(false,false,false,"Cannot zip down a non existing directory",false,true,false,"Test3")>]
    [<TestCase(false,true,false,"Zip file allready exists",false,true,false,"Test4")>]    
    [<TestCase(true,false,true,"<No Error>",false,false,false,"Test5")>]
    [<TestCase(true,false,true,"<No Error>",false,false,true,"Test6")>]    
    let zipFolderTest (sourceFolderExists:bool, zipFileExists:bool, expectedSuccess:bool, expectedErrorMessage, destinationFolderExists:bool, sourceAndDestiationAreEqual:bool, changeContentOfAFile:bool, testName) =
        let res =
            result{
                    let! testSourceFolderPath = createTestFolder sourceFolderExists ("CompressionTests_SourceFolder" + testName)
                    Assert.AreEqual(sourceFolderExists, DirectoryOperations.directoryPathExists testSourceFolderPath,"Expected source folder existance zipFolder: " + sourceFolderExists.ToString())
                    let! testZipFile = getTestZipFilePath zipFileExists                    
                    Assert.AreEqual(zipFileExists, FileOperations.fileExists testZipFile,"Expected zip file existance before zipFolder: " + zipFileExists.ToString())
                    let! actual = Compression.zipFolder (testSourceFolderPath, testZipFile)
                    Assert.AreEqual(true, FileOperations.fileExists testZipFile,"Zip file does not exist after zipFolder: " + FileSystem.pathValue testZipFile)
                    let! testDestinationFolderPath = createTestFolder destinationFolderExists ("CompressionTests_DestinationFolder" + testName)
                    let! unzipResult = Compression.unzipFile (testZipFile, testDestinationFolderPath)
                    let! existingSourceFolderPath = FileSystem.existingDirectoryPath (FileSystem.pathValue testSourceFolderPath)
                    let! existingDestinationFolderPath = FileSystem.existingDirectoryPath (FileSystem.pathValue testDestinationFolderPath)
                    let deletedOrChangedFile =
                        if(not sourceAndDestiationAreEqual) then                                                        
                            let filePath =
                                existingDestinationFolderPath
                                |> FileSystem.existingDirectoryPathValueToPath
                                |> DirectoryOperations.getFilesUnsafe true
                                |> Array.head
                            if(changeContentOfAFile) then
                                //Change the first file in the destination, to force an inequality
                                FileOperations.writeContentToFile (FileSystem.pathUnSafe filePath) "Some change"|>ignore
                            else                         
                                //Delete the first file in the destination, to force an inequality
                                Assert.IsTrue(System.IO.File.Exists(filePath),"File does not exists: " + filePath)
                                System.IO.File.Delete(filePath)|>ignore
                                Assert.IsFalse(System.IO.File.Exists(filePath),"File exists:" + filePath)
                        else
                            ()
                        
                    Assert.AreEqual(sourceAndDestiationAreEqual,FileOperations.compareDirectory existingSourceFolderPath existingDestinationFolderPath,"Source and destination folder compare.")
                    return actual
                }
        match res with
        |Ok v -> Assert.True(expectedSuccess, "Expected failed but succeeded instead.")
        |Error ex -> 
            Assert.False(expectedSuccess,"Expected success but failed instead")
            Assert.IsTrue(ex.Message.Contains(expectedErrorMessage),"Error message not as expected: " + ex.Message)


         