namespace DriverTool

module Environment=
    
    let expandEnvironmentVariables (text:string) =
        System.Environment.ExpandEnvironmentVariables(text)
    
    let replaceValueWithValueName (text:string, value:string, valueName:string) :string =
        if (value.Length > 1) then
            let replacedText =
                text.Replace(value, System.String.Format("%{0}%", valueName))
            replacedText
        else
            text

    let unExpandEnironmentVariables (text:string) :string =
        let environmentVariables = System.Environment.GetEnvironmentVariables()
        let keys = 
            environmentVariables.Keys
            |> Seq.cast<string>
        let values = 
            environmentVariables.Values
            |> Seq.cast<string>
        let valueKeyMap = 
            keys
            |> Seq.zip values
            |> Seq.sortBy (fun (v,k) -> -v.Length) //Make sure to replace the longest parts of the text first
        let mutable unexpandedText = text
        valueKeyMap
        |> Seq.map (fun (v,k) -> 
                unexpandedText <- replaceValueWithValueName (unexpandedText, v, k)
            )
        |>Seq.toArray
        |>ignore
        unexpandedText

