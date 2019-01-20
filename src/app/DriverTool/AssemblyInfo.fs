namespace DriverTool

open System.Reflection
open System.Runtime.CompilerServices
open System.Runtime.InteropServices

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[<assembly: AssemblyTitle("DriverTool")>]
[<assembly: AssemblyName("DriverTool")>]
[<assembly: AssemblyProduct("DriverTool")>]
[<assembly: AssemblyCompany("<github.com/trondr>")>]
[<assembly: AssemblyCopyright("Copyright © <github.com/trondr> 2018-2019")>]
[<assembly: AssemblyTrademark("")>]
[<assembly: AssemblyDescription("Downloads drivers and software for a specific PC model and creates a driver package that can be imported into SCCM as a package or application.")>]
#if DEBUG
[<assembly: AssemblyConfiguration("Debug")>]
#else
[<assembly: AssemblyConfiguration("Release")>]
#endif
[<assembly: AssemblyCulture("")>]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[<assembly: ComVisible(false)>]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[<assembly: Guid("19822aea-c088-455d-b5a5-4738a3a9dba7")>]

// Version information for an assembly consists of the following four values:
//
//       Major Version
//       Minor Version
//       Build Number
//       Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [<assembly: AssemblyVersion("1.0.*")>]
[<assembly: AssemblyVersion("1.0.0.7")>]
[<assembly: AssemblyFileVersion("1.0.0.7")>]
do
    ()