namespace DriverTool

module LenovoCatalogv2 =
    open System
    open DriverTool
    open DriverTool.Library
    open DriverTool.Library.PackageXml
    open DriverTool.LenovoCatalogXml
    open DriverTool.LenovoCatalog
    open DriverTool.Library.F
    open DriverTool.Library.F0

    
    let getLocalLenvoCatalogv2XmlFilePath cacheFolderPath =
        FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue cacheFolderPath ,"LenovoCatalogv2.xml"))

    let downloadCatalogv2 cacheFolderPath =
        result {
            let! destinationFile = getLocalLenvoCatalogv2XmlFilePath cacheFolderPath
            let! downloadResult = Web.downloadFile (new Uri("https://download.lenovo.com/cdrt/td/catalogv2.xml"), true, destinationFile)
            return downloadResult
        }

    let getSccmPackageInfosFromLenovoCataloModels (lenovoCatalogModels:seq<LenovoCatalogModel>) =
        let products =
            lenovoCatalogModels
            |>Seq.map(fun m -> 
                        
                        m.SccmDriverPacks |> Seq.map(fun dp ->
                            {
                                Model=Some m.Name;
                                Os="win10";
                                OsBuild=Some dp.Version;
                                Name=m.Name;
                                SccmDriverPackUrl= Some dp.Url                                    
                                ModelCodes = (m.ModelTypes |> Seq.map (fun (ModelType mt)-> mt)|> Seq.toArray)
                            }
                          )
                )
            |>Seq.concat
            |>Seq.toArray
        products

    /// Download and parse catalogv2.xml from Lenovo web site. The file catalogv2.xml contains sccm driver package download information for each model and os build.
    let getSccmPackageInfosv2 cacheFolderPath =
        result
            {
                let! catalogXmlPath = downloadCatalogv2 cacheFolderPath
                let! lenovoCatalogModels = DriverTool.LenovoCatalogXml.loadLenovoCatalogv2 catalogXmlPath
                let products = getSccmPackageInfosFromLenovoCataloModels lenovoCatalogModels
                return products
            }
