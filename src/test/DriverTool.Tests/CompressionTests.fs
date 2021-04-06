namespace DriverTool.Tests

open NUnit.Framework

[<TestFixture>]
[<Category(TestCategory.UnitTests)>]
module CompressionTests=
    open DriverTool    
    open DriverTool.Library.FileOperations
    open DriverTool.Library.F
    open DriverTool.Library
        
    let logger = Common.Logging.Simple.ConsoleOutLogger("CompressionTests",Common.Logging.LogLevel.All,true,true,true,"yyyy-MM-dd-HH-mm-ss-ms")
    
    let rnd = System.Random()

    let getRandomCount =        
        rnd.Next (1,20)

    let genRandomNumbers count =
        let rnd = System.Random()
        List.init count (fun _ -> rnd.Next (1,getRandomCount))
    
    let createNRandomFiles folderPath fileCount =
        result{
            let listOfFiles =
                List.init fileCount (fun _ -> createRandomFile logger folderPath)
                |>List.map(fun fp -> resultToOption logger fp)
                |>List.choose (fun fp -> fp)                                
            return listOfFiles
        }

    let createRandomDirectory parentFolderPath =
        result {
            let! existingFolderPath = DirectoryOperations.ensureDirectoryExists false parentFolderPath
            let! randomDirectoryPath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue existingFolderPath, System.IO.Path.GetFileNameWithoutExtension(System.IO.Path.GetRandomFileName())))
            let! directoryPath = DirectoryOperations.createDirectory randomDirectoryPath
            let! randomFile = createNRandomFiles directoryPath getRandomCount
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
        FileOperations.writeContentToFile logger filePath (System.Guid.NewGuid().ToString())

    let createTestFolder shouldExist folderPath =
        result {
            let! preparedTestSourceFolderPath =    
                match shouldExist with
                |false ->
                    DirectoryOperations.deleteDirectory true folderPath |> ignore
                    Result.Ok folderPath
                |true ->
                    DirectoryOperations.deleteDirectory true folderPath |> ignore
                    createRandomFolderAndFileStructure folderPath
            return preparedTestSourceFolderPath             
        }
    
    let getTestZipFilePath zipFileExists temporaryFolderPath =
        result{
            let! testZipFile = FileSystem.path (System.IO.Path.Combine((FileSystem.pathValue temporaryFolderPath),"CompressionTests_SourceFolder.zip"))
            let! preparedTestZipFile =
                match zipFileExists with
                |true -> createTestFile testZipFile
                |false-> FileOperations.deleteFile testZipFile
            return preparedTestZipFile
        }
    
    [<Test>]
    let temporaryFolderTest () =
        match(result{
            use temporaryFolder = new DirectoryOperations.TemporaryFolder(logger)
            let! temporaryFolderPath = temporaryFolder.FolderPath
            let! testSourceFolderPath = createTestFolder true temporaryFolderPath

            use zipFileTemporaryFolder = new DirectoryOperations.TemporaryFolder(logger)
            let! ziptFileTemporaryFolderPath = zipFileTemporaryFolder.FolderPath
            let! testZipFile = getTestZipFilePath false ziptFileTemporaryFolderPath                                       
            
            let! actual = Compression.zipFolder (testSourceFolderPath, testZipFile, logger)
            Assert.AreEqual(true, FileOperations.fileExists testZipFile,"Zip file does not exist after zipFolder: " + FileSystem.pathValue testZipFile)
            use temporaryDestinationFolder = new DirectoryOperations.TemporaryFolder(logger)
            let! temporaryDestinationFolderPath = temporaryDestinationFolder.FolderPath

            let! unzipResult = Compression.unzipFile (testZipFile, temporaryDestinationFolderPath, logger)
            let! existingSourceFolderPath = FileSystem.existingDirectoryPath (FileSystem.pathValue testSourceFolderPath)
            let! existingDestinationFolderPath = FileSystem.existingDirectoryPath (FileSystem.pathValue temporaryDestinationFolderPath)
            logger.Debug(sprintf "ExistingDestinationFolderPath: %A" existingDestinationFolderPath)

            Assert.AreEqual(true,FileOperations.compareDirectory existingSourceFolderPath existingDestinationFolderPath,"Source and destination folder compare.")
            return testSourceFolderPath
        })with
        |Result.Ok v -> 
            Assert.IsTrue(true, "Should allways be ok.")
            ()
        |Result.Error ex -> 
            Assert.Fail("Should allways never fail.")
            ()
                    
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
                    use sourceTemporaryFolder = new DirectoryOperations.TemporaryFolder(logger)
                    let! sourceTemporaryFolderPath = sourceTemporaryFolder.FolderPath
                    let! testSourceFolderPath = createTestFolder sourceFolderExists sourceTemporaryFolderPath
                    Assert.AreEqual(sourceFolderExists, DirectoryOperations.folderPathExists testSourceFolderPath,"Expected source folder existance zipFolder: " + sourceFolderExists.ToString())
                    
                    use zipFileTemporaryFolder = new DirectoryOperations.TemporaryFolder(logger)
                    let! ziptFileTemporaryFolderPath = zipFileTemporaryFolder.FolderPath
                    let! testZipFile = getTestZipFilePath zipFileExists ziptFileTemporaryFolderPath                                       
                    Assert.AreEqual(zipFileExists, FileOperations.fileExists testZipFile,"Expected zip file existance before zipFolder: " + zipFileExists.ToString())

                    let! actual = Compression.zipFolder (testSourceFolderPath, testZipFile, logger)
                    Assert.AreEqual(true, FileOperations.fileExists testZipFile,"Zip file does not exist after zipFolder: " + FileSystem.pathValue testZipFile)
                    use temporaryDestinationFolder = new DirectoryOperations.TemporaryFolder(logger)
                    let! temporaryDestinationFolderPath = temporaryDestinationFolder.FolderPath
                    
                    let! unzipResult = Compression.unzipFile (testZipFile, temporaryDestinationFolderPath, logger)
                    let! existingSourceFolderPath = FileSystem.existingDirectoryPath (FileSystem.pathValue testSourceFolderPath)
                    let! existingDestinationFolderPath = FileSystem.existingDirectoryPath (FileSystem.pathValue temporaryDestinationFolderPath)
                    logger.Debug(sprintf "ExistingDestinationFolderPath: %A" existingDestinationFolderPath)
                    let deletedOrChangedFile () =
                        if(not sourceAndDestiationAreEqual) then                                                        
                            let filePath =
                                existingDestinationFolderPath                                
                                |> DirectoryOperations.getFilesUnsafe true
                                |> Seq.head
                            if(changeContentOfAFile) then
                                //Change the first file in the destination, to force an inequality
                                logger.Debug(sprintf "Changing content of file: '%s' (TID: %i)" filePath System.Threading.Thread.CurrentThread.ManagedThreadId)
                                FileOperations.writeContentToFile logger (FileSystem.pathUnSafe filePath) "Some change"|>ignore
                                logger.Debug(sprintf "Finished changing content of file: '%s' (TID: %i)" filePath System.Threading.Thread.CurrentThread.ManagedThreadId)
                            else                         
                                //Delete the first file in the destination, to force an inequality
                                Assert.IsTrue(System.IO.File.Exists(filePath),"File does not exists: " + filePath)
                                logger.Debug(sprintf "Delete content of file: '%s'" filePath)
                                System.IO.File.Delete(filePath)|>ignore
                                Assert.IsFalse(System.IO.File.Exists(filePath),"File exists:" + filePath)
                        else
                            ()                 
                    deletedOrChangedFile()                    
                    Assert.AreEqual(sourceAndDestiationAreEqual,FileOperations.compareDirectory existingSourceFolderPath existingDestinationFolderPath,"Source and destination folder compare.")                    
                    logger.Debug(sprintf "Returning: %A (TID: %i)" actual System.Threading.Thread.CurrentThread.ManagedThreadId)                    
                    return actual
                }
        match res with
        |Ok v -> Assert.True(expectedSuccess, "Expected failed but succeeded instead.")
        |Error ex -> 
            Assert.False(expectedSuccess,"Expected success but failed instead")
            Assert.IsTrue(ex.Message.Contains(expectedErrorMessage),"Error message not as expected: " + ex.Message)


         