namespace DriverTool.Tests
open NUnit.Framework
open System
open DriverTool

[<TestFixture>]
module WebParsingTests  =
    open DriverTool.Util.FSharp

    [<Test>]
    let getLenovoSccmPackageDownloadUrlTest_Success() =
        let actualResult = WebParsing.getLenovoSccmPackageDownloadUrl "https://support.lenovo.com/downloads/ds122238"
        let expected = "https://download.lenovo.com/pccbbs/thinkcentre_drivers/ts_p320tiny_w1064_201806.exe"
        match actualResult with
        |Ok actual -> Assert.AreEqual(expected,actual)
        |Error e -> Assert.Fail(String.Format("{0}", e.Message))