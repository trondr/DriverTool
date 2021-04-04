namespace DriverTool.Library

module DellDriverPackCatalog =
    open System
    open System.Xml.Linq
    open DriverTool.Library
    open DriverTool.Library.FileSystem
    open DriverTool.Library.XmlHelper

    let ns =
        XNamespace.Get("openmanage/cm/dm").NamespaceName

    ///Get XName partial application with built in name space. Allows writing: 'xDocument.Descendants(xn "DriverPackage")' instead of: 'xDocument.Descendants(XName.Get("DriverPackage",ns))'
    let xn =
        XmlHelper.xnn ns

    ///Get XName partial application without built in name space.
    let xnu =
        XmlHelper.xn
        
    type OperatingSystem =
        {
            OsCode:string
            OsArch:string
        }

    type Model =
        {
            Name:string
            Code:string
        }

    type DriverPackage =
        {
            Name:string
            Models:Model[]
            OperatinSystems:OperatingSystem[]
            Installer:Web.WebFile
            PackageType:string
        }

    let toOperatingSystem (os:XElement) =
        result{
            let! osCode = getRequiredAttribute os (xnu "osCode")
            let! osArch = getRequiredAttribute os (xnu "osArch")
            return 
                {
                    OsCode = osCode
                    OsArch = osArch
                }
        
        }

    let toSupportedOperatingSystems (dp:XElement) =
        dp
            .Descendants(xn "SupportedOperatingSystems")
            .Descendants(xn "OperatingSystem")
                |>Seq.map toOperatingSystem
                |>Seq.toArray
                |>toAccumulatedResult

    let toModel modelXElment =
        result {
           let! systemId = getRequiredAttribute modelXElment (xnu "systemID")
           let! name = getRequiredAttribute modelXElment (xnu "name")
           return 
            {
                Name = name
                Code = systemId
            }           
        }

    let toModels (dp:XElement) =
        dp
            .Descendants(xn "SupportedSystems")
            .Descendants(xn "Brand")
            |>Seq.map(fun brand-> 
                    brand.Descendants(xn "Model")
                    |>Seq.map toModel
                    |>Seq.toArray
                )
            |>Seq.concat
            |>Seq.toArray
            |> toAccumulatedResult

    let pathToDirectoryAndFile (path:string) =        
        match path with
        |Regex @"^(.+?)([^/]+)$" [directory;file] -> 
            (directory.Trim('/'),file)
        |_ -> raise (new Exception("Failed to get directory and file path from path: "+ path ))

    let toDriverPackage baseLocationUrl (dp:XElement)  : Result<DriverPackage,Exception> =
        result{
            let! name = getElementValue dp (xn "Name")
            let! models = toModels dp
            let! operatingSystems = toSupportedOperatingSystems dp
            let! path = getRequiredAttribute dp (xnu "path")
            let installerUrl = baseLocationUrl + "/" + path;
            let! installerCheckSum = getRequiredAttribute dp (xnu "hashMD5")
            let! installerSize = getRequiredAttribute dp (xnu "size")
            let (_, installerName) = pathToDirectoryAndFile path
            let! packageType = getRequiredAttribute dp (xnu "type")
            let! driverPackage = Result.Ok {
                Name = name.Trim()
                Models = models |> Seq.toArray
                OperatinSystems = operatingSystems |>Seq.toArray
                Installer =                     
                        {
                            Url = installerUrl
                            Checksum = installerCheckSum
                            Size = installerSize |> Convert.ToInt64
                            FileName=installerName
                        }
                PackageType=packageType
            }
            return driverPackage
        }

    let loadCatalog (catalogPath:FileSystem.Path) : Result<DriverPackage[],Exception> =
        result{
            let! xDocument = XmlHelper.loadXDocument catalogPath
            let! baseLocation = getRequiredAttribute xDocument.Root (xnu "baseLocation")
            let baseLocationUrl = "http://" + baseLocation
            let! driverPackages = 
                xDocument |> getXDocumentDescendants (xn "DriverPackage")
                |>Seq.map (toDriverPackage baseLocationUrl)
                |>toAccumulatedResult
            return driverPackages |> Seq.toArray
        }
        
        
        
        
       