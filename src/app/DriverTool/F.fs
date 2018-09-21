module F

open System

let tryCatch<'T, 'R> f  (t:'T) : Result<'R, Exception> =
    try
        Result.Ok (f t)
    with
        | ex -> Result.Error ex

let tryCatch2<'T1,'T2, 'R> f  (t1:'T1) (t2:'T2) : Result<'R, Exception> =
    try
        Result.Ok (f t1 t2)
    with
        | ex -> Result.Error ex


let rec getAccumulatedExceptionMessages (ex: Exception) =
    match ex.InnerException with
    | null -> ex.Message
    | _ -> ex.Message + " " + (getAccumulatedExceptionMessages ex.InnerException)

let getUnique list =
    match list with
    |Error ex -> Result.Error ex
    |Ok l -> 
        Result.Ok (l |> Seq.distinct)