module F

open System

let tryCatch<'T, 'R> f  (t:'T) : Result<'R, Exception> =
    try
        Result.Ok (f t)
    with
        | ex -> Result.Error ex

