namespace DriverTool.Tests
open NUnit.Framework
open Paths

[<TestFixture>]
module UnitTests  =
    open System.Security.Cryptography
         
    [<Test>]
    [<TestCase("","Empty string",false)>]
    [<TestCase(@"c:\temp","Local path",false)>]
    [<TestCase(@"\\servername\temp","Remote path",false)>]    
    let HasInvalidPathCharactersTests (path:string) (description:string) (expected:bool) =
        let actual = HasInvalidPathCharacters path
        Assert.AreEqual(expected,actual,description)

    [<Test>]
    [<TestCase(@"\\servername\temp","Remote path",false)>]
    [<TestCase(@"\\servername\temp*","Wildcard path *",true)>]
    [<TestCase(@"\\servername\temp?","Wildcard path ?",true)>]
    let HasWildCardCharactersTests (path:string) (description:string) (expected:bool) =
        let actual = HasWildCardCharacters path
        Assert.AreEqual(expected,actual,description)

    [<Test>]
    [<TestCase("","Empty string",false)>]
    [<TestCase(@"c:\temp","Local path",true)>]
    [<TestCase(@"\\servername\temp","Remote path",true)>]
    [<TestCase(@"\\servername\temp*","Wildcard path *",false)>]
    [<TestCase(@"\\servername\temp?","Wildcard path ?",false)>]
    let IsValidPathTests (path:string) (description:string) (expected:bool) =
        let actual = IsValidPath path
        Assert.AreEqual(expected,actual,description)

    

    