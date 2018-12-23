namespace DriverTool.Tests

open NUnit.Framework

[<TestFixture>]
module DellUpdatesTests =
    open DriverTool
    
    [<Test>]
    [<TestCase("FOLDER03578551M/1/Audio_Driver_D00J4_WN32_6.0.1.6102_A03.EXE","FOLDER03578551M/1","Audio_Driver_D00J4_WN32_6.0.1.6102_A03.EXE")>]
    [<TestCase("FOLDER01766254M/1/9P33_Chipset_Driver_NNGJM_WN_9.4.0.1026_A00.EXE","FOLDER01766254M/1","9P33_Chipset_Driver_NNGJM_WN_9.4.0.1026_A00.EXE")>]
    let pathToDirectoryAndFileTests (path:string,expectedDirectory,expectedFileName) =
       let (actualDirectory,actualFileName) = DriverTool.DellUpdates.pathToDirectoryAndFile path
       Assert.AreEqual(expectedDirectory,actualDirectory,"Directory not expected")
       Assert.AreEqual(expectedFileName,actualFileName,"FileName not expected")
    
    [<Test>]
    [<TestCase("07A7","WIN10X64")>]
    let getUpdates2Test (modelCodeString,operatingSystemCodeString) =
        match(result{
            let! modelCode = ModelCode.create modelCodeString false
            let! operatingSystemCode = OperatingSystemCode.create operatingSystemCodeString false
            let! actual = DriverTool.DellUpdates.getUpdates2 (modelCode, operatingSystemCode)
            Assert.IsTrue(actual.Length > 0,"PackageInfo array is empty")
            System.Console.WriteLine("Number of software components: " + actual.Length.ToString())
            actual
            |>Seq.sortBy(fun p -> p.Name)
            |>Seq.map (fun p -> System.Console.WriteLine((DriverTool.Logging.valueToString p)))
            |>Seq.toArray
            |>ignore
            return actual
        }) with
        |Ok _->Assert.IsTrue(true)
        |Error ex ->Assert.Fail(ex.Message)