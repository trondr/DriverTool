namespace DriverTool.Tests

    open NUnit.Framework

    [<TestFixture>]
    [<Category(TestCategory.UnitTests)>]
    module CryptographyTests =
        open DriverTool
        open DriverTool
        open Init
                
        [<Test>]
        [<TestCase("n20ku13w_x64.exe",true)>]
        [<TestCase("n20ku13w_x64_tampered.exe",false)>]
        let isTrustedTests (fileName:string, expected:bool) =
            match (result
                        {
                            let! destinationFolderPath = FileSystem.path (System.IO.Path.GetTempPath())
                            let assembly = typeof<ThisTestAssembly>.Assembly
                            let! extractedFilePath = EmbeddedResource.extractEmbeddedResouceByFileNameBase (fileName,destinationFolderPath,fileName,assembly)                
                            let actual = Cryptography.isTrusted extractedFilePath
                            Assert.AreEqual(expected,actual)
                            return extractedFilePath
                        }) with
             |Ok r -> Assert.IsTrue(true)
             |Error ex -> Assert.Fail(ex.Message)

            
            

