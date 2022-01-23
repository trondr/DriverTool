namespace DriverTool.Library

module RegExp =
    open System.Text.RegularExpressions
    open DriverTool.Library.F0

    ///Get IgnoreCase regex option if ignoreCase is true. Otherwise return regex option None.
    let toRegexOptions ignoreCase =
        match ignoreCase with
        |true -> 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        |false ->
            System.Text.RegularExpressions.RegexOptions.None

    ///Convert regular expression string to Regex object. If ignoreCase is set to true, case will be ignored when matching Regex
    let toRegEx pattern ignoreCase =
        try
            let regexOptions = toRegexOptions ignoreCase
            Result.Ok (new Regex(pattern,regexOptions))
        with
        |ex -> toErrorResult ex (Some (sprintf "Failed to convert pattern '%s' to RegEx" pattern))

    /// <summary>
    /// Convert array of string patterns to array of Regex objects. If ignoreCase is set to true, case will be ignored when matching Regex
    /// </summary>
    /// <param name="ignoreCase"></param>
    /// <param name="patterns"></param>    
    let toRegexPatterns ignoreCase patterns =
        result {            
            let! regPatterns = 
                patterns
                |>Array.map(fun p -> toRegEx p ignoreCase)
                |>toAccumulatedResult
            let regPatternsArray = (regPatterns|>Seq.toArray)            
            return regPatternsArray
        }
        
    /// <summary>
    /// If any of the regular expressions match the input text, return true
    /// </summary>
    /// <param name="excludeRegExPatterns"></param>
    /// <param name="text"></param>
    let matchAny excludeRegExPatterns text = 
        excludeRegExPatterns
        |>Array.exists(fun (p:Regex) -> p.IsMatch(text))