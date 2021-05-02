namespace DriverTool.Library

[<AutoOpen>]
module F=

    #if DEBUG
    let (|>) value func =
        let result = func value
        result
    #endif

    open System
    open System.IO

    /// <summary>
    /// Null coalescing operator
    /// </summary>
    /// <param name="lhs"></param>
    /// <param name="rhs"></param>
    let (|?) lhs rhs = (if lhs = null then rhs else lhs)

    let tryCatch<'T, 'R> (message:string option) f  (t:'T) : Result<'R, Exception> =
        try
            Result.Ok (f t)
        with
            | ex -> toErrorResult ex message

    let tryCatch2<'T1,'T2, 'R> (message:string option) f (t1:'T1) (t2:'T2) : Result<'R, Exception> =
        try
            Result.Ok (f t1 t2)
        with
            | ex -> toErrorResult ex message

    let tryCatch3<'T1,'T2, 'T3, 'R> (message:string option) f (t1:'T1) (t2:'T2) (t3:'T3) : Result<'R, Exception> =
        try
            Result.Ok (f t1 t2 t3)
        with
            | ex -> toErrorResult ex message

    let tryCatch4<'T1,'T2, 'T3, 'T4, 'R> (message:string option) f (t1:'T1) (t2:'T2) (t3:'T3) (t4:'T4) : Result<'R, Exception> =
        try
            Result.Ok (f t1 t2 t3 t4)
        with
            | ex -> toErrorResult ex message

    let tryCatch5<'T1,'T2, 'T3, 'T4, 'T5, 'R> (message:string option) f (t1:'T1) (t2:'T2) (t3:'T3) (t4:'T4) (t5:'T5) : Result<'R, Exception> =
        try
            Result.Ok (f t1 t2 t3 t4 t5)
        with
            | ex -> toErrorResult ex message

    let tryCatch6<'T1,'T2, 'T3, 'T4, 'T5, 'T6, 'R> (message:string option) f (t1:'T1) (t2:'T2) (t3:'T3) (t4:'T4) (t5:'T5) (t6:'T6) : Result<'R, Exception> =
        try
            Result.Ok (f t1 t2 t3 t4 t5 t6)
        with
            | ex -> toErrorResult ex message

    let tryCatch7<'T1,'T2, 'T3, 'T4, 'T5, 'T6, 'T7, 'R> (message:string option) f (t1:'T1) (t2:'T2) (t3:'T3) (t4:'T4) (t5:'T5) (t6:'T6) (t7:'T7) : Result<'R, Exception> =
        try
            Result.Ok (f t1 t2 t3 t4 t5 t6 t7)
        with
            | ex -> toErrorResult ex message

    let tryCatch8<'T1,'T2, 'T3, 'T4, 'T5, 'T6, 'T7, 'T8, 'R> (message:string option) f (t1:'T1) (t2:'T2) (t3:'T3) (t4:'T4) (t5:'T5) (t6:'T6) (t7:'T7) (t8:'T8) : Result<'R, Exception> =
        try
            Result.Ok (f t1 t2 t3 t4 t5 t6 t7 t8)
        with
            | ex -> toErrorResult ex message

    //Source: http://www.fssnip.net/7UJ/title/ResultBuilder-Computational-Expression
    let ofOption error = function Some s -> Result.Ok s | None -> Result.Error error
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
                |Result.Error ex -> Some(getAccumulatedExceptionMessages ex)
                |Result.Ok v -> None
            results 
            |> Seq.choose f
    
    let getAllValues (results:seq<Result<_,Exception>>) =
        let f = fun (r:Result<_,Exception>) ->
            match r with
            |Result.Error ex -> None
            |Result.Ok v -> Some(v)
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
                toErrorResult (toException (String.Join<string>(" ", allExceptionMessages)) None) None
        accumulatedResult

    let getAllErrorMessages (results:seq<Result<'T,Exception>>) =         
        results
        |> Seq.filter (fun dpi -> 
                            match dpi with
                            |Result.Error _ -> true
                            | _ -> false)
        |> Seq.map (fun dpi -> 
                        match dpi with
                        |Result.Error ex -> getAccumulatedExceptionMessages ex
                        | _ -> String.Empty)

    let getAllSuccesses (results:seq<Result<'T,Exception>>) =
        results
        |> Seq.filter (fun dpi -> 
                                match dpi with
                                |Ok _ -> true
                                | _ -> false
                           )
        |> Seq.map (fun dpi -> 
                        match dpi with
                        |Ok pi -> pi
                        | _ -> failwith "Failed to get all successes due to a bug in the success filter.")

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
            toErrorResult (toException (sprintf "Value '%s' cannot be null." argumentName) None) None
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
                    |Result.Ok vr -> success vr
                    |Result.Error ex -> failure ex
        
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
    

    let optionToBoolean option = 
        match option with
        |None -> false
        |Some _ -> true
    
    let optionToString option =
        match option with
        |None -> String.Empty
        |Some v -> v

    let resultToOption (logger:ILog) (result : Result<_,Exception>) =
        match result with
        |Result.Ok s -> Some s
        |Result.Error ex -> 
            logger.Error(ex.Message)
            None

    let resultToValueUnsafe result =
        match result with
        |Result.Ok v -> 
            v
        |Result.Error ex ->
            raise ex
            

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

    let toDateTime onError cultureInfo dateString =                     
        try
            match cultureInfo with
            |Some ci ->
                System.DateTime.Parse(dateString, ci)
            |None ->
                System.DateTime.Parse(dateString)
        with            
        | ex -> onError(ex)

    let listToSequence (list:System.Collections.IList) =
        seq{
            for item in list do
                yield item
        }

    let max date1 date2 =
        if(date1 > date2) then
            date1
        else
            date2

    /// Euclidean remainder, the proper modulo operation. Source: https://stackoverflow.com/questions/35848489/f-integer-integer-is-calculated-how/35848799
    let inline (%!) a b = (a % b + b) % b

    ///Rounds a float to a specified number of digits
    let round (value:float) (digits:int) =
        System.Math.Round (value, digits)

    ///Convert text string to array of string lines
    let textToLines (text:string) =
        (text.Split([|Environment.NewLine|],StringSplitOptions.RemoveEmptyEntries))

    ///Convert array of string lines to text string
    let linesToText (lines:string array) =
        lines
        |>String.concat Environment.NewLine
