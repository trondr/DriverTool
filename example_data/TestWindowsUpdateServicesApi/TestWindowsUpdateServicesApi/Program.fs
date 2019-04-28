// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open Microsoft.UpdateServices.Administration

[<EntryPoint>]
let main argv = 
    
    let updateFileName = @"C:\Temp\DriverToolCache\HpCatalogForSms.latest\V2\00004850-0000-0000-5350-000000092797.sdp"

    let loadSoftwarePackage (fileName:string) =
        let softwareDistributionPackage = new SoftwareDistributionPackage(fileName)
        printfn "Description: %s" softwareDistributionPackage.Description
        printfn "Installed?: %s" softwareDistributionPackage.IsInstallable

    loadSoftwarePackage updateFileName
    
    printfn "%A" argv
    0 // return an integer exit code
