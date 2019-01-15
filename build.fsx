#r "paket:
nuget Fake.IO.FileSystem
nuget Fake.DotNet.MSBuild
nuget Fake.DotNet.Testing.Nunit
nuget Fake.Testing.Common
nuget Fake.Core.Target //"
#load "./.fake/build.fsx/intellisense.fsx"

open Fake.IO
open Fake.IO.Globbing.Operators //enables !! and globbing
open Fake.DotNet
open Fake.Core
open Fake.Testing
open Fake.DotNet.Testing


//Properties
let buildFolder = System.IO.Path.GetFullPath("./build/")
let buildAppFolder = buildFolder + "app"
let buildTestFolder = buildFolder + "test"
let artifactFolder = System.IO.Path.GetFullPath("./artifact/")
let artifactAppFolder = artifactFolder + "app"

//Targets
Target.create "Clean" (fun _ ->
    Trace.trace "Clean build folder..."
    Shell.cleanDirs [ buildFolder; artifactFolder ]
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

let nunitConsoleRunner =
    let consoleRunner = 
        !! "packages/**/nunit3-console.exe"
        |> Seq.head
    printfn "Console runner:  %s" consoleRunner
    consoleRunner

Target.create "Test" (fun _ -> 
    Trace.trace "Testing app..."    
    !! ("build/test/**/*.Tests.dll")    
    |> NUnit3.run (fun p ->
        {p with ToolPath = nunitConsoleRunner})            
)

let runDotNetPublish projectFilePath outputPath = 
    let result =
        Process.execWithResult (fun info ->
            { info with
                FileName = "dotnet.exe"
                WorkingDirectory = "."
                Arguments = "publish --framework netcoreapp2.0 --runtime win-x64 --output " + outputPath + " " + projectFilePath
            }) (System.TimeSpan.FromMinutes 15.)
    result

Target.create "Publish" (fun _ ->
    Trace.trace "Publishing app..."
    let projectFilePaths = !! ("src/app/**/*.fsproj")    
    for projectFilePath in projectFilePaths do
        let result = runDotNetPublish projectFilePath artifactAppFolder
        printfn "'dotnet.exe publish' exit code: %d" result.ExitCode
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
    ==> "Publish"
    ==> "Default"

//Start build
Target.runOrDefault "Default"