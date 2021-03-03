namespace DriverTool.Library

module RegExp =
    open System.Text.RegularExpressions
    open DriverTool.Library.F0

    let toRegexOptions ignoreCase =
        match ignoreCase with
        |true -> 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        |false ->
            System.Text.RegularExpressions.RegexOptions.None

    /// <summary>
    /// Convert array of string patterns to array of Regex objects. If ignoreCase is set to true, case will be ignored when matching Regex
    /// </summary>
    /// <param name="patterns"></param>
    /// <param name="ignoreCase"></param>
    let toRegexPatterns patterns ignoreCase =
        try
            let regexOptions = toRegexOptions ignoreCase
            let regPatterns = 
                patterns
                |>Array.map(fun p -> (new Regex(p,regexOptions)))
            Result.Ok regPatterns                
        with
        |ex -> toErrorResult (sprintf "Failed to convert list of patterns to RegEx patterns") (Some ex)

    /// <summary>
    /// If any of the regular expressions match the input text, return true
    /// </summary>
    /// <param name="excludeRegExPatterns"></param>
    /// <param name="text"></param>
    let matchAny excludeRegExPatterns text = 
        excludeRegExPatterns
        |>Array.exists(fun (p:Regex) -> p.IsMatch(text))