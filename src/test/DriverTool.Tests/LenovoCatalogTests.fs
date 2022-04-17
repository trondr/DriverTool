namespace DriverTool.Tests

open NUnit.Framework
open DriverTool
open DriverTool.Library

[<TestFixture>]
module LenovoCatalogTests=
    
    open DriverTool.LenovoCatalog
    open DriverTool.LenovoUpdates
    open DriverTool.Library.PackageXml
    open Microsoft.FSharp.Core    
    open DriverTool.Library.Web
    open DriverTool.Library.WebParsing
    open DriverTool.Library.FileSystem
    open DriverTool.Library.FileOperations
    open DriverTool.Library.PathOperations
    open System.Threading    
    open DriverTool.Library.Logging

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
        |Result.Ok _ -> ()
        |Result.Error e -> Assert.Fail(e.ToString())

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
        |Result.Ok v -> 
            Assert.IsTrue(true,sprintf "Success: %A" v)
            Assert.IsTrue((Seq.length v) > 0,sprintf "Lenght of CatalogProducts was not greater than zero")
            let actualProduct = Seq.item testData.ItemIndex v
            Assert.AreEqual(testData.Expected,actualProduct,"Products Not equal.")
        |Result.Error ex ->
            Assert.IsFalse(true,sprintf "Expected success but failed instead: %s" ex.Message)       

    [<Test>]
    [<Category(TestCategory.IntegrationTests)>]
    let getSccmPackageInfosv2Test () =
        match(result{
            use cacheFolder = new DirectoryOperations.TemporaryFolder(logger)
            let! cacheFolderPath = cacheFolder.FolderPath
            let! res = getSccmPackageInfosv2 cacheFolderPath reportProgressStdOut'
            return res
        }) with
        |Result.Ok v -> 
            Assert.IsTrue(v.Length > 0)            
        |Result.Error ex -> Assert.Fail(ex.Message)        
        ()


    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    [<TestCase("LenovoCatalog_catalogv2.xml","20L8","win10","1809","1809","https://download.lenovo.com/pccbbs/mobiles/tp_t480s_w1064_1809_202012.exe")>]    
    [<TestCase("LenovoCatalog_catalogv2.xml","20L8","win10","20H2","2004","https://download.lenovo.com/pccbbs/mobiles/tp_t480s_w1064_2004_202012.exe")>]
    [<TestCase("LenovoCatalog_catalogv2.xml","20FQ","win10","20H2","1909","https://download.lenovo.com/pccbbs/mobiles/tp_x1carbon_mt20fb-20fc-x1yoga_mt20fq-20fr_w1064_1909_201911.exe")>]
    [<TestCase("LenovoCatalog_catalogv2.xml","20QG","win10","*","2004","https://download.lenovo.com/pccbbs/mobiles/tp_x1carbon_mt20qd-20qe-20r1-20r2-x1yoga_mt20qf-20qg-20sa-20sb_w1064_2004_202009.exe")>]
    [<TestCase("LenovoCatalog_catalogv2.xml","20QG","win10","1809","1809","https://download.lenovo.com/pccbbs/mobiles/tp_x1carbon_mt20qd-20qe-x1yoga_mt20qf-20qg_w1064_1809_202006.exe")>]
    let findSccmPackageInfoByModelCode4AndOsAndBuildTest3 (fileName:string,modelCode:string,os:string,osBuild:string, expectedOsBuild, expectedUrl) =
        match (result{
            use cacheFolder = new DirectoryOperations.TemporaryFolder(logger)
            let! cacheFolderPath = cacheFolder.FolderPath                        
            let! destinationFilePath = EmbeddedResource.extractEmbeddedResourceByFileNameBase (fileName,cacheFolderPath,fileName,System.Reflection.Assembly.GetExecutingAssembly())
            Assert.IsTrue((fileExists destinationFilePath),sprintf "File does not exist: %s" (FileSystem.pathValue destinationFilePath))
            let! lenovoCatalogModels = DriverTool.LenovoCatalogXml.loadLenovoCatalogv2 destinationFilePath
            let products = getSccmPackageInfosFromLenovoCataloModels lenovoCatalogModels            
            let actual = findSccmPackageInfoByModelCode4AndOsAndBuild logger modelCode os osBuild products
            let eval =
                match actual with
                |Some p -> 
                    Assert.AreEqual(os, p.Os,"Os is different")
                    Assert.AreEqual(expectedOsBuild, optionToString p.OsBuild,"OsBuild is different")
                    match p.SccmDriverPackUrl with
                    |Some u -> Assert.AreEqual(expectedUrl,u,"Sccm driver package url was not expected")
                    |None -> Assert.Fail(sprintf "Did not find sccm package url for %s %s" os osBuild)
                |None -> Assert.Fail(sprintf "Did not find sccm info for %s %s" os osBuild)
            Assert.IsTrue(actual.IsSome,sprintf "Did not find model %s %s %s" modelCode os osBuild) |> ignore
            printfn "%A" actual
            return actual
        }) with        
        |Result.Ok _ -> ()
        |Result.Error e -> Assert.Fail(e.ToString())
    