namespace DriverTool

module Cryptography =

    open DriverTool.Library
    
    let isTrusted (filePath:FileSystem.Path) =
        DriverTool.CSharpLib.Wintrust.IsTrusted(FileSystem.pathValue filePath)
