#r "paket:
nuget FSharp.Core
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
let modulesBinaryFolder = System.IO.Path.GetFullPath("./modules/DriverTool.PowerCLI/binary/")

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
            System.IO.Path.GetFullPath("./src/app/DriverTool.PowerCLI.Library.CSharp/bin");
            System.IO.Path.GetFullPath("./src/app/DriverTool.PowerCLI.Library.CSharp/obj");
            System.IO.Path.GetFullPath("./src/app/DriverTool.PowerCLI.Library.FSharp/bin");
            System.IO.Path.GetFullPath("./src/app/DriverTool.PowerCLI.Library.FSharp/obj");                        
            System.IO.Path.GetFullPath("./src/test/DriverTool.Tests/bin");
            System.IO.Path.GetFullPath("./src/test/DriverTool.Tests/obj");
            System.IO.Path.GetFullPath("./src/test/DriverTool.PowerCLI.Library.Tests/bin");
            System.IO.Path.GetFullPath("./src/test/DriverTool.PowerCLI.Library.Tests/obj");
            System.IO.Path.GetFullPath("./modules/DriverTool.PowerCLI/internal/tools/DriverTool");
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

    Trace.trace "Building DriverTool DpInstExitCode2ExitCode..."
    !! "src/app/**/DriverTool.DpInstExitCode2ExitCode.fsproj"
        |> MSBuild.runRelease id buildAppFolder "Build"
        |> Trace.logItems "BuildApp-Output: "

    Trace.trace "Building DriverTool DupExitCode2ExitCode..."
    !! "src/app/**/DriverTool.DupExitCode2ExitCode.fsproj"
        |> MSBuild.runRelease id buildAppFolder "Build"
        |> Trace.logItems "BuildApp-Output: "

    Trace.trace "Building DriverTool CSharpLib..."
    !! "src/app/**/DriverTool.CSharpLib.csproj"
        |> MSBuild.runRelease id buildAppFolder "Build"
        |> Trace.logItems "BuildApp-Output: "

    Trace.trace "Building DriverTool Library..."
    !! "src/app/**/DriverTool.Library.fsproj"
        |> MSBuild.runRelease id buildAppFolder "Build"
        |> Trace.logItems "BuildApp-Output: "

    Trace.trace "Building DriverTool UI..."
    !! "src/app/**/DriverTool.UI.csproj"
        |> MSBuild.runRelease id buildAppFolder "Build"
        |> Trace.logItems "BuildApp-Output: "

    Trace.trace "Building DriverTool..."
    !! "src/app/**/DriverTool.fsproj"
        |> MSBuild.runRelease id buildAppFolder "Build"
        |> Trace.logItems "BuildApp-Output: "

    Trace.trace "Building PowerCLI CSharp library..."
    !! "src/app/**/DriverTool.PowerCLI.Library.CSharp.csproj"
        |> Fake.DotNet.MSBuild.runRelease id (System.IO.Path.Combine(modulesBinaryFolder,"DriverTool.PowerCLI.Library.CSharp")) "Build"
        |> Trace.logItems "BuildLibraries-Output: "

    Trace.trace "Building PowerCLI FSharp library..."
    !! "src/app/**/DriverTool.PowerCLI.Library.FSharp.fsproj"
        |> Fake.DotNet.MSBuild.runRelease id (System.IO.Path.Combine(modulesBinaryFolder,"DriverTool.PowerCLI.Library.FSharp")) "Build"
        |> Trace.logItems "BuildLibraries-Output: "
)

Target.create "BuildDocumentation" (fun _ ->
    Trace.trace "Building documentation..."

    CreateProcess.fromRawCommand "./tools/XmlDoc2CmdletDoc/XmlDoc2CmdletDoc.exe" ["-strict";System.IO.Path.Combine(modulesBinaryFolder,"DriverTool.PowerCLI.Library.CSharp","DriverTool.PowerCLI.Library.CSharp.dll")]
    |> CreateProcess.withWorkingDirectory (System.IO.Path.Combine(modulesBinaryFolder,"DriverTool.PowerCLI.Library.CSharp"))
    |> Proc.run
    |> ignore

    CreateProcess.fromRawCommand "./tools/XmlDoc2CmdletDoc/XmlDoc2CmdletDoc.exe" ["-strict";System.IO.Path.Combine(modulesBinaryFolder,"DriverTool.PowerCLI.Library.FSharp","DriverTool.PowerCLI.Library.FSharp.dll")]
    |> CreateProcess.withWorkingDirectory (System.IO.Path.Combine(modulesBinaryFolder,"DriverTool.PowerCLI.Library.FSharp"))
    |> Proc.run
    |> ignore
)

Target.create "BuildTest" (fun _ -> 
    Trace.trace "Building test..."
    !! "src/test/**/DriverTool.Tests.fsproj"
        |> MSBuild.runRelease id buildTestFolder "Build"
        |> Trace.logItems "BuildTest-Output: "

    !! "src/test/**/DriverTool.PowerCLI.Library.Tests.fsproj"
        |> MSBuild.runRelease id buildTestFolder "Build"
        |> Trace.logItems "BuildTest-Output: "

)

Target.create "Default" (fun _ ->
    Trace.trace "Hello world from FAKE"
)

//Dependencies
open Fake.Core.TargetOperators

"Clean" 
    ==> "RestorePackages"
    ==> "BuildApp"
    ==> "BuildDocumentation"
    ==> "BuildTest"
    ==> "Default"

//Start build
Target.runOrDefault "Default"