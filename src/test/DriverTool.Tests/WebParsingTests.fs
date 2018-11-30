namespace DriverTool.Tests
open NUnit.Framework
open System
open DriverTool

[<TestFixture>]
module WebParsingTests  =    
   
    open F
    open DriverTool.LenovoCatalog
    open System.Threading

    [<Test>]    
    [<Apartment(ApartmentState.STA)>]
    [<TestCase("https://support.lenovo.com/downloads/ds122238","https://download.lenovo.com/pccbbs/thinkcentre_drivers/ts_p320tiny_w1064_201806.txt","297ce1fbe0e0dfe4397c1413fe3850211600274356122b44af7d38fd9fcd5be4","https://download.lenovo.com/pccbbs/thinkcentre_drivers/ts_p320tiny_w1064_201806.exe","6aca612b0282e6f24de6aa19173e58c04ed9c480791ccb928cc039378c3eb513")>]
    [<TestCase("https://support.lenovo.com/downloads/ds112090","https://download.lenovo.com/pccbbs/mobiles/tp_t460s_w1064_1809_201810.txt","442fa90fb21d02716b1ca755af3249271557016e08283efe67dda747f892f8d1","https://download.lenovo.com/pccbbs/mobiles/tp_t460s_w1064_1809_201810.exe","a0e86800445f919cb9a94c0b5ae26fbc3c0c9c1ed3d2feda7a33131f71d512d1")>]
    let getLenovoSccmPackageDownloadUrlTest_Success(webPageUrl, expectedReadmeUrl, expectedReadmeChecksum, expectedInstallerUrl, expectedInstallerChecksum) =      
        printfn "%s" (System.IntPtr.Size.ToString())
        let actualResult = getLenovoSccmPackageDownloadInfo webPageUrl
        let expected = {ReadmeUrl=expectedReadmeUrl; ReadmeChecksum = expectedReadmeChecksum; InstallerUrl = expectedInstallerUrl;InstallerChecksum = expectedInstallerChecksum}
        match actualResult with
        |Ok actual -> Assert.AreEqual(expected,actual)
        |Error e -> Assert.Fail(String.Format("{0}", e.Message))