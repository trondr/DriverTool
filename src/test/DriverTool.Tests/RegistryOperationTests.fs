﻿namespace DriverTool.Tests

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