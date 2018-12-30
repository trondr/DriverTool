namespace DriverTool

open NUnit.Framework
open System

[<TestFixture>]
module HpCatalogTests =
    
    [<Test>]
    let downloadDriverPackCatalogTest () = 
        let actual = 
            result
                {
                    let! xmlPath = HpCatalog.downloadDriverPackCatalog()
                    return xmlPath
                }
        match actual with
        |Ok p -> 
            printf "%s" (p.Value)
            Assert.IsTrue(true)
        |Error e -> Assert.Fail(String.Format("{0}", e.Message))
    


