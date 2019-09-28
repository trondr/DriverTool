﻿namespace DriverTool
module PathOperations =
    let combine2Paths (path1, path2) =
        FileSystem.path (System.IO.Path.Combine(path1, path2))
    
    let combine3Paths (path1, path2, path3) =
        FileSystem.path (System.IO.Path.Combine(path1, path2, path3))
    
    let getTempPath = 
        System.IO.Path.GetTempPath()

    let getRandomFileName() =
        System.IO.Path.GetRandomFileName()
        
    let getTempFile fileName =
        System.IO.Path.Combine(getTempPath,fileName)

    