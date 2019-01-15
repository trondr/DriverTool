namespace DriverTool

module Cryptography =
    
    let isTrusted (filePath:Path) =
        DriverTool.CSharpLib.Wintrust.IsTrusted(filePath.Value)
