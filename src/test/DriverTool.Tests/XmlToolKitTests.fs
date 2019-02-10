namespace DriverTool.Tests

open NUnit.Framework

[<TestFixture>]
[<Category(TestCategory.UnitTests)>]
module XmlToolKitTests = 

    open DriverTool.XmlToolKit
    open System.IO

    [<Test>]
    let xmlDocLoadSaveTest () =
        let memoryStreamToString (ms:MemoryStream) =
            ms.Position <- 0L
            use reader = new StreamReader(ms)
            let text = reader.ReadToEnd()
            text
        let doc =
                XDocument (XDeclaration "1.0" "utf-8" "yes") [
                    XComment "Saved by DriverTool function."
                    XElement "configuration" [
                        XElement "Name1" ["Value1"]
                        XElement "Name2" ["Value2"]
                        XElement "Name3" ["Value3"]
                        XElement "Name4" ["Value4"]                        
                    ]
                ]
        let expectedXmlValue = sprintf "%s" (doc.Declaration.ToString() + System.Environment.NewLine + doc.ToString())
        printfn "%s" expectedXmlValue
        use ms = doc.Save()
        let actualXmlValue = memoryStreamToString ms
        printfn "%s" actualXmlValue
        Assert.AreEqual(expectedXmlValue, actualXmlValue)

        