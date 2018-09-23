namespace DriverTool

open System.Reflection
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
[<assembly: AssemblyCompany("github.com/trondr")>]
[<assembly: AssemblyProduct("DriverTool")>]
[<assembly: AssemblyName("DriverTool")>]
[<assembly: AssemblyCopyright("Copyright © <github.com/trondr> 2018")>]
[<assembly: AssemblyTrademark("Trademark ™")>]
[<assembly: AssemblyDescription("Driver tool downloads software and drivers for a specific PC model and creates a driver packages that can be installed via SCCM.")>]
#if DEBUG
[<assembly: AssemblyConfiguration("Debug")>]
#else
[<assembly: AssemblyConfiguration("Release")>]
#endif
[<assembly: ComVisible(false)>]
[<assembly: AssemblyVersion("1.0.0.0")>]
[<assembly: AssemblyFileVersion("1.0.0.0")>]
()