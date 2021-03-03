namespace DriverTool.Tests

module RegExpTests =
    open NUnit.Framework
    open DriverTool.Library
        
    [<Test>]
    [<Category(TestCategory.UnitTests)>]

    [<TestCase("Some software for something",true, true, true)>]
    [<TestCase("Some softare for something",false,true, true)>]
    [<TestCase("Some driver for something",false,true, true)>]
    [<TestCase("Some Driver for something",false,true, true)>]

    [<TestCase("Some BIOS for something",true,true, true)>]
    [<TestCase("Some B1OS for something",true,true, true)>]
    [<TestCase("Some BOS for something",false,true, true)>]

    [<TestCase("Some software for something",true,true, false)>]
    [<TestCase("Some softare for something",false,true, false)>]

    [<TestCase("Some software for something",false, false, true)>]

    let excludesTests (text:string, expected:bool, ignoreCase:bool,isSuccess:bool) =
        match(result
                {
                    let patterns = 
                        match isSuccess with
                        |true ->
                            [|@"Soft\ware";@"Utility";@"B.OS";"Firmware"|]
                        |false -> 
                            [|@"Soft\ware";@"Utility";@"B.OS";"Firmware";@"\!#%((()"|]
                    let! regExPatterns = RegExp.toRegexPatterns patterns ignoreCase
                    let actual = RegExp.matchAny regExPatterns text
                    Assert.AreEqual(expected,actual,text)
                    return actual
                })with
        |Ok _ -> 
            Assert.IsTrue(isSuccess)
        |Error ex -> 
            printf "%A" ex
            Assert.IsFalse(isSuccess,(sprintf "Expected success but failed with: %A" ex))