namespace DriverTool.Library

open System

module WrappedString =
    
    //Credits: https://fsharpforfunandprofit.com/posts/designing-with-types-more-semantic-types/
    open System.Text

    type IWrappedString =
        abstract Value :string

    let create canonicalize (validate:string->Result<string,System.Exception>) cstor (s:string) =
        match s with
        |null -> Result.Error (new System.Exception(sprintf "Failed to create wrapped string. Argument is null or empty: %s" (nameof s)))
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
    
    let toOptionalString (s:String127) =
        match s with        
        |v when String.IsNullOrWhiteSpace(WrappedString.value v) -> None
        |_ -> Some s

module String50 =
    type String50 = private String50 of string with
        interface WrappedString.IWrappedString with
            member this.Value =              
                let (String50 s) = 
                    this in s
    let create =
        WrappedString.create WrappedString.singleLineTrimmed (WrappedString.lengthValidator 50) String50

    let toOptionalString (s:String50) =
        match s with        
        |v when String.IsNullOrWhiteSpace(WrappedString.value v) -> None
        |_ -> Some s

module String32 =
    type String32 = private String32 of string with
        interface WrappedString.IWrappedString with
            member this.Value =              
                let (String32 s) = 
                    this in s
    let create =
        WrappedString.create WrappedString.singleLineTrimmed (WrappedString.lengthValidator 32) String32

    let toOptionalString (s:String32) =
        match s with        
        |v when String.IsNullOrWhiteSpace(WrappedString.value v) -> None
        |_ -> Some s