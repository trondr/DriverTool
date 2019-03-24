namespace DriverTool
    
    module XmlHelper =
        open System
        open System.Xml.Linq

        let getOptionalAttribute (xElement:XElement) (attributeName:string) =        
            match xElement with
            | null -> None
            |_ -> 
                let attribute = xElement.Attribute(XName.Get(attributeName))            
                match attribute with
                |null -> None
                |_ -> Some attribute.Value

        let getRequiredAttribute (xElement:XElement) (attributeName:string) =
            match xElement with
            | null -> Result.Error (new Exception(sprintf "Element is null. Failed to get attribute name '%s'" attributeName))
            |_ -> 
                let attribute = xElement.Attribute(XName.Get(attributeName))            
                match attribute with
                |null -> Result.Error (new Exception(sprintf "Attribute '%s' not found on element: %A" attributeName xElement))
                |_ -> Result.Ok attribute.Value
        
        let getElementValue (parentXElement:XElement) (elementName:string) = 
            match parentXElement with
            | null -> Result.Error (new Exception(sprintf "Parent element is null. Failed to get element value '%s'" elementName))
            |_ -> 
                let xElemement = parentXElement.Element(XName.Get(elementName))            
                match xElemement with
                |null -> Result.Error (new Exception(sprintf "Element '%s' not found on parent element: %A" elementName parentXElement))
                |_ -> Result.Ok xElemement.Value