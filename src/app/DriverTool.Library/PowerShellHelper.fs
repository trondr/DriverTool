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

    ///Run PowerShell commands unsafe
    let runPowerShellUnsafe (action:PowerShell->Collection<PSObject>) =
        use powershell = PowerShell.Create(RunspaceMode.NewRunspace)
        action(powershell)

    ///Run PowerShell commands
    let runPowerShell (action:PowerShell->Collection<PSObject>) =
        let message = sprintf "Failed to run PowerShell commands."
        tryCatch (Some message) runPowerShellUnsafe action

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

    ///Run PowerShell script unsafe
    let runPowerShellScriptUnsafe script (variables:Dictionary<string,obj>) =
        use runspace = RunspaceFactory.CreateRunspace()
        runspace.Open()
        use pipeline = runspace.CreatePipeline()
        let preparedScript = 
            script
            |> removeComments
            |> addTranscriptLogging
        pipeline.Commands.AddScript(preparedScript)
        variables |> Seq.map (fun kvp -> 
                runspace.SessionStateProxy.SetVariable(kvp.Key,kvp.Value)
            ) |> Seq.iter id
        let output = pipeline.Invoke() |> toSeq |> Seq.toArray
        runspace.Close()
        output

    ///Run PowwerShell script
    let runPowerShellScript script (variables:Dictionary<string,obj>) =
        if(logger.IsDebugEnabled) then(logger.Debug(sprintf "Running PowershellScript:%s%s" System.Environment.NewLine script)) else ()
        let message = sprintf "Failed to run PowerShell script:%s%s" Environment.NewLine script
        tryCatch2 (Some message) runPowerShellScriptUnsafe script variables

    ///Get first string value returned by PowerShell script
    let getFirstStringValue script =
        result{
            let variables = new Dictionary<string, obj>()
            let! values = (runPowerShellScript script variables)
            let message = sprintf "Failed to get first string value from the return value of PowerShell script: %s%s" Environment.NewLine script
            let! firstValue = tryCatch (Some message) Array.head values
            return firstValue.ToString()
        }
        