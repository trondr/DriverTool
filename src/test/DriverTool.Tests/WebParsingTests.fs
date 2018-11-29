namespace DriverTool.Tests
open NUnit.Framework
open System
open DriverTool

[<TestFixture>]
module WebParsingTests  =    
    open System.Threading

    let verifyAppartmentStateSta =
        if (Thread.CurrentThread.GetApartmentState() <> ApartmentState.STA) then
            raise (new ThreadStateException("The current threads apartment state is not STA"))

    [<Test>]    
    [<Apartment(ApartmentState.STA)>]
    let getLenovoSccmPackageDownloadUrlTest_Success() =
        verifyAppartmentStateSta |> ignore
        printfn "%s" (System.IntPtr.Size.ToString())
        let actualResult = WebParsing.getLenovoSccmPackageDownloadUrl "https://support.lenovo.com/downloads/ds122238"
        let expected = 
            [("readme", "https://download.lenovo.com/pccbbs/thinkcentre_drivers/ts_p320tiny_w1064_201806.txt", "297ce1fbe0e0dfe4397c1413fe3850211600274356122b44af7d38fd9fcd5be4"); ("installer","https://download.lenovo.com/pccbbs/thinkcentre_drivers/ts_p320tiny_w1064_201806.exe","6aca612b0282e6f24de6aa19173e58c04ed9c480791ccb928cc039378c3eb513")]
        match actualResult with
        |Ok actual -> Assert.AreEqual(expected,actual)
        |Error e -> Assert.Fail(String.Format("{0}", e.Message))