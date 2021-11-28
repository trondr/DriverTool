namespace DriverTool.Tests

open NUnit.Framework

[<TestFixture>]
[<Category(TestCategory.UnitTests)>]
module RegistryOperationTests =
    open DriverTool
    open Microsoft.Win32
    open System.IO
    open DriverTool.Library.RegistryOperations

    [<Test>]
    [<TestCase(@"HKLM\Software","HKEY_LOCAL_MACHINE","Software")>]
    [<TestCase(@"HKLM\Software\Test","HKEY_LOCAL_MACHINE","Software\Test")>]
    [<TestCase(@"HKCU\Software\Test","HKEY_CURRENT_USER","Software\Test")>]
    let parseRegKeyPathTests (regKeypath,expectedHive,expectedSubKeyPath) =
        let (hive, subKeyPath) = parseRegKeyPath regKeypath
        Assert.AreEqual(expectedHive,hive.ToString(),"Hive not equal.")
        Assert.AreEqual(expectedSubKeyPath,subKeyPath,"Sub key path not equal.")

    [<Test>]
    [<TestCase(@"HKLM\Software",true)>]
    [<TestCase(@"HKLM\Software\TestGurba123",false)>]
    [<TestCase(@"HKCU\Software\Microsoft",true)>]
    let regKeyExistsTests (regKeyPath, expectedExists) =
        let actual = regKeyExists regKeyPath
        Assert.AreEqual(expectedExists,actual)
    [<Test>]
    [<TestCase(@"HKCU\Software\MyTestCompany123")>]
    let createRegKeyTest (regKeyPath) =
        deleteRegKey regKeyPath
        Assert.IsFalse(regKeyExists regKeyPath,"Registry path exists:" + regKeyPath)
        use regkey = (createRegKey regKeyPath)
        Assert.IsTrue(regKeyExists regKeyPath,"Registry path does not exist:" + regKeyPath)
        deleteRegKey regKeyPath
        Assert.IsFalse(regKeyExists regKeyPath,"Registry path exists:" + regKeyPath)

    [<Test>]
    [<TestCase(true,100000)>]
    [<TestCase(false,100)>]
    let getRegistrySubKeyPathsTest (recursive,max) =
        let regKeyPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion"
        let actual = 
            getRegistrySubKeyPaths regKeyPath recursive
            |> Seq.map (fun p -> System.Console.WriteLine(p))
            |> Seq.toArray
        Assert.IsTrue(actual.Length > 0 && actual.Length < max,"Number of sub keys was 0 or higher than " + max.ToString() + ". Actual: " + actual.Length.ToString() )


    type RegKeyExistsOutCome = True=1|False=2|Error=3

    open System
    [<Test>]
    [<TestCase(@"HKEY_CURRENT_USER\Software",RegKeyExistsOutCome.True,"",true)>]
    [<TestCase(@"HKEY_CURRENT_USER\Software",RegKeyExistsOutCome.False,"",false)>]
    [<TestCase(@"HKEY_CURRENT_USER\Software",RegKeyExistsOutCome.Error,"Failed to check registry key exists",false)>]
    let regKeyExistsBaseTest (regKeyPath,outCome:RegKeyExistsOutCome,errorMessage,expected) =
        let regKeyOpenStub (regKeyPath, writeable) =
            match outCome with
            |RegKeyExistsOutCome.True -> Result.Ok (openRegKeyUnsafe (regKeyPath,writeable))
            |RegKeyExistsOutCome.False -> Result.Ok null
            |RegKeyExistsOutCome.Error -> Result.Error (new System.Exception(errorMessage))
            | _ -> raise(new System.Exception("Invalid test input. Only one of the values [True|False|Error] are supported"))
        
        let logWriteStub message = 
            Assert.AreEqual(sprintf "Failed to open registry key [%s] due to: %s" regKeyPath errorMessage, message)

        let actual = regKeyExistsBase regKeyOpenStub (regKeyPath) true logWriteStub
        Assert.AreEqual(expected,actual)
        ()
    
    type RegValueExistsOutCome = True=1|False=2

    [<Test>]
    [<TestCase(@"HKEY_CURRENT_USER\Environment","TEMP",RegKeyExistsOutCome.True,RegValueExistsOutCome.True,"",true,TestName ="KeyExistsValueExists_True")>]
    [<TestCase(@"HKEY_CURRENT_USER\Environment","TEMP_NOTEXISTS",RegKeyExistsOutCome.False,RegValueExistsOutCome.True,"",false,TestName ="KeyNotExistsValueExists_False")>]
    [<TestCase(@"HKEY_CURRENT_USER\Environment","TEMP_SOME_ERROR",RegKeyExistsOutCome.Error,RegValueExistsOutCome.True,"Failed to check registry key exists",false,TestName ="KeyErrorValueExists_False")>]

    [<TestCase(@"HKEY_CURRENT_USER\Environment","TEMP",RegKeyExistsOutCome.True,RegValueExistsOutCome.False,"",false,TestName ="KeyExistsValueNotExists_False")>]
    [<TestCase(@"HKEY_CURRENT_USER\Environment","TEMP_NOTEXISTS",RegKeyExistsOutCome.False,RegValueExistsOutCome.False,"",false,TestName ="KeyNotExistsValueNotExists_False")>]
    [<TestCase(@"HKEY_CURRENT_USER\Environment","TEMP_SOME_ERROR",RegKeyExistsOutCome.Error,RegValueExistsOutCome.False,"Failed to check registry key exists",false,TestName ="KeyErrorValueNotExists_False")>]

    let regKeyValueExistsBaseTest (regKeyPath,valueName,regKeyExistsOutCome:RegKeyExistsOutCome,regValueExistsOutCome:RegValueExistsOutCome,errorMessage,expected) =
        let regKeyOpenStub (regKeyPath, writeable) =
            match regKeyExistsOutCome with
            |RegKeyExistsOutCome.True -> Result.Ok (openRegKeyUnsafe (regKeyPath,writeable))
            |RegKeyExistsOutCome.False -> Result.Ok null
            |RegKeyExistsOutCome.Error -> Result.Error (new System.Exception(errorMessage))
            | _ -> raise(new System.Exception("Invalid test input. Only one of the values [True|False|Error] are supported"))
        
        let getRegKeyValueStub (regKey,valueName) : option<obj> =
            Assert.IsTrue(regKey <> null,"regKey was null")
            match regValueExistsOutCome with
            |RegValueExistsOutCome.True -> Some ("SomeValue":>obj)
            |RegValueExistsOutCome.False -> None
            | _ -> raise(new System.Exception("Invalid test input. Only one of the values [True|False] are supported"))

        let logWriteStub message = 
            Assert.AreEqual(sprintf "Failed to open registry key [%s] due to: %s" regKeyPath errorMessage,message)

        let actual = regValueExistsBase regKeyOpenStub getRegKeyValueStub (regKeyPath) valueName true logWriteStub
        Assert.AreEqual(expected,actual)
        ()
     
    [<Test>]
    [<TestCase(@"HKEY_CURRENT_USER\TestKey123",true,"TestValueName","Value1","Value1",true,TestName="ValueIsEqual")>]
    [<TestCase(@"HKEY_CURRENT_USER\TestKey123",true,"TestValueName","Value1","Value2",false,TestName="ValueIsUnEqual")>]
    [<TestCase(@"HKEY_CURRENT_USER\TestKey123",true,"TestValueName",null,"Value2",false,TestName="ValueIsNull")>]

    [<TestCase(@"HKEY_CURRENT_USER\TestKey123",false,"TestValueName","Value1","Value1",false,TestName="ValueIsEqual")>]
    [<TestCase(@"HKEY_CURRENT_USER\TestKey123",false,"TestValueName","Value1","Value2",false,TestName="ValueIsUnEqual")>]
    [<TestCase(@"HKEY_CURRENT_USER\TestKey123",false,"TestValueName",null,"Value2",false,TestName="ValueIsNull")>]

    let regValueIsTest(regKeyPath:string,expectedRegKeyExists,valueName,value1,value2,expected) =
        let openRegKeyStub (regKeyPath,writeable) = 
            Assert.AreEqual(false,writeable,"writeable should be false")
            if(expectedRegKeyExists) then
                let regKey = createRegKey regKeyPath
                Assert.IsTrue(regKeyExists regKeyPath, "Registry key does not exist: " + regKeyPath)
                Some regKey
            else
                deleteRegKey regKeyPath
                Assert.IsFalse(regKeyExists regKeyPath, "Registry key exists: " + regKeyPath)
                None
        
        let getRegKeyValueStub (regKey, valueN) =
            Assert.IsTrue(regKey <> null,"RegKey is null.")
            Assert.AreEqual(valueName,valueN,"Value name is not: " + valueName)
            match value1 with
            |null -> None
            |_ -> Some value1

        let actual = regValueIsBase openRegKeyStub getRegKeyValueStub regKeyPath valueName value2 
        Assert.AreEqual(expected,actual)
        if(regKeyExists regKeyPath) then
            deleteRegKey regKeyPath
        ()
    
    [<Test>]
    [<TestCase(@"HKEY_CURRENT_USER\TestKey123",true,"TestValueName","Value1","Value1",TestName="getRegValueTest1")>]
    [<TestCase(@"HKEY_CURRENT_USER\TestKey123",true,"TestValueName","Value2","Value2",TestName="getRegValueTest2")>]
    [<TestCase(@"HKEY_CURRENT_USER\TestKey123",true,"TestValueName","Value3","Value3",TestName="getRegValueTest3")>]
    [<TestCase(@"HKEY_CURRENT_USER\TestKey123",true,"TestValueName",null,null,TestName="getRegValueTest4")>]

    [<TestCase(@"HKEY_CURRENT_USER\TestKey123",false,"TestValueName","Value1",null,TestName="getRegValueTest5")>]
    [<TestCase(@"HKEY_CURRENT_USER\TestKey123",false,"TestValueName","Value2",null,TestName="getRegValueTest6")>]
    [<TestCase(@"HKEY_CURRENT_USER\TestKey123",false,"TestValueName","Value2",null,TestName="getRegValueTest7")>]
    let getRegValueTest (regKeyPath:string,expectedRegKeyExists,valueName,value1:obj,expected:obj) =
        let openRegKeyStub (regKeyPath,writeable) = 
            Assert.AreEqual(false,writeable,"writeable should be false")
            if(expectedRegKeyExists) then
                let regKey = createRegKey regKeyPath
                Assert.IsTrue(regKeyExists regKeyPath, "Registry key does not exist: " + regKeyPath)
                Some regKey
            else
                deleteRegKey regKeyPath
                Assert.IsFalse(regKeyExists regKeyPath, "Registry key exists: " + regKeyPath)
                None
        
        let getRegKeyValueStub (regKey, valueN) =
            Assert.IsTrue(regKey <> null,"RegKey is null.")
            Assert.AreEqual(valueName,valueN,"Value name is not: " + valueName)
            match value1 with
            |null -> None
            |_ -> Some value1

        let actual = getRegValueBase openRegKeyStub getRegKeyValueStub regKeyPath valueName
        match actual with
        |Some av -> Assert.AreEqual(expected,av)
        |None -> Assert.AreEqual(expected,null)

    
    [<Test>]
    let deleteRegKeyTest() =
        
        //Setup
        let regKeyPath = @"HKEY_CURRENT_USER\TestKey123"
        let regSubKeyPath = regKeyPath + @"\SubKey123"
        use regKey = createRegKey regSubKeyPath
        use subRegKey = createRegKey regSubKeyPath

        Assert.IsTrue(regKeyExists regKeyPath,"Reg key does not exist: " + regKeyPath)
        Assert.IsTrue(regKeyExists regSubKeyPath,"Sub reg key does not exist: " + regSubKeyPath)

        deleteRegKey regKeyPath

        Assert.False(regKeyExists regKeyPath,"Reg key exists: " + regKeyPath)
        Assert.False(regKeyExists regSubKeyPath,"Sub reg key exists: " + regSubKeyPath)