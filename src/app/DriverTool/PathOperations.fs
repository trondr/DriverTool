namespace DriverTool
module PathOperations =
    let combine2Paths path1 path2 =
        System.IO.Path.Combine(path1,path2)
    let combine3Paths path1 path2 path3 =
        System.IO.Path.Combine(path1,path2,path3)
    let combine4Paths path1 path2 path3 path4 =
        System.IO.Path.Combine(path1,path2,path3,path4)
    
