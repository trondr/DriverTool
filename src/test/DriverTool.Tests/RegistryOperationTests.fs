namespace DriverTool.Tests

open NUnit.Framework

[<TestFixture>]
module RegistryOperationTests =
    open DriverTool
    open Microsoft.Win32
    open System.IO
    open DriverTool.RegistryOperations

    [<Test>]
    [<TestCase(@"HKLM\Software","HKEY_LOCAL_MACHINE","Software")>]
    [<TestCase(@"HKLM\Software\Test","HKEY_LOCAL_MACHINE","Software\Test")>]
    [<TestCase(@"HKCU\Software\Test","HKEY_CURRENT_USER","Software\Test")>]
    let parseRegKeyPathTests (regKeypath,expectedHive,expectedSubKeyPath) =
        let (hive, subKeyPath) = RegistryOperations.parseRegKeyPath regKeypath
        Assert.AreEqual(expectedHive,hive.ToString(),"Hive not equal.")
        Assert.AreEqual(expectedSubKeyPath,subKeyPath,"Sub key path not equal.")

    [<Test>]
    [<TestCase(@"HKLM\Software",true)>]
    [<TestCase(@"HKLM\Software\TestGurba123",false)>]
    [<TestCase(@"HKCU\Software\Microsoft",true)>]
    let regKeyExistsTests (regKeyPath, expectedExists) =
        let actual = RegistryOperations.regKeyExists regKeyPath
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
        let regKeyPath = @"HKEY_CURRENT_USER\Software"
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
            |RegKeyExistsOutCome.True -> Result.Ok (RegistryOperations.openRegKeyUnsafe (regKeyPath,writeable))
            |RegKeyExistsOutCome.False -> Result.Ok null
            |RegKeyExistsOutCome.Error -> Result.Error (new System.Exception(errorMessage))
            | _ -> raise(new System.Exception("Invalid test input. Only one of the values [True|False|Error] are supported"))
        
        let logWriteStub message = 
            Assert.AreEqual(String.Format("Failed to open registry key [{0}] due to: {1}",regKeyPath,errorMessage),message)

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
            |RegKeyExistsOutCome.True -> Result.Ok (RegistryOperations.openRegKeyUnsafe (regKeyPath,writeable))
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
            Assert.AreEqual(String.Format("Failed to open registry key [{0}] due to: {1}",regKeyPath,errorMessage),message)

        let actual = regValueExistsBase regKeyOpenStub getRegKeyValueStub (regKeyPath) valueName true logWriteStub
        Assert.AreEqual(expected,actual)
        ()
        