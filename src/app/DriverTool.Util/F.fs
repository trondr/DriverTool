module F

open System.Text.RegularExpressions
//Source: http://www.fssnip.net/29/title/Regular-expression-active-pattern
let (|Regex|_|) pattern input =
    let m = Regex.Match(input, pattern)
    if m.Success then Some(List.tail [ for g in m.Groups -> g.Value ])
    else None

