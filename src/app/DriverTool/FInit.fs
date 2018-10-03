module FInit

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

