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
let buildAppFolder = buildFolder + "app"
let buildTestFolder = buildFolder + "test"

//Targets
Target.create "Clean" (fun _ ->
    Trace.trace "Clean build folder..."
    Shell.cleanDir buildFolder
)

Target.create "BuildApp" (fun _ -> 
    Trace.trace "Building app..."
    !! "src/app/**/*.fsproj"
        |> MSBuild.runRelease id buildAppFolder "Build"
        |> Trace.logItems "BuildApp-Output: "
)

Target.create "BuildTest" (fun _ -> 
    Trace.trace "Building test..."
    !! "src/test/**/*.fsproj"
        |> MSBuild.runRelease id buildTestFolder "Build"
        |> Trace.logItems "BuildTest-Output: "
)

let runDotNetTest libraryFilePath = 
    let result =
        Process.execWithResult (fun info ->
            { info with
                FileName = "dotnet.exe"
                WorkingDirectory = "."
                Arguments = "test " + libraryFilePath
            }) (System.TimeSpan.FromMinutes 15.)
    result

Target.create "Test" (fun _ -> 
    let testLibraries = !! ("src/test/**/*.fsproj")    
    for testLibrary in testLibraries do
        let result = runDotNetTest testLibrary
        printfn "'dotnet.exe test' exit code: %d" result.ExitCode
        for message in result.Messages do
            printfn "   %s" message
        for error in result.Errors do
            failwith (sprintf "%s" error)        
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