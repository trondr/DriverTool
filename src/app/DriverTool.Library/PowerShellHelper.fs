namespace DriverTool.Library

module PowerShellHelper =
    open System
    open System.Collections.Generic
    open System.Management.Automation
    open System.Management.Automation.Runspaces
    open System.Collections.ObjectModel
    open DriverTool.Library.Configuration

    ///Convert Collection of PSObject's to a sequence of PSObject's
    let toSeq (psObjects:Collection<PSObject>) =
        seq{
            for pso in psObjects do
                yield pso
        }

    ///Run PowerShell commands
    let runPowerShell (action:PowerShell->Collection<PSObject>) =
        use powershell = PowerShell.Create(RunspaceMode.NewRunspace)
        action(powershell)

    ///Convert text string to array of string lines
    let textToLines (text:string) =
        (text.Split([|Environment.NewLine|],StringSplitOptions.RemoveEmptyEntries))

    ///Convert array of string lines to text string
    let linesToText (lines:string array) =
        lines
        |>String.concat Environment.NewLine

    ///Remove commented lines from PowerShell script
    let removeComments (script:String) =
            script
            |>textToLines
            |> Array.filter (fun line -> not (line.Trim().StartsWith("#"))) //Filter out commented lines
            |> linesToText //Put the script back together

    /// Start and stop transcript logging
    let addTranscriptLogging script = 
        let transcriptFilePath = getLogFilePath + ".transcript.log"
        let startTransScript = sprintf "Start-TranScript -Path \"%s\" -Append| Out-Null" transcriptFilePath
        let stopTransScript = "Stop-TranScript|Out-Null"
        let scriptLines = (script|>textToLines)
        Array.append (Array.append [|startTransScript|] scriptLines) [|stopTransScript|]
        |>linesToText

    ///Run PowerShell script
    let runPowerShellScript script (variables:Dictionary<string,obj>) =
        use runspace = RunspaceFactory.CreateRunspace()
        runspace.Open()
        use pipeline = runspace.CreatePipeline()
        let preparedScript = 
            script
            |> removeComments
            //|> addTranscriptLogging
        pipeline.Commands.AddScript(preparedScript)
        variables |> Seq.map (fun kvp -> 
                runspace.SessionStateProxy.SetVariable(kvp.Key,kvp.Value)
            ) |> Seq.iter id
        let output = pipeline.Invoke() |> toSeq
        runspace.Close()
        output

    let getFirstValue script =
        let variables = new Dictionary<string, obj>()
        let value = 
            (runPowerShellScript script variables)            
            |>Seq.toArray
            |>Array.head
        value.ToString()