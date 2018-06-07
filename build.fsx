#r "paket:
nuget Fake.IO.FileSystem
nuget Fake.DotNet.MSBuild
nuget Fake.DotNet.Testing.Nunit
nuget Fake.Core.Target //"
#load "./.fake/build.fsx/intellisense.fsx"

open Fake.IO
open Fake.IO.Globbing.Operators //enables !! and globbing
open Fake.DotNet
open Fake.DotNet.Testing
open Fake.Core


//Properties
let buildFolder = "./build/"
let testFolder = "./test/"

//Targets
Target.create "Clean" (fun _ ->
    Trace.trace "Clean build folder..."
    Shell.cleanDirs [buildFolder; testFolder]
)

Target.create "BuildApp" (fun _ -> 
    Trace.trace "Building app..."
    !! "src/app/**/*.fsproj"
        |> MSBuild.runRelease id buildFolder "Build"
        |> Trace.logItems "BuildApp-Output: "
)

Target.create "BuildTest" (fun _ -> 
    Trace.trace "Building test..."
    !! "src/test/**/*.fsproj"
        |> MSBuild.runRelease id testFolder "Build"
        |> Trace.logItems "BuildTest-Output: "
)

let nunitConsoleRunnerPath = "E:/Dev/github.trondr/DriverTool/tools/NUnit/nunit3-console.exe"

Target.create "Test" (fun _ -> 
    !! (testFolder + "/*.Tests.dll")
        |> NUnit3.run (fun p -> 
            {p with 
                ShadowCopy = false
                ToolPath = nunitConsoleRunnerPath}
        )
)


Target.create "Default" (fun _ ->
    Trace.trace "Hello world from FAKE"
)

//Dependencies
open Fake.Core.TargetOperators

"Clean"
    ==> "BuildApp"
    ==> "BuildTest"
    ==> "Test"
    ==> "Default"

//Start build
Target.runOrDefault "Default"