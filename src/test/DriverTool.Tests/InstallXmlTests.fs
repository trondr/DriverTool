namespace DriverTool.Tests

    open NUnit.Framework
    open DriverTool
    

    [<TestFixture>]
    module InstallXmlTests =         
        open DriverTool.InstallXml
        type ThisAssembly = { Empty:string;}

        type internal TestData ={IsSuccess:bool;Expected:InstallConfigurationData;ExpectedErrorMessage:string}
        
        let internal testData = 
            [|
                yield 
                    {
                        IsSuccess=true
                        Expected=
                            {
                                LogDirectory = "%public%\Logs"
                                LogFileName = "MyCompany_Lenovo_ThinkPad_P50_20EQ0022MN_WIN10X64_Drivers_2019-03-20.log"
                                PackageName = "MyCompany Lenovo ThinkPad P50 20EQ0022MN WIN10X64 Drivers 2019-03-20"
                                PackageVersion = "1.0"
                                PackageRevision = "000"
                                Publisher = "MyCompany"
                                ComputerVendor = "Lenovo"
                                ComputerModel = "20EQ0022MN"
                                ComputerSystemFamiliy = "ThinkPad P50"
                                OsShortName = "WIN10X64"                    
                            }
                        ExpectedErrorMessage="N/A"
                }
            
            |]

        [<Test>]
        [<Category(TestCategory.UnitTests)>]
        [<TestCaseSource("testData")>]
        let loadInstallXmlTest (testDataObject:obj) =
            let testData = (testDataObject:?>TestData)
            match(result {              
                let! tempDestinationFolderPath = FileSystem.path (PathOperations.getTempPath)
                let! installXmlPath = EmbeddedResource.extractEmbeddedResouceByFileNameBase ("InstallXmlInstall.xml", tempDestinationFolderPath,"InstallXmlInstall.xml",typeof<ThisAssembly>.Assembly)
                let! existingInstallXmlPath = DriverTool.FileOperations.ensureFileExists installXmlPath
                let! installConfiguration = loadInstallXml existingInstallXmlPath
                return installConfiguration
            }) with
            |Ok actual -> 
                Assert.IsTrue(true,sprintf "Success: %A" actual)                                
                Assert.AreEqual(testData.Expected,actual,"Install configuration not equal.")
            |Error ex ->
                Assert.IsFalse(true,sprintf "Expected success but failed instead: %s" ex.Message)   

