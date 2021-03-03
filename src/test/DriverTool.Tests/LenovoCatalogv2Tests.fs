namespace DriverTool.Tests

open NUnit.Framework
open DriverTool.LenovoCatalogv2
open DriverTool.LenovoCatalog
open DriverTool.Library
open DriverTool.Library.FileOperations

[<TestFixture>]
module LenovoCatalogv2Tests=

    [<Test>]
    [<Category(TestCategory.IntegrationTests)>]
    let getSccmPackageInfosv2Test () =
        match(result{
            use cacheFolder = new DirectoryOperations.TemporaryFolder(logger)
            let! cacheFolderPath = cacheFolder.FolderPath
            let! res = getSccmPackageInfosv2 cacheFolderPath
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
    let findSccmPackageInfoByModelCode4AndOsAndBuildTest2 (fileName:string,modelCode:string,os:string,osBuild:string, expectedOsBuild, expectedUrl) =
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
        |Ok _ -> ()
        |Error e -> Assert.Fail(e.ToString())
