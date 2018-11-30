namespace DriverTool.Tests

open NUnit.Framework

[<TestFixture>]
module LenovoCatalogTests=
    
    open DriverTool.LenovoCatalog
    open Microsoft.FSharp.Core

    [<Test>]
    let getSccmPackagesInfoTest () =        
        let actual = getSccmPackagesInfo
        match actual with
        | Ok products -> 
            let productArray = products|>Seq.toArray
            Assert.IsTrue(productArray.Length > 300,"Expected product array greater than zeror." )
        | Error ex -> Assert.Fail("Did not expect getSccmPackagesInfo to fail. Error: " + ex.Message)

    open System.Threading
    [<Test>]
    [<Apartment(ApartmentState.STA)>]
    let getSccmPackageDownloadInfosTest () =
        let sccmPackageInfos = getSccmPackagesInfo
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
        