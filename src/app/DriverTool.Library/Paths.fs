namespace DriverTool.Library

module Paths =

    open System.IO
    open DriverTool.Library.RegExp
    
    let HasInvalidPathCharacters (path :string) = 
        let invalidPathCharacters = Path.GetInvalidPathChars();
        if (path.IndexOfAny(invalidPathCharacters) <> -1) 
        then true
        else false

    let HasWildCardCharacters (path :string) = 
        let wildCardCharcaters = [| '*';'?' |];
        let normalizedPath = replace @"^\\\\\?\\" "" true path //Remove any long path prefix before checking for wild card.
        if (normalizedPath.IndexOfAny(wildCardCharcaters) <> -1) 
        then true
        else false

    let IsValidPath (path:string) =
        let invalidPath = 
            System.String.IsNullOrWhiteSpace(path)
            || (HasInvalidPathCharacters path)
            || (HasWildCardCharacters path)
        not invalidPath
        