namespace DriverTool.Tests

open NUnit.Framework

[<TestFixture>]
[<Category(TestCategory.UnitTests)>]
module AssemblyResolveTests =    
    open DriverTool.Tests.Init
    open DriverTool.Library.AssemblyResolver
    open System.Reflection
    
    [<Test>]
    [<TestCase(true,true)>]
    [<TestCase(false,false)>]
    let loadAssemblyFromSearchPathTest (fileExists:bool, isNonNull:bool) =

        let fileExistStub (filePath:string) =
            fileExists

        let assemblyLoadStub (filePath:string) =
            match fileExists with
            |true -> typeof<ThisTestAssembly>.Assembly
            |false -> null
        
        let searchPaths = [|@"c:\temp";@"c:\temp2"|]

        let actual = loadAssemblyFromSearchPathBase (assemblyLoadStub,fileExistStub,searchPaths,new AssemblyName("SomeAssembly"))
        Assert.IsTrue((actual<>null) = isNonNull)
        ()
