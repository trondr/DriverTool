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
            | ex -> Result.Error (sourceException ex)

    let tryCatchWithMessage<'T,'R> f (t:'T) message : Result<'R, Exception> =
        try
            Result.Ok (f t)
        with
            | ex -> toErrorResult message (Some ex)

    let tryCatch2<'T1,'T2, 'R> f  (t1:'T1) (t2:'T2) : Result<'R, Exception> =
        try
            Result.Ok (f t1 t2)
        with
            | ex -> Result.Error (sourceException ex)

    let tryCatch2WithMessage<'T1,'T2, 'R> f  (t1:'T1) (t2:'T2) message : Result<'R, Exception> =
        try
            Result.Ok (f t1 t2)
        with
            | ex -> toErrorResult message (Some ex)

    let tryCatch3<'T1,'T2, 'T3, 'R> f  (t1:'T1) (t2:'T2) (t3:'T3) : Result<'R, Exception> =
        try
            Result.Ok (f t1 t2 t3)
        with
            | ex -> Result.Error (sourceException ex)

    let tryCatch3WithMessage<'T1,'T2, 'T3, 'R> f  (t1:'T1) (t2:'T2) (t3:'T3) message : Result<'R, Exception> =
        try
            Result.Ok (f t1 t2 t3)
        with
            | ex ->                
                toErrorResult message (Some ex)



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
        let resultsArray = results |> Seq.toArray        
        
        let allExceptionMessages = 
                (getAllExceptions resultsArray) 
                |> Seq.toArray
        
        let accumulatedResult =             
            match allExceptionMessages.Length with
            | 0 -> 
                let allValues = getAllValues resultsArray
                Result.Ok allValues
            | _ -> 
                toErrorResult (String.Join<string>(" ", allExceptionMessages)) None
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
        |Null -> 
            toErrorResult (sprintf "Value '%s' cannot be null." argumentName) None            
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
        let failure ex = Result.Error (sourceException ex)
        createWithContinuationGeneric success failure validator value 

    open System.Text.RegularExpressions
    //Source: http://www.fssnip.net/29/title/Regular-expression-active-pattern
    let (|Regex|_|) pattern input =
        let m = Regex.Match(input, pattern)
        if m.Success then Some(List.tail [ for g in m.Groups -> g.Value ])
        else None
    
    open System.Security.Cryptography
    open Common.Logging

    //Source: https://stackoverflow.com/questions/33312260/how-can-i-select-a-random-value-from-a-list-using-f
    let shuffleCrypto xs =
        let a = xs |> Seq.toArray
        use rng = new RNGCryptoServiceProvider ()
        let bytes = Array.zeroCreate a.Length
        rng.GetBytes bytes
        Array.zip bytes a |> Array.sortBy fst |> Array.map snd

    let getNRandomItems n xs = 
       xs |> shuffleCrypto |> Seq.take n
    

    let arrayToSeq (array:Array) =
        seq{
            for item in array do
                yield item
        }

    let getEnumValuesToString (enumType) =
        let enumValues = 
            Enum.GetValues(enumType)
            |>arrayToSeq
            |>Seq.map(fun v -> v.ToString())
            |>Seq.toArray
        "[" + String.Join("|",enumValues) + "]"

    let optionToBoolean option = 
        match option with
        |None -> false
        |Some _ -> true
    
    let resultToOption (logger:ILog) (result : Result<_,Exception>) =
        match result with
        |Ok s -> Some s
        |Error ex -> 
            logger.Error(ex.Message)
            None

    open System.Collections.Generic

    //Source: http://tomasp.net/blog/imperative-i-return.aspx/
    type Imperative<'T> = unit -> option<'T>

    type ImperativeBuilder() = 
        member x.Combine(a, b) = (fun () ->
            match a() with 
            | Some(v) -> Some(v) 
            | _ -> b() )
        member x.Delay(f:unit -> Imperative<_>) : Imperative<_> = (fun () -> f()())
        member x.Return(v) : Imperative<_> = (fun () -> Some(v))
        member x.Zero() = (fun () -> None)
        member x.Run(imp) = 
            match imp() with
            | Some(v) -> v
            | _ -> failwith "Nothing returned!"

        member x.For(inp:seq<_>, f) =
            let rec loop(en:IEnumerator<_>) = 
              if not(en.MoveNext()) then x.Zero() else
                x.Combine(f(en.Current), x.Delay(fun () -> loop(en)))
            loop(inp.GetEnumerator())
        
        member x.While(gd, body) = 
            let rec loop() =
                if not(gd()) then x.Zero() else
                x.Combine(body, x.Delay(fun () -> loop()))
            loop()
    let imperative = new ImperativeBuilder()  

    let stringToStream (text:string) =
        let stream = new System.IO.MemoryStream()
        let sw = new System.IO.StreamWriter(stream)
        sw.Write(text)
        sw.Flush()
        stream.Position <- 0L
        stream

    let toDateString (dateTime:DateTime) =
        dateTime.ToString("yyyy-MM-dd")


    open System
    open System.Threading
    
    type ShouldRetry = ShouldRetry of (RetryCount * LastException -> bool * RetryDelay)
    and RetryCount = int
    and LastException = exn
    and RetryDelay = TimeSpan
    type RetryPolicy = RetryPolicy of ShouldRetry
        
    type RetryPolicies() =
        static member NoRetry () : RetryPolicy =
            RetryPolicy( ShouldRetry (fun (retryCount, _) -> (retryCount < 1, TimeSpan.Zero)) )
        static member Retry (retryCount : int , intervalBewteenRetries : RetryDelay) : RetryPolicy =
            RetryPolicy( ShouldRetry (fun (currentRetryCount, _) -> (currentRetryCount < retryCount, intervalBewteenRetries)))
        static member Retry (currentRetryCount : int) : RetryPolicy =
            RetryPolicies.Retry(currentRetryCount, TimeSpan.Zero)
    
    type RetryResult<'T> = 
        | RetrySuccess of 'T
        | RetryFailure of exn
        
    type Retry<'T> = Retry of (RetryPolicy -> RetryResult<'T>)
    
    type RetryBuilder() =
        member self.Return (value : 'T) : Retry<'T> = Retry (fun retryPolicy -> RetrySuccess value)
    
        member self.Bind (retry : Retry<'T>, bindFunc : 'T -> Retry<'U>) : Retry<'U> = 
            Retry (fun retryPolicy ->
                let (Retry retryFunc) = retry 
                match retryFunc retryPolicy with
                | RetrySuccess value ->
                    let (Retry retryFunc') = bindFunc value
                    retryFunc' retryPolicy
                | RetryFailure exn -> RetryFailure exn )
    
        member self.Delay (f : unit -> Retry<'T>) : Retry<'T> = 
            Retry (fun retryPolicy ->
                let resultCell : option<RetryResult<'T>> ref = ref None 
                let lastExceptionCell : exn ref = ref null
                let (RetryPolicy(ShouldRetry shouldRetry)) = retryPolicy
                let canRetryCell : bool ref = ref true
                let currentRetryCountCell : int ref = ref 0
                while !canRetryCell do
                    try
                        let (Retry retryFunc) = f ()
                        let result = retryFunc retryPolicy
                        resultCell := Some result
                        canRetryCell := false
                    with e -> 
                        lastExceptionCell := e
                        currentRetryCountCell := 1 + !currentRetryCountCell
                        match shouldRetry(!currentRetryCountCell, !lastExceptionCell) with
                        | (true, retryDelay) ->
                            Thread.Sleep(retryDelay)
                        | (false, _) -> 
                            canRetryCell := false
                
                match !resultCell with
                | Some result -> result
                | None -> RetryFailure !lastExceptionCell )
    