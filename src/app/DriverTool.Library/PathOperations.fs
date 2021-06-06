namespace DriverTool.Library

module PathOperations =
    let combinePaths2 path1 path2 =
        FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue path1, path2))
    
    let combine2Paths (path1, path2) =
        FileSystem.path (System.IO.Path.Combine(path1, path2))
    
    let combine3Paths (path1, path2, path3) =
        FileSystem.path (System.IO.Path.Combine(path1, path2, path3))

    let combine4Paths (path1, path2, path3, path4) =
        FileSystem.path (System.IO.Path.Combine(path1, path2, path3, path4))
    
    let getTempPath = 
        System.IO.Path.GetTempPath()

    let getRandomFileName() =
        System.IO.Path.GetRandomFileName()
        
    let getTempFile fileName =
        System.IO.Path.Combine(getTempPath,fileName)

    let getFileNameFromPath path = 
        System.IO.Path.GetFileName(FileSystem.pathValue path)
    
    let isUncPath path =
        let uri = new System.Uri(FileSystem.pathValue path)
        uri.IsUnc

    let getDrive path =
        match (FileSystem.pathValue path) with
        |Regex @"^([a-zA-Z]):.+$" [drive] -> Result.Ok drive
        |_ -> Result.Error (toException (sprintf "Failed to get drive. Path '%s' does not contain a drive." (FileSystem.pathValue path)) None)

    let getPathTail path =
        match (FileSystem.pathValue path) with
        |Regex @"^[a-zA-Z]:\\(.+)$" [pathTail] -> Result.Ok pathTail
        |_ -> Result.Error (toException (sprintf "Failed to get path tail. Path '%s' does not contain a drive." (FileSystem.pathValue path)) None)

    let isNetworkDrive drive =
        let driveInfo = new System.IO.DriveInfo(drive)
        driveInfo.DriveType = System.IO.DriveType.Network
      
    open System
    open System.Runtime.InteropServices
    open System.ComponentModel
    [<DllImport("mpr.dll")>]
    extern int WNetGetUniversalName(string lpLocalPath, [<MarshalAs(UnmanagedType.U4)>] int dwInfoLevel, IntPtr lpBuffer, [<MarshalAs(UnmanagedType.U4)>] int& lpBufferSize)

    let networkDriveToUncPath drive =        
        let mutable buffer = Marshal.AllocCoTaskMem(1)
        try
            let mutable size = 0            
            let drivePath = sprintf "%s:\\" drive
            let UniversalNameInfoLevel = 0x00000001
            let ErrorMoreData = 234
            let ErrorSuccess = 0
            let apiReturnValue = WNetGetUniversalName (drivePath,UniversalNameInfoLevel,buffer, &size)
            match apiReturnValue with
            |x when x = ErrorMoreData ->
                Marshal.FreeCoTaskMem(buffer)    
                buffer <- Marshal.AllocCoTaskMem(size)
                let apiReturnValue2 = WNetGetUniversalName (drivePath,UniversalNameInfoLevel,buffer, &size)
                match apiReturnValue2 with
                | x when x = ErrorSuccess ->
                    let uncPath = Marshal.PtrToStringAnsi(new IntPtr(buffer.ToInt64() + Convert.ToInt64(IntPtr.Size)), size) 
                    FileSystem.path (uncPath.Substring(0, uncPath.IndexOf('\x00')))
                | _ ->
                    Result.Error (toException (sprintf "Failed to get unc path from network drive '%s'." drive) (Some (new Win32Exception(apiReturnValue2) :> Exception)))
            |_ ->                               
                Result.Error (toException (sprintf "Failed to get unc path from network drive '%s'." drive) (Some (new Win32Exception(apiReturnValue) :> Exception)))
        finally
            Marshal.FreeCoTaskMem(buffer)
    
    ///Convert local drive to unc path using admin share
    let localDriveToUncPath localDrive getComputerName =
        result{
            let path = sprintf "\\\\%s\\%s$" (getComputerName()) localDrive
            let! uncPath = FileSystem.path path
            return uncPath        
        }

    ///Convert path to unc path. If local drive convert to unc path using admin share if allowAdminShareForLocalDrive = true.
    let toUncPath useAdminShareForLocalDrive path = 
        result{
            let! uncPath =
                match (isUncPath path) with
                |true -> Result.Ok path
                |false ->
                    result{
                        let! drive = getDrive path
                        let! pathTail = getPathTail path                         
                        let! uncPath =
                            match (isNetworkDrive drive) with
                            |true ->
                                //network drive
                                result{
                                    let! uncPathShare = networkDriveToUncPath drive
                                    let! uncPath = combinePaths2 uncPathShare pathTail
                                    return uncPath                        
                                }
                            |false ->
                                //local drive
                                match useAdminShareForLocalDrive with
                                |true ->
                                    result{
                                        let! localUncPathShare = localDriveToUncPath drive (fun () -> System.Environment.MachineName)
                                        let! uncPath = combinePaths2 localUncPathShare pathTail
                                        return uncPath                            
                                    }
                                |false -> Result.Error (toException (sprintf "Failed to get unc path from '%s'. The path is a local path." (FileSystem.pathValue path)) None)
                        return uncPath
                    }
            return uncPath
        }
    