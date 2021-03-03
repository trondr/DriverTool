namespace DriverTool

module LenovoCatalogXml =
    open System
    open System.Xml.Linq    
    open DriverTool.Library.F
    open DriverTool.Library
    
    type ModelType = ModelType of string

    type Queries = 
        {
            ModelTypes:ModelType[]
            Version:string
            Smbios:string            
        }

    type DriverPack = 
        {
            Id:string
            Date:DateTime option
            Url:string
        }

    type LenovoCatalogProduct = 
        {
            Model:string;
            Family:string;
            Os:string;
            Build:string
            Name:string
            DriverPacks:DriverPack[]
            Queries:Queries
        }

    let toInt32Unsafe (number:string) = 
        System.Convert.ToInt32(number)

    let toDateTime year month day =
        match year with
        |0 -> DateTime.MinValue
        |_ -> 
            match month with
            |0 -> DateTime.MinValue
            |_ ->
                match day with
                |0 -> DateTime.MinValue
                |_ -> new DateTime(year,month,day)

    let toDate date = 
        match date with
        |None -> None
        |Some d -> 
            match d with
            |Regex @"^(\d{4})(\d{2})" [year;month] -> Some (toDateTime (toInt32Unsafe year) (toInt32Unsafe month) 1)            
            |Regex @"^(\d{4})(\d{2})\w{2}" [year;month] -> Some (toDateTime (toInt32Unsafe year) (toInt32Unsafe month) 1)
            |Regex @"^(\d{4})-(\d{2})-(\d{2})" [year;month;day] -> Some (toDateTime (toInt32Unsafe year) (toInt32Unsafe month)(toInt32Unsafe day))
            |_ -> 
                printf "Unsupported date format: %s" d
                None                
    
    let toDriverPack (driverPackXElement:XElement) =
        result{
            let! id = XmlHelper.getRequiredAttribute driverPackXElement "id"
            let date = XmlHelper.getOptionalAttribute driverPackXElement "date"
            let url = driverPackXElement.Value
            return 
                {
                    Id = id
                    Date = toDate date
                    Url = url
                }
        }

    let toDriverPacks (productXElement:XElement) =
        result{
             let driverPackElements = productXElement.Elements(XName.Get("DriverPack"))
             let! driverPacks = 
                driverPackElements 
                |> Seq.map (fun d -> toDriverPack d)
                |>toAccumulatedResult
             return driverPacks
        }

    let toModelType (typeXElement:XElement) =
        result
            {
                let modelType = typeXElement.Value
                return ModelType modelType                
            }

    let toQueries  (queriesXElement:XElement) =
        result{
            let typeElements = queriesXElement.Descendants(XName.Get("Type"))
            let! modelTypes = typeElements|>Seq.map(fun t -> toModelType t) |> toAccumulatedResult
            let! version = XmlHelper.getElementValue queriesXElement "Version"
            let! smbios = XmlHelper.getElementValue queriesXElement "Smbios"
            return
                {
                    ModelTypes=modelTypes |> Seq.toArray
                    Version = version
                    Smbios = smbios
                }
        }

    let toProduct (productXElement:XElement) = 
        result{
            let! model = XmlHelper.getRequiredAttribute productXElement "model"        
            let! family = XmlHelper.getRequiredAttribute productXElement "family"
            let! os = XmlHelper.getRequiredAttribute productXElement "os"
            let! build = XmlHelper.getRequiredAttribute productXElement "build"
            let! name = XmlHelper.getElementValue productXElement "Name"
            let! driverPacks = toDriverPacks productXElement
            let! queries = toQueries (productXElement.Element(XName.Get("Queries")))
            return {
                Model = model
                Family = family
                Os = os
                Build = build
                Name = name
                DriverPacks = driverPacks |> Seq.toArray
                Queries = queries
                }
        }

    let toProducts (xDocument:XDocument) = 
        xDocument.Descendants(XName.Get("Product"))
        |>Seq.map(fun p -> toProduct p)
        |>toAccumulatedResult
            
    let loadLenovoCatalog (lenovoCatalogXlmFilePath:FileSystem.Path) =
        result{
            let! existingCatalogXmlPath = FileOperations.ensureFileExistsWithMessage (sprintf "Lenovo catalog xml file '%A' not found." lenovoCatalogXlmFilePath) lenovoCatalogXlmFilePath
            let xDocument = XDocument.Load(FileSystem.pathValue existingCatalogXmlPath)
            let! products = toProducts xDocument                
            return products
        }

    type SccmDriverPack =
        {
            Version:string
            Url:string
            ReleaseDate:DateTime option
        }

    type LenovoCatalogModel =
        {
            Name:string
            ModelTypes:ModelType[]
            SccmDriverPacks:SccmDriverPack[]
        }

    let toSccmDriverPack (sccmXElement:XElement) =
        result{
            let! version = XmlHelper.getRequiredAttribute sccmXElement "version"
            let url = sccmXElement.Value
            return 
                {
                    Version = version                    
                    Url = url
                    ReleaseDate = None
                }
        }        
    
    let toSccmDriverPacks (modelXElement:XElement) =
        result{
             let sccmDriverPackElements = modelXElement.Elements(XName.Get("SCCM"))
             let! sccmDriverPacks = 
                sccmDriverPackElements 
                |> Seq.map (fun d -> toSccmDriverPack d)
                |>toAccumulatedResult
             return sccmDriverPacks
        }

    let toModelTypes (modelXElement:XElement) =
        result{
            let typeElements = modelXElement.Descendants(XName.Get("Type"))
            let! modelTypes = typeElements|>Seq.map(fun t -> toModelType t) |> toAccumulatedResult            
            return modelTypes            
        }

    let toModel (modelXElement:XElement) =
        result{
            let! name = XmlHelper.getRequiredAttribute modelXElement "name"
            let! modelTypes = toModelTypes modelXElement
            let! sccmDriverPacks = toSccmDriverPacks modelXElement
            return
                {
                    Name = name
                    ModelTypes = (modelTypes |> Seq.toArray)
                    SccmDriverPacks = (sccmDriverPacks |> Seq.toArray)                    
                }
        }

    let toModels (xDocument:XDocument) =
        xDocument.Descendants(XName.Get("Model"))
        |>Seq.map(fun m -> toModel m)
        |>toAccumulatedResult

    let loadLenovoCatalogv2 (lenovoCatalogv2XlmFilePath:FileSystem.Path) =
        result{
            let! existingCatalogv2XmlPath = FileOperations.ensureFileExistsWithMessage (sprintf "Lenovo catalog xml file '%A' not found." lenovoCatalogv2XlmFilePath) lenovoCatalogv2XlmFilePath
            let xDocument = XDocument.Load(FileSystem.pathValue existingCatalogv2XmlPath)
            let! products = toModels xDocument                
            return products
        }
        