namespace DriverTool.Library

//Source: https://nbevans.wordpress.com/2015/04/15/super-skinny-xml-document-generation-with-f/

module XmlToolKit =
    
    open System.Text
    open System.Xml
    open System.Xml.Linq
    open System.IO
 
    let XDeclaration version encoding standalone = XDeclaration(version, encoding, standalone)
    let XLocalName localName namespaceName = XName.Get(localName, namespaceName)
    let XName expandedName = XName.Get(expandedName)
    let XDocument xdecl content = XDocument(xdecl, content |> Seq.map (fun v -> v :> obj) |> Seq.toArray)
    let XComment (value:string) = XComment(value) :> obj
    let XElementNS localName namespaceName content = XElement(XLocalName localName namespaceName, content |> Seq.map (fun v -> v :> obj) |> Seq.toArray) :> obj
    let XElement expandedName content = XElement(XName expandedName, content |> Seq.map (fun v -> v :> obj) |> Seq.toArray) :> obj
    let XAttributeNS localName namespaceName value = XAttribute(XLocalName localName namespaceName, value) :> obj
    let XAttribute expandedName value = XAttribute(XName expandedName, value) :> obj
 
    type XDocument with
        /// Saves the XML document to a MemoryStream using UTF-8 encoding, indentation and character checking.
        member doc.Save() =
            let ms = new MemoryStream()
            use xtw = XmlWriter.Create(ms, XmlWriterSettings(Encoding = Encoding.UTF8, Indent = true, CheckCharacters = true))
            doc.Save(xtw)
            ms.Position <- 0L
            ms

