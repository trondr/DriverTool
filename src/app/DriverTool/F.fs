﻿namespace DriverTool


[<AutoOpen>]
module F=

    #if DEBUG
    let (|>) value func =
        let result = func value
        result
    #endif

    open System
    /// <summary>
    /// Null coalescing operator
    /// </summary>
    /// <param name="lhs"></param>
    /// <param name="rhs"></param>
    let (|?) lhs rhs = (if lhs = null then rhs else lhs) 

    let tryCatch<'T, 'R> f  (t:'T) : Result<'R, Exception> =
        try
            Result.Ok (f t)
        with
            | ex -> Result.Error ex

    let tryCatchWithMessage<'T,'R> f (t:'T) message : Result<'R, Exception> =
        try
            Result.Ok (f t)
        with
            | ex -> Result.Error (new Exception(message, ex))

    let tryCatch2<'T1,'T2, 'R> f  (t1:'T1) (t2:'T2) : Result<'R, Exception> =
        try
            Result.Ok (f t1 t2)
        with
            | ex -> Result.Error ex

    let getUniqueRecords records =
        let uniqueRecords = 
            records |> Seq.distinct
        uniqueRecords

    let getUnique list =
        list |> Seq.distinct

    let getUniqueR list =
        match list with
        |Error ex -> Result.Error ex
        |Ok l -> 
            Result.Ok (l |> Seq.distinct)

    //Source: http://www.fssnip.net/7UJ/title/ResultBuilder-Computational-Expression
    let ofOption error = function Some s -> Ok s | None -> Error error
    //Source: http://www.fssnip.net/7UJ/title/ResultBuilder-Computational-Expression
    type ResultBuilder() =
        member __.Return(x) = Ok x

        member __.ReturnFrom(m: Result<_, _>) = m

        member __.Bind(m, f) = Result.bind f m
        member __.Bind((m, error): (Option<'T> * 'E), f) = m |> ofOption error |> Result.bind f

        member __.Zero() = None

        member __.Combine(m, f) = Result.bind f m

        member __.Delay(f: unit -> _) = f

        member __.Run(f) = f()

        member __.TryWith(m, h) =
            try __.ReturnFrom(m)
            with e -> h e

        member __.TryFinally(m, compensation) =
            try __.ReturnFrom(m)
            finally compensation()

        member __.Using(res:#IDisposable, body) =
            __.TryFinally(body res, fun () -> match res with null -> () | disp -> disp.Dispose())

        member __.While(guard, f) =
            if not (guard()) then Ok () else
            do f() |> ignore
            __.While(guard, f)

        member __.For(sequence:seq<_>, body) =
            __.Using(sequence.GetEnumerator(), fun enum -> __.While(enum.MoveNext, __.Delay(fun () -> body enum.Current)))

    let result = new ResultBuilder()

    let getAllExceptions (results:seq<Result<_,Exception>>) =
            let f = fun (r:Result<_,Exception>) ->
                match r with
                |Error ex -> Some(getAccumulatedExceptionMessages ex)
                |Ok v -> None
            results 
            |> Seq.choose f
    
    let getAllValues (results:seq<Result<_,Exception>>) =
        let f = fun (r:Result<_,Exception>) ->
            match r with
            |Error ex -> None
            |Ok v -> Some(v)
        results 
        |> Seq.choose f

    let toAccumulatedResult (results:seq<Result<_,Exception>>) =
        let allExceptionMessages = 
                (getAllExceptions results) 
                |> Seq.toArray
        
        let accumulatedResult =             
            match allExceptionMessages.Length with
            | 0 -> 
                let allValues = getAllValues results
                Result.Ok allValues
            | _ -> Result.Error (new Exception(String.Join<string>(" ", allExceptionMessages)))
        accumulatedResult

    open System.IO

    let toValidDirectoryName (name:string) =    
        let invalid = 
            new String(Path.GetInvalidFileNameChars()) + new String(Path.GetInvalidPathChars());
        let validName = 
                new string
                    (
                        seq{
                            //Remove invalid characters from name
                            for (c:char) in name do
                                if(not (invalid.Contains(c.ToString())) ) then
                                    yield c    
                        } |> Seq.toArray
                    )    
        validName
            .Replace("[","_")
            .Replace("]", "_")
            .Replace("(", "_")
            .Replace(")", "_")
            .Replace(",", "_")
            .Replace(" ", "_")
            .Replace("__", "_")
            .Trim()
            .Trim('_');
    
    let nullOrWhiteSpaceGuard (obj:string) (argumentName:string) =
            if(String.IsNullOrWhiteSpace(obj)) then
                raise (new ArgumentException("Value cannot be null or whitespace.",argumentName))
    
    let nullGuardResult (obj: 'T when 'T : not struct, argumentName:string) =
        match obj with
        |Null -> Result.Error (new ArgumentNullException("Value cannot be null.",argumentName) :> Exception)
        |NotNull v -> Result.Ok v

    let nullGuard (obj: 'T when 'T : not struct) (argumentName:string) =
        match obj with
        |Null -> raise (new ArgumentNullException("Value cannot be null.",argumentName))
        |NotNull _ -> ()

    let createWithContinuationGeneric success failure validator (value:'T) : Result<'T,Exception> = 
                match value with
                |Null -> failure (new ArgumentNullException("value","Value cannot be null.") :> Exception)
                |NotNull v -> 
                    let result = validator value
                    match result with
                    |Ok vr -> success vr
                    |Error ex -> failure ex
        
    let createGeneric validator (value:'T) =
        let success v = Result.Ok v
        let failure ex = Result.Error ex
        createWithContinuationGeneric success failure validator value 

    open System.Text.RegularExpressions
    //Source: http://www.fssnip.net/29/title/Regular-expression-active-pattern
    let (|Regex|_|) pattern input =
        let m = Regex.Match(input, pattern)
        if m.Success then Some(List.tail [ for g in m.Groups -> g.Value ])
        else None
    
    open System.Security.Cryptography

    //Source: https://stackoverflow.com/questions/33312260/how-can-i-select-a-random-value-from-a-list-using-f
    let shuffleCrypto xs =
        let a = xs |> Seq.toArray
        use rng = new RNGCryptoServiceProvider ()
        let bytes = Array.zeroCreate a.Length
        rng.GetBytes bytes
        Array.zip bytes a |> Array.sortBy fst |> Array.map snd

    let getNRandomItems n xs = 
       xs |> shuffleCrypto |> Seq.take n