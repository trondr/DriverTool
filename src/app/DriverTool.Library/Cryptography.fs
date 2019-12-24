namespace DriverTool.Library

module Cryptography =

    let isTrusted (filePath:FileSystem.Path) =
        DriverTool.CSharpLib.Wintrust.IsTrusted(FileSystem.pathValue filePath)
