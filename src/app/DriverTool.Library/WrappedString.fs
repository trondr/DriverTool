namespace DriverTool.Library

module WrappedString =
    
    //Credits: https://fsharpforfunandprofit.com/posts/designing-with-types-more-semantic-types/

    type IWrappedString =
        abstract Value :string

    let create canonicalize (validate:string->Result<string,System.Exception>) cstor (s:string) =
        match s with
        |null -> Result.Error (new System.Exception(sprintf "Argument is null or empty: %s" (nameof s)))
        | _ ->
            result{
                let s' = canonicalize s
                let! s'' = validate s'
                return (cstor s'')
            }
    
    /// Apply the given function to the wrapped value
    let apply f (s:IWrappedString) =
        s.Value |> f

    /// Get the wrapped value
    let value s = apply id s

    /// Equality test
    let equals left right =
        (value left) = (value right)

    /// Comparison
    let compareTo left right =
        (value left).CompareTo (value right)

    /// Canonicalizes a string before construction
    /// * converts all whitespace to a space char
    /// * trims both ends
    let singleLineTrimmed s =
        System.Text.RegularExpressions.Regex.Replace(s,"\s"," ").Trim()

    /// A validation function based on length
    let lengthValidator len (s:string) =
        match(s.Length <= len) with
        |true -> Result.Ok s
        |false -> Result.Error (toException (sprintf "String '%s' exceeds maximum lenght of %d." s len) None)

module String127 =
    type String127 = private String127 of string with
        interface WrappedString.IWrappedString with
            member this.Value =              
                let (String127 s) = 
                    this in s
    let create =
        WrappedString.create WrappedString.singleLineTrimmed (WrappedString.lengthValidator 127) String127

module String50 =
    type String50 = private String50 of string with
        interface WrappedString.IWrappedString with
            member this.Value =              
                let (String50 s) = 
                    this in s
    let create =
        WrappedString.create WrappedString.singleLineTrimmed (WrappedString.lengthValidator 50) String50

module String32 =
    type String32 = private String32 of string with
        interface WrappedString.IWrappedString with
            member this.Value =              
                let (String32 s) = 
                    this in s
    let create =
        WrappedString.create WrappedString.singleLineTrimmed (WrappedString.lengthValidator 32) String32