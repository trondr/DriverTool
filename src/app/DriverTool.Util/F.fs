module F

open System.Text.RegularExpressions
open System.Security.Cryptography

//Source: http://www.fssnip.net/29/title/Regular-expression-active-pattern
let (|Regex|_|) pattern input =
    let m = Regex.Match(input, pattern)
    if m.Success then Some(List.tail [ for g in m.Groups -> g.Value ])
    else None

//Source: https://stackoverflow.com/questions/33312260/how-can-i-select-a-random-value-from-a-list-using-f
let shuffleCrypto xs =
    let a = xs |> Seq.toArray
    use rng = new RNGCryptoServiceProvider ()
    let bytes = Array.zeroCreate a.Length
    rng.GetBytes bytes
    Array.zip bytes a |> Array.sortBy fst |> Array.map snd

let getNRandomItems n xs = 
   xs |> shuffleCrypto |> Seq.take n