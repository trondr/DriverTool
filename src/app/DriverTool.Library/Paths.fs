namespace DriverTool.Library

module Paths =

    open System.IO
    
    let HasInvalidPathCharacters (path :string) = 
        let invalidPathCharacters = Path.GetInvalidPathChars();
        if (path.IndexOfAny(invalidPathCharacters) <> -1) 
        then true
        else false

    let HasWildCardCharacters (path :string) = 
        let wildCardCharcaters = [| '*';'?' |];
        if (path.IndexOfAny(wildCardCharcaters) <> -1) 
        then true
        else false

    let IsValidPath (path:string) =
        let invalidPath = 
            System.String.IsNullOrWhiteSpace(path)
            || (HasInvalidPathCharacters path)
            || (HasWildCardCharacters path)
        not invalidPath
        