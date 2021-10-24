#r "paket:
nuget FSharp.Core 4.7.2
nuget Fake.Core.Target
nuget Fake.DotNet.NuGet
nuget Fake.DotNet.MSBuild
nuget Fake.DotNet.Cli 5.20.4
nuget Fake.DotNet.Testing.Nunit
nuget Fake.Testing.Common
nuget NUnit.Console
//"
#load "./.fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.IO
open Fake.IO.Globbing.Operators //enables !! and globbing
open Fake.DotNet

//Properties
let binaryFolder = System.IO.Path.GetFullPath("./modules/DriverTool.PowerCLI/binary/")
let assemblyVersion =
    let majorVersion = "1"
    let minorVersion = "0"
    let now = System.DateTime.Now
    let buildVersion = sprintf "%02d%03d" (now.Year - 2000) (now.DayOfYear) //Example: 21289
    let revisionVersion = "1"
    sprintf "%s.%s.%s.%s" majorVersion minorVersion buildVersion revisionVersion //Example: 1.0.21289.1

//Functions
let cleanBinaryDirectory () =
    Trace.trace "Clean binary folder..."
    [
        System.IO.Path.Combine(binaryFolder,"DriverTool.PowerCLI.Library.CSharp");
        System.IO.Path.Combine(binaryFolder,"DriverTool.PowerCLI.Library.FSharp");
        System.IO.Path.Combine(binaryFolder,"DriverTool.PowerCLI.Library.Tests");
    ] |> Shell.deleteDirs

let cleanBuildDirectories () = 
    Trace.trace "Clean build output folders..."
    let dirs = 
        [
            System.IO.Path.GetFullPath("./library/.vs");
            System.IO.Path.GetFullPath("./library/DriverTool.PowerCLI.Library.CSharp/bin");
            System.IO.Path.GetFullPath("./library/DriverTool.PowerCLI.Library.CSharp/obj");
            System.IO.Path.GetFullPath("./library/DriverTool.PowerCLI.Library.FSharp/bin");
            System.IO.Path.GetFullPath("./library/DriverTool.PowerCLI.Library.FSharp/obj");
            System.IO.Path.GetFullPath("./library/DriverTool.PowerCLI.Library.Tests/bin");
            System.IO.Path.GetFullPath("./library/DriverTool.PowerCLI.Library.Tests/obj");
        ]
    dirs|> Shell.deleteDirs
    let files =
        [
            System.IO.Path.GetFullPath("./library/DriverTool.PowerCLI.Library.v3.ncrunchsolution.user");
        ]
    files |> Fake.IO.File.deleteAll

let cleanPackagesDirectory () =
    [System.IO.Path.GetFullPath("./packages/")]|>Shell.deleteDirs

let trimModule () =
    [System.IO.Path.Combine(binaryFolder,"DriverTool.PowerCLI.Library.Tests")]|> Shell.deleteDirs

//Targets
Target.create "Clean" (fun _ -> 
    Trace.trace "Clean"
    cleanBinaryDirectory ()
    cleanBuildDirectories ()    
)

Target.create "RestorePackages" (fun _ ->
    Trace.trace "Restoring packages..."
    "./library/DriverTool.PowerCLI.Library.sln"
    |> Fake.DotNet.NuGet.Restore.RestoreMSSolutionPackages (fun p -> 
            {p with Retries = 4}
        )
)

Target.create "BuildLibraries" (fun _ ->
    Trace.trace "Building libraries..."
    
    Trace.trace "Building CSharp library..."
    !! "library/DriverTool.PowerCLI.Library.CSharp/DriverTool.PowerCLI.Library.CSharp.csproj"
    |> Fake.DotNet.MSBuild.runRelease id (System.IO.Path.Combine(binaryFolder,"DriverTool.PowerCLI.Library.CSharp")) "Build"
    |> Trace.logItems "BuildLibraries-Output: "

    Trace.trace "Building FSharp library..."
    !! "library/DriverTool.PowerCLI.Library.FSharp/DriverTool.PowerCLI.Library.FSharp.fsproj"
    |> Fake.DotNet.MSBuild.runRelease id (System.IO.Path.Combine(binaryFolder,"DriverTool.PowerCLI.Library.FSharp")) "Build"
    |> Trace.logItems "BuildLibraries-Output: "
)

Target.create "BuildDocumentation" (fun _ ->
    Trace.trace "Building documentation..."

    CreateProcess.fromRawCommand "./tools/XmlDoc2CmdletDoc/XmlDoc2CmdletDoc.exe" ["-strict";System.IO.Path.Combine(binaryFolder,"DriverTool.PowerCLI.Library.CSharp","DriverTool.PowerCLI.Library.CSharp.dll")]
    |> CreateProcess.withWorkingDirectory (System.IO.Path.Combine(binaryFolder,"DriverTool.PowerCLI.Library.CSharp"))
    |> Proc.run
    |> ignore

    CreateProcess.fromRawCommand "./tools/XmlDoc2CmdletDoc/XmlDoc2CmdletDoc.exe" ["-strict";System.IO.Path.Combine(binaryFolder,"DriverTool.PowerCLI.Library.FSharp","DriverTool.PowerCLI.Library.FSharp.dll")]
    |> CreateProcess.withWorkingDirectory (System.IO.Path.Combine(binaryFolder,"DriverTool.PowerCLI.Library.FSharp"))
    |> Proc.run
    |> ignore
)

Target.create "RunTests" (fun _ ->
    Trace.trace "Running tests..."    
    Fake.DotNet.DotNet.test (fun p -> {
        p with
            NoBuild = false
            Configuration = DotNet.BuildConfiguration.Release
        }) "library/DriverTool.PowerCLI.Library.Tests/DriverTool.PowerCLI.Library.Tests.fsproj"
)

Target.create "BuildModule" (fun _ ->
    Trace.trace ("Building module...")
    trimModule()
    cleanBuildDirectories()    
)

Target.create "BuildTemplate" (fun _ ->
    Trace.trace ("Building template...")
    cleanPackagesDirectory()
)

Target.create "Default" (fun _ ->
    Trace.trace ("DriverTool.PowerCLI" + "." + assemblyVersion)
)

//Dependencies
open Fake.Core.TargetOperators

"Clean"    
    ==> "BuildTemplate"

"Clean"
     ==> "RestorePackages"
     ==> "BuildLibraries"
     ==> "RunTests"
     ==> "BuildDocumentation"
     ==> "BuildModule"

"Clean"
    ==> "RestorePackages"
    ==> "BuildLibraries"
    ==> "RunTests"
    ==> "BuildDocumentation"
    ==> "Default"

//Start build
Target.runOrDefault "Default"