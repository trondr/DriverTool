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
        }

    let getAttribute (xElement:XElement, attributeName:string) =        
        xElement.Attribute(xnu attributeName).Value

    let toSupportedOperatingSystems (dp:XElement) =
        dp
            .Descendants(xn "SupportedOperatingSystems")
            .Descendants(xn "OperatingSystem")
                |>Seq.map(fun os -> 
                        let osCode = (getAttribute (os, "osCode"))
                        let osArch = (getAttribute (os,"osArch"))
                        {
                            OsCode = osCode
                            OsArch = osArch
                        }
                        )
                |>Seq.toArray

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

    let toDriverPackage (dp:XElement) : Result<DriverPackage,Exception> =
        result{
            let! name = getElementValue dp (xn "Name")
            let! models = toModels dp
            let operatingSystems = toSupportedOperatingSystems dp                                    
            let! driverPackage = Result.Ok {
                Name = name.Trim()
                Models = models |> Seq.toArray
                OperatinSystems = operatingSystems
            }
            return driverPackage
        }

    let loadCatalog (catalogPath:FileSystem.Path) : Result<DriverPackage[],Exception> =
        result{
            let! xDocument = XmlHelper.loadXDocument catalogPath
            let! driverPackages = 
                xDocument |> getXDocumentDescendants (xn "DriverPackage")
                |>Seq.map toDriverPackage
                |>toAccumulatedResult
            return driverPackages |> Seq.toArray
        }
        
        
        
        
       