namespace DriverTool.Tests

module LenovoCatalogXmlTests =
    
    open NUnit.Framework

    [<Test>]
    [<TestCase("201905a","20190501")>]
    [<TestCase("201906","20190601")>]
    [<Category(TestCategory.UnitTests)>]
    let toDateTests (stringDate, expectedDate) =                
        let actual = DriverTool.LenovoCatalogXml.toDate (Some stringDate)
        match actual with
        |Some d ->
            let actualDate= (sprintf "%i%02i%02i" d.Year d.Month d.Day)
            Assert.AreEqual(expectedDate,actualDate)
        |None->
            Assert.Fail("Returned date was None.")

