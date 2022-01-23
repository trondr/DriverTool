namespace DriverTool.PowerCLI.Library.FSharp.CmdLets

open System.Collections
open System.Management.Automation
open System.Management.Automation.Language
open DriverTool.Library

type ManufacturerCompleter () =
    interface IArgumentCompleter with
        member this.CompleteArgument(commandName:string,parameterName:string,wordToComplete:string,commandAst:CommandAst,fakeBoundParameters:IDictionary) =
            DriverTool.Library.ManufacturerTypes.getValidManufacturerNames()
            |>Seq.filter(fun m -> (new WildcardPattern(wordToComplete + "*", WildcardOptions.IgnoreCase)).IsMatch(m))
            |>Seq.map(fun m -> new CompletionResult(m))

type ModelCodeCompleter () =
    interface IArgumentCompleter with
        member this.CompleteArgument(commandName:string,parameterName:string,wordToComplete:string,commandAst:CommandAst,fakeBoundParameters:IDictionary) =
            let modelCodes =
                if(fakeBoundParameters.Contains("Manufacturer")) then
                    let manufacturerName = fakeBoundParameters.["Manufacturer"] :?>string
                    Data.getModelCodes manufacturerName Data.allDriverPacks.Value        
                else
                    [||]            
            modelCodes
            |>Seq.filter(fun mc -> (new WildcardPattern(wordToComplete + "*", WildcardOptions.IgnoreCase)).IsMatch(mc))
            |>Seq.map(fun mc -> new CompletionResult(mc))

type OperatingSystemCompleter () =
    interface IArgumentCompleter with
        member this.CompleteArgument(commandName:string,parameterName:string,wordToComplete:string,commandAst:CommandAst,fakeBoundParameters:IDictionary) =
            let operatingSystems =
                if(fakeBoundParameters.Contains("Manufacturer") && fakeBoundParameters.Contains("ModelCode")) then
                    let manufacturerName = fakeBoundParameters.["Manufacturer"] :?>string
                    let modelCode = fakeBoundParameters.["ModelCode"] :?>string
                    Data.getOperatingSystems manufacturerName modelCode Data.allDriverPacks.Value        
                else
                    [||]            
            operatingSystems
            |>Seq.filter(fun mc -> (new WildcardPattern(wordToComplete + "*", WildcardOptions.IgnoreCase)).IsMatch(mc))
            |>Seq.map(fun mc -> new CompletionResult(mc))

type OperatingSystemCodeCompleter () =
    interface IArgumentCompleter with
        member this.CompleteArgument(commandName:string,parameterName:string,wordToComplete:string,commandAst:CommandAst,fakeBoundParameters:IDictionary) =            
            OperatingSystem.getValidOsShortNames
            |>Seq.toArray
            |>Seq.filter(fun mc -> (new WildcardPattern(wordToComplete + "*", WildcardOptions.IgnoreCase)).IsMatch(mc))
            |>Seq.map(fun mc -> new CompletionResult(mc))

type OsBuildCompleter () =
    interface IArgumentCompleter with
        member this.CompleteArgument(commandName:string,parameterName:string,wordToComplete:string,commandAst:CommandAst,fakeBoundParameters:IDictionary) =
            let modelCodes =
                if(fakeBoundParameters.Contains("Manufacturer") && fakeBoundParameters.Contains("ModelCode")) then
                    let manufacturerName = fakeBoundParameters.["Manufacturer"] :?>string
                    let modelCode = fakeBoundParameters.["ModelCode"] :?>string
                    let operatingSystem = fakeBoundParameters.["OperatingSystem"] :?>string
                    Data.getOsBuild manufacturerName modelCode operatingSystem Data.allDriverPacks.Value        
                else
                    [||]            
            modelCodes
            |>Seq.filter(fun mc -> (new WildcardPattern(wordToComplete + "*", WildcardOptions.IgnoreCase)).IsMatch(mc))
            |>Seq.map(fun mc -> new CompletionResult(mc))

