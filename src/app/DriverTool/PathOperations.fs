namespace DriverTool
module PathOperations =
    let combine2Paths (path1, path2) =
        Path.create (System.IO.Path.Combine(path1, path2))
    
    let combine3Paths (path1, path2, path3) =
        Path.create (System.IO.Path.Combine(path1, path2, path3))
    
    let combine4Paths (path1, path2, path3, path4) =
        Path.create (System.IO.Path.Combine(path1, path2, path3, path4))
    
    let getTempPath = 
        System.IO.Path.GetTempPath()
        
    let getTempFile fileName =
        System.IO.Path.Combine(getTempPath,fileName)

    