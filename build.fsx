#r "paket:
nuget Fake.IO.FileSystem
nuget Fake.DotNet.AssemblyInfoFile
nuget Fake.DotNet.MSBuild
nuget Fake.DotNet.Testing.Nunit
nuget Fake.Testing.Common
nuget Fake.DotNet.NuGet
nuget Fake.IO.Zip
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

let assemblyVersion =
    let majorVersion = "1"
    let minorVersion = "0"
    let now = System.DateTime.Now    
    let buildVersion = sprintf "%02d%03d" (now.Year - 2000) (now.DayOfYear) //Example: 19063
    let revisionVersion = "1"
    sprintf "%s.%s.%s.%s" majorVersion minorVersion buildVersion revisionVersion //Example: 1.0.19063.1

let getVersion file = 
    System.Reflection.AssemblyName.GetAssemblyName(file).Version.ToString()

//Targets
Target.create "Clean" (fun _ ->
    Trace.trace "Clean build folder..."
    Shell.cleanDirs [ buildFolder; artifactFolder ]
)

Target.create "RestorePackages" (fun _ ->
     "./DriverTool.sln"
     |> Fake.DotNet.NuGet.Restore.RestoreMSSolutionPackages (fun p ->
         { p with             
             Retries = 4 })
   )

Target.create "BuildApp" (fun _ -> 
    Trace.trace "Building app..."
    AssemblyInfoFile.createFSharp "./src/app/DriverTool/AssemblyInfo.fs"
        [
            AssemblyInfo.Title "DriverTool"
            AssemblyInfo.Description "Download drivers and software for a specific PC model and create a driver package that can be imported into SCCM as a package or application." 
            AssemblyInfo.Product "DriverTool"
            AssemblyInfo.Company "github/trondr"
            AssemblyInfo.Copyright "Copyright \u00A9 github/trondr 2018-2019"
            AssemblyInfo.Version assemblyVersion
            AssemblyInfo.FileVersion assemblyVersion                        
            AssemblyInfo.ComVisible false
            AssemblyInfo.Guid "19822aea-c088-455d-b5a5-4738a3a9dba7"
            AssemblyInfo.InternalsVisibleTo "DriverTool.Tests"
        ]
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
        {p with ToolPath = nunitConsoleRunner;Where = "cat==UnitTests";TraceLevel=NUnit3.NUnit3TraceLevel.Verbose})
)

Target.create "Publish" (fun _ ->
    Trace.trace "Publishing app..."
    let assemblyVersion = getVersion (System.IO.Path.Combine(buildAppFolder,"DriverTool.exe"))
    let files = 
        [|
            System.IO.Path.Combine(buildAppFolder,"DriverTool.exe")
            System.IO.Path.Combine(buildAppFolder,"DriverTool.pdb")
            System.IO.Path.Combine(buildAppFolder,"DriverTool.exe.config")
            System.IO.Path.Combine(buildAppFolder,"FSharp.Core.dll")
        |]
    let zipFile = System.IO.Path.Combine(artifactFolder,sprintf "DriverTool.%s.zip" assemblyVersion)
    files
    |> Fake.IO.Zip.createZip buildAppFolder zipFile (sprintf "DriverTool %s" assemblyVersion) 9 false 
)

Target.create "Default" (fun _ ->
    Trace.trace "Hello world from FAKE"
)

//Dependencies
open Fake.Core.TargetOperators

"Clean" 
    ==> "RestorePackages"
    ==> "BuildApp"
    ==> "BuildTest"
    ==> "Test"
    ==> "Publish"
    ==> "Default"

//Start build
Target.runOrDefault "Default"