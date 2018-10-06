namespace DriverTool
open System
open FileOperations
open System.IO

module ExistingPath=

    type ExistingFilePath private (filePath:string) =
        member x.Value = filePath
        static member NewWithContinutation success failure (filePath:string) : Result<ExistingFilePath,Exception> =
            let path = Path.create filePath
            match path with
            |Ok p -> 
                match (fileExists p.Value) with
                |true -> success (ExistingFilePath (p.Value))
                |false-> failure (new FileNotFoundException("File not found: " + filePath):>Exception)
            |Error ex -> failure ex

        static member New (filePath:string) =
            let success e = Result.Ok e
            let failure ex = Result.Error ex
            ExistingFilePath.NewWithContinutation success failure filePath

    type ExistingDirectoryPath private (directoryPath:string) =
        member x.Value = directoryPath
        static member NewWithContinutation success failure directoryPath : Result<ExistingDirectoryPath,Exception> =
            let path = Path.create directoryPath
            match path with
            |Ok p -> 
                match (fileExists p.Value) with
                |true -> success (ExistingDirectoryPath (p.Value))
                |false-> failure (new DirectoryNotFoundException("Directory not found: " + directoryPath):>Exception)
            |Error ex -> failure ex

        static member New (directoryPath:string) =
            let success e = Result.Ok e
            let failure ex = Result.Error ex
            ExistingDirectoryPath.NewWithContinutation success failure directoryPath