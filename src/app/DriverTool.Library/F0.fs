namespace DriverTool.Library

[<AutoOpen>]
module F0=

    //Source: https://stackoverflow.com/questions/2920094/how-can-i-convert-between-f-list-and-f-tuple
    let tupleToArray t = 
        if Microsoft.FSharp.Reflection.FSharpType.IsTuple(t.GetType()) 
            then Some (Microsoft.FSharp.Reflection.FSharpValue.GetTupleFields t)
            else None

    //Source: https://stackoverflow.com/questions/2920094/how-can-i-convert-between-f-list-and-f-tuple
    let listToTuple l =
        let l' = List.toArray l
        let types = l' |> Array.map (fun o -> o.GetType())
        let tupleType = Microsoft.FSharp.Reflection.FSharpType.MakeTupleType types
        Microsoft.FSharp.Reflection.FSharpValue.MakeTuple (l' , tupleType)

    //Source: http://www.fssnip.net/mW/title/memoize-
    let memoize fn =
      let cache = new System.Collections.Generic.Dictionary<_,_>()
      (fun x ->
        match cache.TryGetValue x with
        | true, v -> v
        | false, _ -> let v = fn (x)
                      cache.Add(x,v)
                      v)
    //Source: http://www.fssnip.net/7UQ/title/Null-Value-guard-active-pattern
    let (|Null|NotNull|) (x : 'T when 'T : not struct) =
        if obj.ReferenceEquals(x, null) then Null else NotNull x

    open System

    let rec getAccumulatedExceptionMessages (ex: Exception) =
        match ex.InnerException with
        | null -> ex.Message
        | _ -> ex.Message + " " + (getAccumulatedExceptionMessages ex.InnerException)

    let sourceException ex = 
        System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex).SourceException

    let toException message (innerException: System.Exception option) =
        match innerException with
        |Some iex ->
            (new System.Exception(message, sourceException iex))            
        |None ->
            (new System.Exception(message))
    
    let toErrorResult ex message =
       match message with
       |Some m -> Result.Error (toException m (Some ex))
       |None -> Result.Error (sourceException ex)

    //Not implemented result.
    let toNotImplementedResult message =
       match message with
       |Some m -> Result.Error (toException $"Not implemented. %s{m}" None)
       |None -> Result.Error (toException "Not implemented." None)
    
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
