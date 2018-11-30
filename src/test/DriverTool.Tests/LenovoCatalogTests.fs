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
            
        
