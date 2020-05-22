namespace DriverTool.Tests

module DirectoryOperationTests=
    open NUnit.Framework
    open DriverTool.Library.DirectoryOperations
    open DriverTool.Library.F
    open DriverTool.Library.FileSystem
    open DriverTool.Library.PathOperations

    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    let ``moveDirectoryUnsafe target base folder does not exist `` () =
        match(result{
            //Create test source folder
            let! tempSourcePath = path (System.IO.Path.Combine(System.IO.Path.GetTempPath(),System.Guid.NewGuid().ToString(),"source"))
            let! existingTempSourcePath = ensureDirectoryExists true tempSourcePath
            let! tempTargetPathBase = path (System.IO.Path.Combine(System.IO.Path.GetTempPath(),System.Guid.NewGuid().ToString()))
            let! existingTempTargetBase = ensureDirectoryExists true tempTargetPathBase
            let! tempTargetPath = combinePaths2 existingTempTargetBase "target"            
            moveDirectoryUnsafe existingTempSourcePath tempTargetPath
            return tempSourcePath
        })with
        |Result.Ok v ->
            Assert.IsTrue(true)
        |Result.Error ex ->
            Assert.IsTrue(false,ex.Message)        
