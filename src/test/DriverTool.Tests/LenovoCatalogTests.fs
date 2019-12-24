namespace DriverTool.Tests

open NUnit.Framework
open DriverTool
open DriverTool.Library

[<TestFixture>]
module LenovoCatalogTests=
    
    open DriverTool.LenovoCatalog
    open DriverTool.LenovoUpdates
    open DriverTool.PackageXml
    open Microsoft.FSharp.Core    
    open DriverTool.Web
    open DriverTool.WebParsing
    open DriverTool.Library.FileOperations
    open DriverTool.Library.PathOperations
    open System.Threading    
    
    let logger = Common.Logging.Simple.ConsoleOutLogger("LenovoUpdateTests",Common.Logging.LogLevel.All,true,true,true,"yyyy-MM-dd-HH-mm-ss-ms")

    type ThisAssembly = { Empty:string;}

    [<Test>]
    [<Category(TestCategory.IntegrationTests)>]
    let getSccmPackagesInfoTest () =        
        match(result{
              use cacheFolder = new DirectoryOperations.TemporaryFolder(logger)
              let! cacheFolderPath = cacheFolder.FolderPath
              let actual = getSccmPackageInfos cacheFolderPath
              let res =
                  match actual with
                  |Result.Ok products -> 
                      let productArray = products|>Seq.toArray
                      Assert.IsTrue(productArray.Length > 300,"Expected product array greater than zeror." )
                  |Result.Error ex -> 
                      Assert.Fail("Did not expect getSccmPackagesInfo to fail. Error: " + ex.Message)        
              return cacheFolderPath
        })with
        |Result.Ok v -> Assert.IsTrue(true)
        |Result.Error ex -> Assert.Fail(ex.Message)

    [<Test>]
    [<Category(TestCategory.IntegrationTests)>]
    [<Apartment(ApartmentState.STA)>]
    let getSccmPackageDownloadInfosTest () =
        match(result{
            use cacheFolder = new DirectoryOperations.TemporaryFolder(logger)
            let! cacheFolderPath = cacheFolder.FolderPath
            let sccmPackageInfos = getSccmPackageInfos cacheFolderPath
            let res =
                match sccmPackageInfos with
                |Error ex -> 
                    Assert.Fail("Did not expect to fail. Error:" + ex.Message)
                    //Result.Error (new System.Exception("Did not expect to fail"))
                |Ok products ->
                    let downloadInfosResult = 
                        products
                        |> getNRandomItems 4
                        |> Seq.map (fun p -> 
                                        getLenovoSccmPackageDownloadInfo  p.SccmDriverPackUrl.Value "WIN10X64" "*"
                                        )
                        |> toAccumulatedResult                
                    match downloadInfosResult with
                    |Ok dis -> 
                        dis 
                        |> Seq.map(fun di -> System.Console.WriteLine("{0};{1}",di.InstallerFile.Url,di.InstallerFile.Checksum))
                        |> Seq.iter (fun p -> p)
                        //|> ignore
                    |Error ex -> 
                        Assert.IsTrue(ex.Message.Contains("Sccm package not found"))                
                Assert.IsTrue(true)
            return cacheFolderPath
        })with
        |Result.Ok v -> Assert.IsTrue(true)
        |Result.Error ex -> Assert.Fail(ex.Message)
        
        
        
      
    [<Test>]
    [<Category(TestCategory.IntegrationTests)>]
    let getUniqueLenovoOperatingSystemTest () =
        let result = 
            result
                {
                    use cacheFolder = new DirectoryOperations.TemporaryFolder(logger)
                    let! cacheFolderPath = cacheFolder.FolderPath
                    let! products = getSccmPackageInfos cacheFolderPath
                    let uniqueOses = 
                        products 
                        |> Seq.map(fun p -> p.Os)                        
                        |> Seq.distinct
                        |> Seq.toArray
                    uniqueOses 
                    |> Seq.map(fun os -> System.Console.WriteLine(os))
                    |> Seq.iter(fun os -> os)
                    Assert.AreEqual(4,uniqueOses.Length) |> ignore
                    return products
                }
        result |> ignore
        Assert.IsTrue(true)

    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    [<TestCase("WIN7X64","win764")>]    
    [<TestCase("WIN7X86","win732")>]

    [<TestCase("WIN81X64","win81")>]    
    [<TestCase("WIN81X86","win81")>]

    [<TestCase("WIN10X64","win10")>]    
    [<TestCase("WIN10X86","win10")>]
    let osShortNameToLenovoOsTest_Supported (osShortName, expected:string) =
        let actual = (osShortNameToLenovoOs osShortName)
        Assert.AreEqual(expected, actual)

    [<TestCase("WIN2008X64")>]    
    [<TestCase("WIN2008X86")>]
    [<TestCase("VISTAX64")>]    
    [<TestCase("VISTAX86")>]
    
    [<TestCase("WIN2008R2X64")>]    
    [<TestCase("WIN2008R2X64")>]
    
    [<TestCase("WIN2012X64")>]    
    [<TestCase("WIN2012X64")>]
    [<TestCase("WIN8X64")>]    
    [<TestCase("WIN8X86")>]

    [<TestCase("WIN2012R2X64")>]    
    [<TestCase("WIN2012R2X64")>]
    

    [<TestCase("WIN2016X64")>]    
    [<TestCase("WIN2016X64")>]
    [<Category(TestCategory.UnitTests)>]
    let osShortNameToLenovoOsTest_UnSupported (osShortName) =
        Assert.Throws<System.Exception>(fun () -> (
                                                                osShortNameToLenovoOs osShortName|> ignore                        
                                                                Assert.IsTrue(true)
                                           ))|>ignore
        Assert.IsTrue(true)

    [<Test>]
    [<Category(TestCategory.IntegrationTests)>]
    let findSccmPackageInfoByModelCode4AndOsAndBuildTest () =
        result{
                use cacheFolder = new DirectoryOperations.TemporaryFolder(logger)
                let! cacheFolderPath = cacheFolder.FolderPath
                let! products = getSccmPackageInfos cacheFolderPath
                let randomProducts = 
                    products
                    |>getNRandomItems 10
                randomProducts 
                |> Seq.map (fun p -> 
                                p.ModelCodes
                                |> Seq.map (fun m -> 
                                                let actual = (findSccmPackageInfoByModelCode4AndOsAndBuild logger m p.Os p.OsBuild.Value products)
                                                Assert.IsTrue(actual.IsSome) |> ignore
                                            ) |> ignore
                            )|>ignore

                let actual = findSccmPackageInfoByModelCode4AndOsAndBuild logger "20FA" "win10" "1709" products
                Assert.IsTrue(actual.IsSome,"Did not find model 20FA win10 *") |> ignore
                return ""
        } |> ignore
    
    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    [<TestCase("LenovoCatalog.xml","20L8","win10","1809","1809")>]
    [<TestCase("LenovoCatalog.xml","20L8","win10","*","1809")>]
    [<TestCase("LenovoCatalog.xml","20L6","win10","1809","1809")>]
    [<TestCase("LenovoCatalog.xml","20FA","win10","1709","1709")>]
    [<TestCase("LenovoCatalog.xml","20HJ","win10","1809","*")>]    
    [<TestCase("LenovoCatalog.xml","20HJ","win10","*","*")>]
    
    [<TestCase("LenovoCatalog_v2.xml","20L8","win10","1809","1809")>]
    [<TestCase("LenovoCatalog_v2.xml","20L8","win10","*","1903")>]
    [<TestCase("LenovoCatalog_v2.xml","20L6","win10","1809","1809")>]
    [<TestCase("LenovoCatalog_v2.xml","20FA","win10","1709","1709")>]
    [<TestCase("LenovoCatalog_v2.xml","20HJ","win10","1809","1809")>]    
    [<TestCase("LenovoCatalog_v2.xml","20HJ","win10","*","1903")>]
    [<TestCase("LenovoCatalog_v2.xml","20QG","win10","*","1903")>]
    [<TestCase("LenovoCatalog_v2.xml","20QG","win10","1809","1809")>]
    let findSccmPackageInfoByModelCode4AndOsAndBuildTest2 (fileName:string,modelCode:string,os:string,osBuild:string, expectedOsBuild) =
        match (result{
            use cacheFolder = new DirectoryOperations.TemporaryFolder(logger)
            let! cacheFolderPath = cacheFolder.FolderPath                        
            let! destinationFilePath = EmbeddedResource.extractEmbeddedResourceByFileNameBase (fileName,cacheFolderPath,fileName,System.Reflection.Assembly.GetExecutingAssembly())
            Assert.IsTrue((fileExists destinationFilePath),sprintf "File does not exist: %s" (FileSystem.pathValue destinationFilePath))
            let! lenovoCatalogProducts = DriverTool.LenovoCatalogXml.loadLenovoCatalog destinationFilePath
            let products =
                getSccmPackageInfosFromLenovoCatalogProducts lenovoCatalogProducts
            //let! products = getSccmPackageInfos cacheFolderPath
            let actual = findSccmPackageInfoByModelCode4AndOsAndBuild logger modelCode os osBuild products
            let eval =
                match actual with
                |Some p -> 
                    Assert.AreEqual(os, p.Os,"Os is different")
                    Assert.AreEqual(expectedOsBuild, optionToString p.OsBuild,"OsBuild is different")
                |None -> Assert.Fail(sprintf "Did not find sccm info for %s %s" os osBuild)
            Assert.IsTrue(actual.IsSome,sprintf "Did not find model %s %s %s" modelCode os osBuild) |> ignore
            printfn "%A" actual
            return actual
        }) with        
        |Ok _ -> ()
        |Error e -> Assert.Fail(e.ToString())

    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    [<TestCase("https://somedomain.com/somefolder/file.txt","file.txt")>]
    let getFileNameFromUrlTest (url,expected) =     
        let actual = getFileNameFromUrl url
        Assert.AreEqual(expected, actual,"File name not expected")

    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    [<TestCase("https://somedomain.com/somefolder/file_201806.txt","2018-06-01")>]
    [<TestCase("https://somedomain.com/somefolder/file_201806.exe","2018-06-01")>]
    [<TestCase("https://somedomain.com/somefolder/file_201806.exe2232","2018-06-01")>]
    [<TestCase("https://somedomain.com/somefolder/file_201806.exe2232.txt","2018-06-01")>]
    [<TestCase("https://somedomain.com/somefolder/ts_p520p520c_w1064_20180613.exe","2018-06-13")>]
    let getReleaseDateFromUrlTest (url,expected) =     
        let actual = getReleaseDateFromUrlBase url
        Assert.AreEqual(expected, actual.ToString("yyyy-MM-dd"),"Release month not expected")    
        
    [<Test>]
    [<Category(TestCategory.IntegrationTests)>]
    [<Apartment(ApartmentState.STA)>]
    [<TestCase("https://support.lenovo.com/downloads/ds122238",
        "https://download.lenovo.com/pccbbs/thinkcentre_drivers/ts_p320tiny_w1064_201806.txt",
        "297ce1fbe0e0dfe4397c1413fe3850211600274356122b44af7d38fd9fcd5be4",
        "https://download.lenovo.com/pccbbs/thinkcentre_drivers/ts_p320tiny_w1064_201806.exe",
        "6aca612b0282e6f24de6aa19173e58c04ed9c480791ccb928cc039378c3eb513",
        "win10",
        "*")>]

    [<TestCase("https://support.lenovo.com/downloads/ds112090",
        "https://download.lenovo.com/pccbbs/mobiles/tp_t460s_w1064_1809_201810.txt",
        "442fa90fb21d02716b1ca755af3249271557016e08283efe67dda747f892f8d1",
        "https://download.lenovo.com/pccbbs/mobiles/tp_t460s_w1064_1809_201810.exe",
        "a0e86800445f919cb9a94c0b5ae26fbc3c0c9c1ed3d2feda7a33131f71d512d1",
        "win10",
        "1809")>]

    [<TestCase("https://support.lenovo.com/downloads/ds112247",
        "https://download.lenovo.com/pccbbs/mobiles/tp_t460p_w1064_201905.txt",
        "4c47c813d16baa3dc5b3e3b25a251eb98ab13cf9d069d29d2bcc001be7c29889",
        "https://download.lenovo.com/pccbbs/mobiles/tp_t460p_w1064_201905.exe",
        "7144fde1287e6289944ae82342792923021f222cc14312e0febcd752c1ab277d",
        "WIN10X64",
        "*"
        )>]

    [<TestCase("https://support.lenovo.com/no/en/downloads/ds540208",
        "https://download.lenovo.com/pccbbs/mobiles/tp_x1carbon_mt20qd-20qe-x1yoga_mt20qf-20qg_w1064_1809_201908.txt",
        "b0e8fe25ee16800a177a31a49a8ac2827b9f4e92b7d3a827b15707a6b3156f84",
        "https://download.lenovo.com/pccbbs/mobiles/tp_x1carbon_mt20qd-20qe-x1yoga_mt20qf-20qg_w1064_1809_201908.exe",
        "b165b8756735dc0e5bab30335a05217d6cce5ca7430821522e83182d1fbf8bab","WIN10X64","1809")>]
    let getLenovoSccmPackageDownloadUrlTest_Success(webPageUrl, expectedReadmeUrl, expectedReadmeChecksum, expectedInstallerUrl, expectedInstallerChecksum,os,osBuild) =      
        printfn "%s" (System.IntPtr.Size.ToString())
        let actualResult = getLenovoSccmPackageDownloadInfo webPageUrl os osBuild
        let expected = 
            {
                ReadmeFile=
                    {
                        Url=expectedReadmeUrl; 
                        Checksum = expectedReadmeChecksum;
                        FileName=(getFileNameFromUrl expectedReadmeUrl);
                        Size=0L;
                    };
                InstallerFile=
                    {
                        Url = expectedInstallerUrl;
                        Checksum = expectedInstallerChecksum; 
                        FileName=(getFileNameFromUrl expectedInstallerUrl);
                        Size=0L
                    }
                
                Released=(getReleaseDateFromUrl expectedInstallerUrl);
                Os=osShortNameToLenovoOs os;
                OsBuild=osBuild
            }
        match actualResult with
        |Ok actual -> Assert.AreEqual(expected,actual)
        |Error e -> Assert.Fail(e.Message)
    
    
    [<Test>]
    [<Category(TestCategory.IntegrationTests)>]
    [<Apartment(ApartmentState.STA)>]
    let getDownloadLinksFromWebPageContentTest () =

        let expectedSccmPacakages = seq {            
            let readmeUrl1 = "https://download.lenovo.com/pccbbs/mobiles/tp_t460s_w1064_201710.txt"
            let installerUrl1 = "https://download.lenovo.com/pccbbs/mobiles/tp_t460s_w1064_201710.exe"
            yield { 
                    ReadmeFile=
                        {
                            Url = readmeUrl1; 
                            Checksum = "25444b51d04288ac041a6b21a318cb88f3fa58c6c049a294e0e8bcfbe060ec8c"; 
                            FileName = (getFileNameFromUrl readmeUrl1);
                            Size=0L;
                        }
                    InstallerFile=
                        {
                            Url= installerUrl1; 
                            Checksum="9ae55aa67c48809cb845957e32df5894cbdff0ab2933a75e3daef5ae895774c7"; 
                            FileName = (getFileNameFromUrl installerUrl1);
                            Size=0L
                        }
                        
                    
                    Released=(getReleaseDateFromUrl installerUrl1);
                    Os="win10";
                    OsBuild="1709"
                }
            let readmeUrl2 = "https://download.lenovo.com/pccbbs/mobiles/tp_t460s_w1064_1803_201807.txt"
            let installerUrl2 = "https://download.lenovo.com/pccbbs/mobiles/tp_t460s_w1064_1803_201807.exe"
            yield { 
                    ReadmeFile=
                        {
                            Url = readmeUrl2; 
                            Checksum = "1bc74a7d91b5dc45585d92d03c14fee59e4d8055cb27374211c462a2b362d6f7"; 
                            FileName = (getFileNameFromUrl readmeUrl2);
                            Size=0L;
                        }
                    InstallerFile=
                        {
                            Url= installerUrl2; 
                            Checksum="d424c27eae77e2ec1df973dc25ffa60854bc833e2b11df007d8f5985add4ea1d"; 
                            FileName = (getFileNameFromUrl installerUrl2);
                            Size=0L
                        }                    
                    Released=(getReleaseDateFromUrl installerUrl2);
                    Os="win10";
                    OsBuild="1803"
                }
            let readmeUrl3 = "https://download.lenovo.com/pccbbs/mobiles/tp_t460s_w1064_1809_201810.txt"
            let installerUrl3 = "https://download.lenovo.com/pccbbs/mobiles/tp_t460s_w1064_1809_201810.exe"
            yield { 
                    ReadmeFile =
                        {
                            Url = readmeUrl3; 
                            Checksum = "442fa90fb21d02716b1ca755af3249271557016e08283efe67dda747f892f8d1"; 
                            FileName = (getFileNameFromUrl readmeUrl3);
                            Size=0L;
                        }
                    InstallerFile=
                        {
                            Url= installerUrl3; 
                            Checksum="a0e86800445f919cb9a94c0b5ae26fbc3c0c9c1ed3d2feda7a33131f71d512d1"; 
                            FileName = (getFileNameFromUrl installerUrl3);    
                            Size=0L
                        }                    
                    Released=(getReleaseDateFromUrl installerUrl3);
                    Os="win10";
                    OsBuild="1809"
                }
        }        
        let url = "https://support.lenovo.com/no/en/downloads/ds112090"
        let testResult =
            result{
                let! contentFile = FileSystem.path (getTempFile "ds112090.html")        
                let! file = (downloadWebContent url contentFile false)
                let! content = (file|> readContentFromFile)                
                let! actualResult = (getDownloadLinksFromWebPageContent content)
                let actual = actualResult |>Seq.toArray
                System.Console.Write(actual.ToString())
                let expectedArray = (expectedSccmPacakages |> Seq.toArray)
                for i in 0..2 do
                    Assert.AreEqual(expectedArray.[i].InstallerFile.Checksum,actual.[i].InstallerFile.Checksum,"InstallerChecksum"  + i.ToString())
                    Assert.AreEqual(expectedArray.[i].InstallerFile.FileName,actual.[i].InstallerFile.FileName,"InstallerFileName"  + i.ToString())
                    Assert.AreEqual(expectedArray.[i].InstallerFile.Url,actual.[i].InstallerFile.Url,"InstallerUrl" + i.ToString())
                    Assert.AreEqual(expectedArray.[i].ReadmeFile.Checksum,actual.[i].ReadmeFile.Checksum,"ReadmeChecksum" + i.ToString())
                    Assert.AreEqual(expectedArray.[i].ReadmeFile.FileName,actual.[i].ReadmeFile.FileName,"ReadmeFileName" + i.ToString())
                    Assert.AreEqual(expectedArray.[i].ReadmeFile.Url,actual.[i].ReadmeFile.Url,"ReadmeUrl" + i.ToString())
                    Assert.AreEqual(expectedArray.[i].Os,actual.[i].Os,"Os" + i.ToString())
                    Assert.AreEqual(expectedArray.[i].OsBuild,actual.[i].OsBuild,"OsBuild" + i.ToString())
                    Assert.AreEqual(expectedArray.[i].Released,actual.[i].Released,"Released" + i.ToString())
                //Assert.IsTrue( (expectedArray = actual) )
                //Assert.Fail()
                return ()
            }
        match testResult with
        |Ok v -> Assert.IsTrue(true)
        |Error ex -> Assert.Fail(ex.Message)
    
    open DriverTool.LenovoCatalogXml
    open NUnit.Framework

    type internal TestData ={IsSuccess:bool;Expected:LenovoCatalogProduct;ItemIndex:int;ExpectedErrorMessage:string}

    let internal testData = 
        [
            yield 
                {
                    IsSuccess = true
                    Expected={
                        Model= "Tablet10"
                        Family="len"
                        Os="win10"
                        Build="*"
                        Name="Lenovo Tablet 10"
                        DriverPacks=
                            [|
                                yield {Id="sccm";Date= Some (toDateTime 2018 7 1);Url = "https://support.lenovo.com/downloads/ds503143";}
                                yield {Id="WinPE 10";Date= None;Url = "no winpe"}
                            |]
                        Queries = 
                            {
                                ModelTypes = 
                                    [|
                                        yield ModelType "20L4"
                                        yield ModelType "20L3"
                                    |]
                                Version = "Lenovo Tablet 10"
                                Smbios = "N29E"
                            }
                    }
                    ItemIndex=0
                    ExpectedErrorMessage = "N/A"
                }
            yield 
                {
                    IsSuccess = true
                    Expected={
                        Model= "M710q-SKL"
                        Family="tc"
                        Os="win764"
                        Build="*"
                        Name="ThinkCentre M710q - SKL"
                        DriverPacks=
                            [|
                                yield {Id="sccm";Date= Some (toDateTime 2017 2 1);Url = "https://support.lenovo.com/downloads/ds120804"}
                                yield {Id="WinPE 3.1 x64";Date= None;Url = "https://support.lenovo.com/downloads/ds105413"}
                                yield {Id="WinPE 5";Date= None;Url = "no winpe"}
                                yield {Id="WinPE 10";Date= None;Url = "http://support.lenovo.com/downloads/ds105415"}
                            |]
                        Queries = 
                            {
                                ModelTypes = 
                                    [|
                                        yield ModelType "10QR"
                                        yield ModelType "10MT"
                                        yield ModelType "10MS"
                                        yield ModelType "10MR"                                        
                                        yield ModelType "10MQ"                                        
                                    |]
                                Version = "ThinkCentre M710q"
                                Smbios = "M1AK"
                            }
                    }
                    ItemIndex=12
                    ExpectedErrorMessage = "N/A"
                }
        ]

    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    [<TestCaseSource("testData")>]
    let loadLenovoCatalogTest (testDataObject:obj) =
        let testData = (testDataObject:?>TestData)
        match(result {              
            let! tempDestinationFolderPath = FileSystem.path (PathOperations.getTempPath)
            let! catalogXmlPath = EmbeddedResource.extractEmbeddedResourceByFileNameBase ("LenovoCatalog.xml", tempDestinationFolderPath,"LenovoCatalog.xml",typeof<ThisAssembly>.Assembly)
            let! existingCatalogXmlPath = ensureFileExists catalogXmlPath
            let! catalogProducts = loadLenovoCatalog existingCatalogXmlPath
            return catalogProducts
        }) with
        |Ok v -> 
            Assert.IsTrue(true,sprintf "Success: %A" v)
            Assert.IsTrue((Seq.length v) > 0,sprintf "Lenght of CatalogProducts was not greater than zero")
            let actualProduct = Seq.item testData.ItemIndex v
            Assert.AreEqual(testData.Expected,actualProduct,"Products Not equal.")
        |Error ex ->
            Assert.IsFalse(true,sprintf "Expected success but failed instead: %s" ex.Message)       

    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    [<TestCase("ds112090.html","WIN10X64", "1809",true,"https://download.lenovo.com/pccbbs/mobiles/tp_t460s_w1064_1809_201810.exe")>]
    [<TestCase("ds540208.html","WIN10X64", "1809",true,"https://download.lenovo.com/pccbbs/mobiles/tp_x1carbon_mt20qd-20qe-x1yoga_mt20qf-20qg_w1064_1809_201908.exe")>]
    [<TestCase("ds540208_v2.html","WIN10X64", "1903",true,"https://download.lenovo.com/pccbbs/mobiles/tp_x1carbon_mt20qd-20qe-x1yoga_mt20qf-20qg_w1064_1903_201908.exe")>]
    [<TestCase("ds540208_v2.html","WIN10X64", "1809",true,"https://download.lenovo.com/pccbbs/mobiles/tp_x1carbon_mt20qd-20qe-x1yoga_mt20qf-20qg_w1064_1809_201909.exe")>]
    [<TestCase("ds540208_v2.html","WIN10X64", "1803",true,"https://download.lenovo.com/pccbbs/mobiles/tp_x1carbon_mt20qd-20qe-x1yoga_mt20qf-20qg_w1064_1803_201909.exe")>]
    [<TestCase("ds540208_v2.html","WIN10X64", "1709",true,"https://download.lenovo.com/pccbbs/mobiles/tp_x1carbon_mt20qd-20qe-x1yoga_mt20qf-20qg_w1064_1709_201909.exe")>]    
    [<TestCase("ds540208.html","WIN10X64", "1903",true,"https://download.lenovo.com/pccbbs/mobiles/tp_x1carbon_mt20qd-20qe-x1yoga_mt20qf-20qg_w1064_1903_201908.exe")>]
    [<TestCase("ds540208.html","WIN10X64", "1809",true,"https://download.lenovo.com/pccbbs/mobiles/tp_x1carbon_mt20qd-20qe-x1yoga_mt20qf-20qg_w1064_1809_201908.exe")>]
    [<TestCase("ds540208.html","WIN10X64", "1803",true,"https://download.lenovo.com/pccbbs/mobiles/tp_x1carbon_mt20qd-20qe-x1yoga_mt20qf-20qg_w1064_1803_201908.exe")>]
    [<TestCase("ds540208.html","WIN10X64", "1709",true,"https://download.lenovo.com/pccbbs/mobiles/tp_x1carbon_mt20qd-20qe-x1yoga_mt20qf-20qg_w1064_1709_201908.exe")>]
    let getLenovoSccmPackageDownloadInfoFromContentTest (fileName:string, os:string, osbuild:string, expectedSucess:bool, expectedInstallerUrlString:string) =
        match(result{
            let! destinationFolderPath = FileSystem.path getTempPath
            let! destinationFilePath = EmbeddedResource.extractEmbeddedResourceByFileNameBase (fileName,destinationFolderPath,fileName,System.Reflection.Assembly.GetExecutingAssembly())
            Assert.IsTrue((fileExists destinationFilePath),sprintf "File does not exist: %s" (FileSystem.pathValue destinationFilePath))
            let content = System.IO.File.ReadAllText((FileSystem.pathValue destinationFilePath))
            let! info = getLenovoSccmPackageDownloadInfoFromContent content os osbuild
            Assert.AreEqual(expectedInstallerUrlString,info.InstallerFile.Url)
            return destinationFolderPath
        }) with
        |Ok v -> Assert.IsTrue(expectedSucess,sprintf "Expected failure but succeded instead." )
        |Error ex ->
            Assert.IsFalse(true,sprintf "Expected success but failed instead: %s" ex.Message)      
        
    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    [<TestCase("ds112090.html", 3, true,"N/A", "https://support.lenovo.com/no/en/downloads/ds112090")>]
    [<TestCase("ds112090_MissingDownloadsTab.html", 0, false,"Download tabs ('downloadTabs') not found in html document.", "https://support.lenovo.com/no/en/downloads/ds112090")>]
    [<TestCase("ds112090_Missing_Downloads_Links.html", 0, false,"No download links were found beneath the downloads tab in the html document.", "https://support.lenovo.com/no/en/downloads/ds112090")>]
    //[<TestCase("ds112090_Invalid_Document.html", 0, false,"N/A.", "https://support.lenovo.com/no/en/downloads/ds112090")>]

    
    let getDownloadLinksFromWebPageContentTests (fileName:string, expectedCount:int, expectedSuccess:bool,expectedErrorMessage:string,source:string) =
        match(result{
            let! destinationFolderPath = FileSystem.path getTempPath
            let! destinationFilePath = EmbeddedResource.extractEmbeddedResourceByFileNameBase (fileName,destinationFolderPath,fileName,System.Reflection.Assembly.GetExecutingAssembly())
            Assert.IsTrue((fileExists destinationFilePath),sprintf "File does not exist: %s" (FileSystem.pathValue destinationFilePath))
            let content = System.IO.File.ReadAllText((FileSystem.pathValue destinationFilePath))
            let! actual = getDownloadLinksFromWebPageContent content
            let actualArray = actual |> Seq.toArray
            Assert.AreEqual(expectedCount,(Array.length actualArray),"Sccm package info count is not expected.")
            return actual
        }) with
        |Ok v -> Assert.IsTrue(expectedSuccess,sprintf "Expected failure but succeded instead." )
        |Error ex ->
            Assert.IsFalse(expectedSuccess,sprintf "Expected success but failed instead: %s" ex.Message)
            Assert.IsTrue(ex.Message.StartsWith(expectedErrorMessage),sprintf "Error message '%s' does not contain '%s'" ex.Message expectedErrorMessage)