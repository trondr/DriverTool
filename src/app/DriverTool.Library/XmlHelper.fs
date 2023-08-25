namespace DriverTool.Library
    
    module XmlHelper =
        open System
        open System.Xml.Linq
        open DriverTool.Library.FileSystem

        ///Get XName. Allows writing: 'xElement.Element(xn "Name")' instead of: 'xElement.Element(XName.Get("Name"))'
        let xn name = 
            XName.Get(name)

        ///Get XName with namespace. Allows writing: 'xElement.Element(xnn ns "Name")' instead of: 'xElement.Element(XName.Get("Name",ns))'
        let xnn nameSpace name = 
            XName.Get(name, nameSpace)

        ///Get descendants from xDocument
        let getDocumentDescendants elementName (xDocument:XDocument)=
            xDocument.Descendants(elementName)

        ///Get descendants from xElement
        let getElementDescendants elementName (xElement:XElement)=
            xElement.Descendants(elementName)
        
        ///Get optional attribute from xElement. Example: getOptionalAttribute xElement (xn "SystemId")
        let getOptionalAttribute (xElement:XElement) attributeName =        
            match xElement with
            | null -> None
            |_ -> 
                let attribute = xElement.Attribute(attributeName)
                match attribute with
                |null -> None
                |_ -> Some attribute.Value

        ///Get required attribute from xElement. Example: getRequiredAttribute xElement (xn "SystemId")
        let getRequiredAttribute (xElement:XElement) (attributeName:XName) =
            match xElement with
            | null -> Result.Error (toException (sprintf "Element is null. Failed to get attribute name '%s'" attributeName.LocalName ) None)
            |_ -> 
                let attribute = xElement.Attribute(attributeName)            
                match attribute with
                |null -> Result.Error (toException (sprintf "Attribute '%s' not found on element: %A" attributeName.LocalName xElement) None)
                |_ -> Result.Ok attribute.Value
        
        ///Get value from xElement. Example: getElementValue xElement (xn "DisplayName")
        let getElementValue (parentXElement:XElement) elementName = 
            match parentXElement with
            | null -> Result.Error (toException (sprintf "Parent element is null. Failed to get element value '%A'" elementName) None)
            |_ -> 
                let xElemement = parentXElement.Element(elementName)
                match xElemement with
                |null -> Result.Error (toException (sprintf "Element '%A' not found on parent element: %A" elementName parentXElement) None)
                |_ -> Result.Ok xElemement.Value
        
        ///Load xml document
        let loadXDocument (xmlFilePath:Path) =
            try
                Result.Ok (XDocument.Load(FileSystem.pathValue xmlFilePath))
            with
            |ex -> Result.Error (toException (sprintf "Failed to load xml file '%A' due to: %s" xmlFilePath ex.Message) (Some ex))

        //Get root of xml document.
        let getRootElement (document:XDocument) =
            result{
                let! validDocument = nullGuard' document (nameof document) None
                let! validRootElement = nullGuard' validDocument.Root "Root" (Some $"Root element not found.")
                return validRootElement
            }            
        
        //Get child element.
        let getChildElement (name:XName) (element:XElement) =
            result{
                let! validElement = nullGuard' element (nameof element) None
                let! validName = nullGuard' name (nameof name) None
                let childElement = validElement.Element(validName)
                let! validChildElement = nullGuard' childElement validName.LocalName (Some $"Element not found: %s{validName.LocalName}")                
                return validChildElement
            }
            
        //Get optional child element.
        let getOptionalChildElement (name:XName) (element:XElement) =
            result{
                let! validElement = nullGuard' element (nameof element) None
                let! validName = nullGuard' name (nameof name) None
                let childElement = validElement.Element(validName)
                let validChildElement =
                    match childElement with
                    |Null -> None
                    |NotNull c -> Some c
                return validChildElement
            }
        