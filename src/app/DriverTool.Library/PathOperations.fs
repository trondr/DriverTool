namespace DriverTool.Library

module PathOperations =
    let combinePaths2 path1 path2 =
        FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue path1, path2))
    
    let combine2Paths (path1, path2) =
        FileSystem.path (System.IO.Path.Combine(path1, path2))
    
    let combine3Paths (path1, path2, path3) =
        FileSystem.path (System.IO.Path.Combine(path1, path2, path3))

    let combine4Paths (path1, path2, path3, path4) =
        FileSystem.path (System.IO.Path.Combine(path1, path2, path3, path4))
    
    let getTempPath = 
        System.IO.Path.GetTempPath()

    let getRandomFileName() =
        System.IO.Path.GetRandomFileName()
        
    let getTempFile fileName =
        System.IO.Path.Combine(getTempPath,fileName)

    let getFileNameFromPath path = 
        System.IO.Path.GetFileName(FileSystem.pathValue path)
     