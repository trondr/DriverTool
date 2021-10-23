#r "paket:
nuget FSharp.Core 4.7.0
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
    let revisionVersion = "62"
    sprintf "%s.%s.%s.%s" majorVersion minorVersion buildVersion revisionVersion //Example: 1.0.19063.1

let getVersion file = 
    System.Reflection.AssemblyName.GetAssemblyName(file).Version.ToString()

//Targets
Target.create "Clean" (fun _ ->
    Trace.trace "Clean build folder..."
    let folders =
        [ 
            buildFolder; 
            artifactFolder;
            System.IO.Path.GetFullPath("./src/app/DriverTool/bin");
            System.IO.Path.GetFullPath("./src/app/DriverTool/obj");
            System.IO.Path.GetFullPath("./src/app/DriverTool.CSharpLib/bin");
            System.IO.Path.GetFullPath("./src/app/DriverTool.CSharpLib/obj");
            System.IO.Path.GetFullPath("./src/app/DriverTool.DpInstExitCode2ExitCode/bin");
            System.IO.Path.GetFullPath("./src/app/DriverTool.DpInstExitCode2ExitCode/obj");
            System.IO.Path.GetFullPath("./src/app/DriverTool.DupExitCode2ExitCode/bin");
            System.IO.Path.GetFullPath("./src/app/DriverTool.DupExitCode2ExitCode/obj");
            System.IO.Path.GetFullPath("./src/app/DriverTool.Library/bin");
            System.IO.Path.GetFullPath("./src/app/DriverTool.Library/obj");
            System.IO.Path.GetFullPath("./src/app/DriverTool.UI/bin");
            System.IO.Path.GetFullPath("./src/app/DriverTool.UI/obj");
            System.IO.Path.GetFullPath("./src/test/DriverTool.Tests/bin");
            System.IO.Path.GetFullPath("./src/test/DriverTool.Tests/obj");
            ]
    folders |> Shell.cleanDirs 
)

Target.create "RestorePackages" (fun _ ->
     "./DriverTool.sln"
     |> Fake.DotNet.NuGet.Restore.RestoreMSSolutionPackages (fun p ->
         { p with             
             Retries = 4 })
   )

Target.create "BuildApp" (fun _ -> 
    Trace.trace "Building app..."
    let now2 = System.DateTime.Now
    let year = now2.Year.ToString()
    AssemblyInfoFile.createFSharp "./src/app/DriverTool/AssemblyInfo.fs"
        [
            AssemblyInfo.Title "DriverTool"
            AssemblyInfo.Description "Download drivers and software for current PC model and create a driver package that can be imported into SCCM as a package or application." 
            AssemblyInfo.Product "DriverTool"
            AssemblyInfo.Company "github/trondr"
            AssemblyInfo.Copyright (sprintf "Copyright \u00A9 github/trondr 2018-%s" year )
            AssemblyInfo.Version assemblyVersion
            AssemblyInfo.FileVersion assemblyVersion                        
            AssemblyInfo.ComVisible false
            AssemblyInfo.Guid "19822aea-c088-455d-b5a5-4738a3a9dba7"
            AssemblyInfo.InternalsVisibleTo "DriverTool.Tests"
        ]
    
    AssemblyInfoFile.createFSharp "./src/app/DriverTool.Library/AssemblyInfo.fs"
        [
            AssemblyInfo.Title "DriverTool.Library"
            AssemblyInfo.Description "Library providing common DriverTool functionality."
            AssemblyInfo.Product "DriverTool.Library"
            AssemblyInfo.Company "github/trondr"
            AssemblyInfo.Copyright (sprintf "Copyright \u00A9 github/trondr 2018-%s" year )
            AssemblyInfo.Version assemblyVersion
            AssemblyInfo.FileVersion assemblyVersion                        
            AssemblyInfo.ComVisible false
            AssemblyInfo.Guid "19822aea-c088-455d-b5a5-4738a3a9dba8"
            AssemblyInfo.InternalsVisibleTo "DriverTool.Tests"
        ]

    !! "src/app/**/DriverTool.DpInstExitCode2ExitCode.fsproj"
    |> MSBuild.runRelease id buildAppFolder "Build"
    |> Trace.logItems "BuildApp-Output: "

    !! "src/app/**/DriverTool.DupExitCode2ExitCode.fsproj"
    |> MSBuild.runRelease id buildAppFolder "Build"
    |> Trace.logItems "BuildApp-Output: "

    !! "src/app/**/DriverTool.CSharpLib.csproj"
    |> MSBuild.runRelease id buildAppFolder "Build"
    |> Trace.logItems "BuildApp-Output: "

    !! "src/app/**/DriverTool.Library.fsproj"
        |> MSBuild.runRelease id buildAppFolder "Build"
        |> Trace.logItems "BuildApp-Output: "

    !! "src/app/**/DriverTool.UI.csproj"
    |> MSBuild.runRelease id buildAppFolder "Build"
    |> Trace.logItems "BuildApp-Output: "

    !! "src/app/**/DriverTool.fsproj"
    |> MSBuild.runRelease id buildAppFolder "Build"
    |> Trace.logItems "BuildApp-Output: "
)

Target.create "BuildTest" (fun _ -> 
    Trace.trace "Building test..."
    !! "src/test/**/DriverTool.Tests.fsproj"
        |> MSBuild.runRelease id buildTestFolder "Build"
        |> Trace.logItems "BuildTest-Output: "
)

let nunitConsoleRunner() =
    let consoleRunner = 
        !! "packages/**/nunit3-console.exe"
        |> Seq.head
    printfn "Console runner:  %s" consoleRunner
    consoleRunner

Target.create "Test" (fun _ -> 
    Trace.trace "Testing app..."    
    !! ("build/test/**/*.Tests.dll")    
    |> NUnit3.run (fun p ->
        {p with ToolPath = nunitConsoleRunner();Where = "cat==UnitTests";TraceLevel=NUnit3.NUnit3TraceLevel.Off})
)

Target.create "Publish" (fun _ ->
    Trace.trace "Publishing app..."
    let assemblyVersion = getVersion (System.IO.Path.Combine(buildAppFolder,"DriverTool.exe"))
    let files = !!("build/app/**/*")        
    let zipFile = System.IO.Path.Combine(artifactFolder,sprintf "DriverTool.%s.zip" assemblyVersion)
    files
    |> Fake.IO.Zip.createZip buildAppFolder zipFile (sprintf "DriverTool %s" assemblyVersion) 9 false 
    Trace.trace (sprintf "Published: %s" zipFile)
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