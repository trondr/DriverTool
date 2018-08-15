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

