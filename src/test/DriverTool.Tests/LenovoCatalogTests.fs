namespace DriverTool.Tests

open NUnit.Framework

[<TestFixture>]
module LenovoCatalogTests=
    
    open DriverTool.LenovoCatalog
    open Microsoft.FSharp.Core

    [<Test>]
    let getSccmPackagesInfoTest () =        
        let actual = getSccmPackageInfos
        match actual with
        | Ok products -> 
            let productArray = products|>Seq.toArray
            Assert.IsTrue(productArray.Length > 300,"Expected product array greater than zeror." )
        | Error ex -> Assert.Fail("Did not expect getSccmPackagesInfo to fail. Error: " + ex.Message)

    open System.Threading
    open F
    open System
    open DriverTool

    [<Test>]
    [<Apartment(ApartmentState.STA)>]
    let getSccmPackageDownloadInfosTest () =
        let sccmPackageInfos = getSccmPackageInfos
        match sccmPackageInfos with
        |Error ex -> 
            Assert.Fail("Did not expect to fail. Error:" + ex.Message)
            //Result.Error (new System.Exception("Did not expect to fail"))
        |Ok products ->
            let downloadInfosResult = 
                products
                |> F.getNRandomItems 4
                |> Seq.map (fun p -> 
                                getLenovoSccmPackageDownloadInfo  p.SccmDriverPackUrl.Value                            
                                )
                |> F.toAccumulatedResult                
            match downloadInfosResult with
            |Ok dis -> 
                dis 
                |> Seq.map(fun di -> System.Console.WriteLine("{0};{1}",di.InstallerUrl,di.InstallerChecksum))
                |> Seq.iter (fun p -> p)
                //|> ignore
            |Error ex -> 
                Assert.Fail("Did not expect to fail. Error:" + ex.Message)                
        Assert.IsTrue(true)
      
    [<Test>]
    let getUniqueLenovoOperatingSystemTest () =
        let result = 
            result
                {
                    let! products = getSccmPackageInfos
                    let uniqueOses = 
                        products 
                        |> Seq.map(fun p -> p.Os)                        
                        |> getUnique
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
    
    let osShortNameToLenovoOsTest_UnSupported (osShortName) =
        Assert.Throws<System.Exception>(fun () -> (
                                                                osShortNameToLenovoOs osShortName|> ignore                        
                                                                Assert.IsTrue(true)
                                           ))|>ignore
        Assert.IsTrue(true)

    [<Test>]
    [<TestCase("ThinkPad P50","win10","*","P50","ThinkPad P50","win10","*")>]
    let findSccmPackageInfoByNameAndOsAndBuildTest (name,os,osbuild,expectedmodel,expectedname,expectedos,expectedosbuild) =
        result{
            let! products = getSccmPackageInfos
            let actual = findSccmPackageInfoByNameAndOsAndBuild name os osbuild products
            Assert.AreEqual(actual.Model.Value,expectedmodel)
            Assert.AreEqual(actual.Name,expectedname)
            Assert.AreEqual(actual.Os,expectedos)
            Assert.AreEqual(actual.OsBuild.Value,expectedosbuild)
            Assert.AreEqual(actual.Name,expectedname)

            return actual            
        } |> ignore
        
    [<Test>]    
    let findSccmPackageInfoByNameAndOsAndBuildTestCurrentSystem () =  
        let modelInfo = getModelInfo
        match result{
                        let! products = getSccmPackageInfos
                        let actual = findSccmPackageInfoByNameAndOsAndBuild modelInfo.Name modelInfo.Os modelInfo.OsBuild products
                        return actual
                    } with
        |Ok v -> 
            Assert.AreEqual(modelInfo.Name,v.Name)
            Assert.AreEqual(modelInfo.Os,v.Os)
            Assert.IsTrue(modelInfo.OsBuild = v.OsBuild.Value || v.OsBuild.Value = "*")
            Assert.IsTrue(true)
        |Error ex -> Assert.Fail(ex.Message)
           
    [<Test>]
    let findSccmPackageInfoByModelCode4AndOsAndBuildTest () =
        result{
                let! products = getSccmPackageInfos
                let randomProducts = 
                    products
                    |>getNRandomItems 10
                randomProducts 
                |> Seq.map (fun p -> 
                                p.ModelCodes
                                |> Seq.map (fun m -> 
                                                let actual = (findSccmPackageInfoByModelCode4AndOsAndBuild m p.Os p.OsBuild.Value products)
                                                Assert.IsTrue(actual.IsSome) |> ignore
                                            ) |> ignore
                            )|>ignore

                let actual = findSccmPackageInfoByModelCode4AndOsAndBuild "20FA" "win10" "1709" products
                Assert.IsTrue(actual.IsSome,"Did not find model 20FA win10 *") |> ignore
                return ""
        } |> ignore
        
    [<Test>]
    [<TestCase("https://somedomain.com/somefolder/file.txt","file.txt")>]
    let getFileNameFromUrlTest (url,expected) =     
        let actual = getFileNameFromUrl url
        Assert.AreEqual(expected, actual,"File name not expected")

    [<Test>]
    [<TestCase("https://somedomain.com/somefolder/file_201806.txt","2018-06-01")>]
    [<TestCase("https://somedomain.com/somefolder/file_201806.exe","2018-06-01")>]
    [<TestCase("https://somedomain.com/somefolder/file_201806.exe2232","2018-06-01")>]
    [<TestCase("https://somedomain.com/somefolder/file_201806.exe2232.txt","2018-06-01")>]
    let getReleaseDateFromUrlTest (url,expected) =     
        let actual = getReleaseDateFromUrl url
        Assert.AreEqual(expected, actual.ToString("yyyy-MM-dd"),"Release month not expected")    
        
    [<Test>]    
    [<Apartment(ApartmentState.STA)>]
    [<TestCase("https://support.lenovo.com/downloads/ds122238","https://download.lenovo.com/pccbbs/thinkcentre_drivers/ts_p320tiny_w1064_201806.txt","297ce1fbe0e0dfe4397c1413fe3850211600274356122b44af7d38fd9fcd5be4","https://download.lenovo.com/pccbbs/thinkcentre_drivers/ts_p320tiny_w1064_201806.exe","6aca612b0282e6f24de6aa19173e58c04ed9c480791ccb928cc039378c3eb513")>]
    [<TestCase("https://support.lenovo.com/downloads/ds112090","https://download.lenovo.com/pccbbs/mobiles/tp_t460s_w1064_1809_201810.txt","442fa90fb21d02716b1ca755af3249271557016e08283efe67dda747f892f8d1","https://download.lenovo.com/pccbbs/mobiles/tp_t460s_w1064_1809_201810.exe","a0e86800445f919cb9a94c0b5ae26fbc3c0c9c1ed3d2feda7a33131f71d512d1")>]
    let getLenovoSccmPackageDownloadUrlTest_Success(webPageUrl, expectedReadmeUrl, expectedReadmeChecksum, expectedInstallerUrl, expectedInstallerChecksum) =      
        printfn "%s" (System.IntPtr.Size.ToString())
        let actualResult = getLenovoSccmPackageDownloadInfo webPageUrl
        let expected = {ReadmeUrl=expectedReadmeUrl; ReadmeChecksum = expectedReadmeChecksum;ReadmeFileName=(getFileNameFromUrl expectedReadmeUrl); InstallerUrl = expectedInstallerUrl;InstallerChecksum = expectedInstallerChecksum; InstallerFileName=(getFileNameFromUrl expectedInstallerUrl);Released=(getReleaseDateFromUrl expectedInstallerUrl);Os="";OsBuild=""}
        match actualResult with
        |Ok actual -> Assert.AreEqual(expected,actual)
        |Error e -> Assert.Fail(String.Format("{0}", e.Message))
    
    
    open DriverTool.WebParsing
    open DriverTool.FileOperations
    open DriverTool.PathOperations


    [<Test>]    
    let getDownloadLinksFromWebPageContentTest () =

        let expectedSccmPacakages = seq {            
            let readmeUrl1 = "https://download.lenovo.com/pccbbs/mobiles/tp_t460s_w1064_201710.txt"
            let installerUrl1 = "https://download.lenovo.com/pccbbs/mobiles/tp_t460s_w1064_201710.exe"
            yield { 
                    ReadmeUrl = readmeUrl1; 
                    ReadmeChecksum = "25444b51d04288ac041a6b21a318cb88f3fa58c6c049a294e0e8bcfbe060ec8c"; 
                    ReadmeFileName = (getFileNameFromUrl readmeUrl1); 
                    InstallerUrl= installerUrl1; 
                    InstallerChecksum="9ae55aa67c48809cb845957e32df5894cbdff0ab2933a75e3daef5ae895774c7"; 
                    InstallerFileName = (getFileNameFromUrl installerUrl1);
                    Released=(getReleaseDateFromUrl installerUrl1);
                    Os="win10";
                    OsBuild="1709"
                }
            let readmeUrl2 = "https://download.lenovo.com/pccbbs/mobiles/tp_t460s_w1064_1803_201807.txt"
            let installerUrl2 = "https://download.lenovo.com/pccbbs/mobiles/tp_t460s_w1064_1803_201807.exe"
            yield { 
                    ReadmeUrl = readmeUrl2; 
                    ReadmeChecksum = "1bc74a7d91b5dc45585d92d03c14fee59e4d8055cb27374211c462a2b362d6f7"; 
                    ReadmeFileName = (getFileNameFromUrl readmeUrl2); 
                    InstallerUrl= installerUrl2; 
                    InstallerChecksum="d424c27eae77e2ec1df973dc25ffa60854bc833e2b11df007d8f5985add4ea1d"; 
                    InstallerFileName = (getFileNameFromUrl installerUrl2);
                    Released=(getReleaseDateFromUrl installerUrl2);
                    Os="win10";
                    OsBuild="1803"
                }
            let readmeUrl3 = "https://download.lenovo.com/pccbbs/mobiles/tp_t460s_w1064_1809_201810.txt"
            let installerUrl3 = "https://download.lenovo.com/pccbbs/mobiles/tp_t460s_w1064_1809_201810.exe"
            yield { 
                    ReadmeUrl = readmeUrl3; 
                    ReadmeChecksum = "442fa90fb21d02716b1ca755af3249271557016e08283efe67dda747f892f8d1"; 
                    ReadmeFileName = (getFileNameFromUrl readmeUrl3); 
                    InstallerUrl= installerUrl3; 
                    InstallerChecksum="a0e86800445f919cb9a94c0b5ae26fbc3c0c9c1ed3d2feda7a33131f71d512d1"; 
                    InstallerFileName = (getFileNameFromUrl installerUrl3);
                    Released=(getReleaseDateFromUrl installerUrl3);
                    Os="win10";
                    OsBuild="1809"
                }
        }        
        let url = "https://support.lenovo.com/no/en/downloads/ds112090"
        let testResult =
            result{
                let contentFile = getTempFile "ds112090.html"        
                let! file = (downloadWebContent url contentFile false)
                let! content = (file |> readContentFromFile)                
                let actual = (getDownloadLinksFromWebPageContent content)|>Seq.toArray
                System.Console.Write(actual.ToString())
                let expectedArray = (expectedSccmPacakages |> Seq.toArray)
                for i in 0..2 do
                    Assert.AreEqual(expectedArray.[i].InstallerChecksum,actual.[i].InstallerChecksum,"InstallerChecksum"  + i.ToString())
                    Assert.AreEqual(expectedArray.[i].InstallerFileName,actual.[i].InstallerFileName,"InstallerFileName"  + i.ToString())
                    Assert.AreEqual(expectedArray.[i].InstallerUrl,actual.[i].InstallerUrl,"InstallerUrl" + i.ToString())
                    Assert.AreEqual(expectedArray.[i].ReadmeChecksum,actual.[i].ReadmeChecksum,"ReadmeChecksum" + i.ToString())
                    Assert.AreEqual(expectedArray.[i].ReadmeFileName,actual.[i].ReadmeFileName,"ReadmeFileName" + i.ToString())
                    Assert.AreEqual(expectedArray.[i].ReadmeUrl,actual.[i].ReadmeUrl,"ReadmeUrl" + i.ToString())
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