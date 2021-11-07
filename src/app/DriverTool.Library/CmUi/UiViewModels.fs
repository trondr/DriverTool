namespace DriverTool.Library.CmUi
    open System      
    open DriverTool.Library    
    open DriverTool.Library.Logging
    open DriverTool.Library.CmUi.UiModels
    open System.Windows.Threading    
    open System.Windows.Input
    open Common.Logging
    open System.Collections.Specialized
    open System.Collections
    open MvvmHelpers    
    open System.Threading.Tasks    
    open MvvmHelpers.Commands
    open System.Linq
    open DriverTool.Library.PackageXml    
    open System.Windows
    open DriverTool.Library.DriverPack
    open DriverTool.Library.DriverPacks
    
    [<AllowNullLiteral>]
    type DriverPackInfoViewModel(driverPack:DriverPackInfo) =
        inherit BaseViewModel()
        let mutable isSelected = false
        let mutable info = null
        member this.Model = driverPack.Model
        member this.ModelCodes = driverPack.ModelCodes |> String.concat "|"
        member this.Manufacturer = driverPack.Manufacturer
        member this.Os = driverPack.Os
        member this.OsBuild = driverPack.OsBuild
        member this.Released = driverPack.Released
        member this.InstallerFile = driverPack.InstallerFile
        member this.ReadmeFile = driverPack.ReadmeFile
        member this.ModelWmiQuery = driverPack.ModelWmiQuery
        member this.ManufacturerWmiQuery = driverPack.ManufacturerWmiQuery
        member this.IsSelected
            with get() =                
                isSelected
            and set(value) =                
                base.SetProperty(&isSelected,value)|>ignore
        
        static member ToDriverPackInfo(driverPackInfoViewModel: DriverPackInfoViewModel) :DriverPackInfo =
            {
                Model = driverPackInfoViewModel.Model
                ModelCodes = driverPackInfoViewModel.ModelCodes.Split([|'|'|])
                Manufacturer = driverPackInfoViewModel.Manufacturer
                Os = driverPackInfoViewModel.Os
                OsBuild = driverPackInfoViewModel.OsBuild
                Released = driverPackInfoViewModel.Released
                InstallerFile = driverPackInfoViewModel.InstallerFile
                ReadmeFile = driverPackInfoViewModel.ReadmeFile
                ModelWmiQuery = driverPackInfoViewModel.ModelWmiQuery
                ManufacturerWmiQuery = driverPackInfoViewModel.ManufacturerWmiQuery
            }

        override this.ToString() =            
                match info with
                | null ->                    
                    info <- 
                        let sb = new System.Text.StringBuilder()
                        sb.AppendFormat("Model(s): {0}", this.Model)
                            .AppendLine()
                            .AppendFormat("ModelCodes: {0}", this.ModelCodes)
                            .AppendLine()
                            .AppendFormat("Manufacturer: {0}", this.Manufacturer)
                            .AppendLine()
                            .AppendFormat("Operating System: {0} ({1})", this.Os, this.OsBuild)
                            .AppendLine()
                            .AppendFormat("Released: {0}", this.Released.ToString("yyyy-MM-dd"))
                            .AppendLine()
                            .Append(sprintf "InstallerFile: %A" this.InstallerFile)
                            .AppendLine()
                            .Append(sprintf "ReadmeFile: %A" this.ReadmeFile)
                            .AppendLine()
                            .Append(sprintf "ManufacturerWmiQuery: %A" this.ManufacturerWmiQuery)
                            .AppendLine()
                            .Append(sprintf "ModelWmiQuery: %A" this.ModelWmiQuery)
                            .ToString()
                    info
                | _ -> info


    
    type DriverPackInfosViewModel() =
        inherit BaseViewModel()
    
        let mutable driverPackInfos = null
        let mutable selectedDriverPackInfos = null
        let mutable tobePackagedDriverPackInfos = null
        let mutable selectedToBePackagedDriverPackInfos = null
        let mutable searchText=""
        let mutable logger: ILog = null
        let mutable loadCommand = null
        let mutable packageCommand = null
        let mutable addPackageCommand = null
        let mutable removePackageCommand = null
        let mutable copyInfoCommand = null
        let mutable statusMessage = "Ready"
        let mutable selectedDriverPackInfo:DriverPackInfoViewModel = null
        let mutable driverPackInfosViewSource:System.Windows.Data.CollectionViewSource = null
        let mutable progressValue = 0.0
        let mutable progressIsIndeterminate = false
        
        let currentDispatcher =
            System.Windows.Application.Current.Dispatcher

        let updateUi action =
            let a = new System.Action(fun () -> action())            
            if(currentDispatcher.CheckAccess()) then
                a.Invoke()
            else
                currentDispatcher.BeginInvoke(DispatcherPriority.Background,a) |> ignore
        
        let createAsyncCommand action canExecute onError =
            let a = new System.Func<Task>(fun o -> action(o))            
            let c = new System.Func<obj,bool>(fun obj -> canExecute(obj))
            let ar = new System.Action<exn>(fun ex -> onError ex)
            new AsyncCommand(a,c,ar,true)

        let createCommand action canExecute =
            let a = new System.Action<obj>(fun o -> action(o))            
            let c = new System.Func<obj,bool>(fun obj -> canExecute(obj))
            new Command(a,c)
                    
        let toDriverPackInfoViewModel driverPack =
            new DriverPackInfoViewModel (driverPack)

        do
            logger <- Logger<DriverPackInfosViewModel>()
            base.Title <- "DriverTool - CM Device Drivers Packaging"
            
        member this.SearchText
            with get() = searchText
            and set(value) =
                if(base.SetProperty(&searchText,value)) then
                    this.DriverPackInfosView.Refresh()
                    ()

        member this.StatusMessage
            with get() = statusMessage
            and set(value) = 
                base.SetProperty(&statusMessage,value)|>ignore

        member this.SelectedDriverPackInfo
            with get() = selectedDriverPackInfo
            and set(value) =
                base.SetProperty(&selectedDriverPackInfo,value)|>ignore

        member this.DriverPackInfos
            with get() = 
                match driverPackInfos with
                | null ->
                    driverPackInfos <- new ObservableRangeCollection<DriverPackInfoViewModel>()                    
                    driverPackInfos.CollectionChanged.AddHandler(new NotifyCollectionChangedEventHandler(this.OnCollectionChanged))
                    driverPackInfos
                |_ -> driverPackInfos

        member this.DriverPackInfosView
            with get(): System.ComponentModel.ICollectionView =
                match driverPackInfosViewSource with
                |null ->
                    driverPackInfosViewSource <- new System.Windows.Data.CollectionViewSource()
                    driverPackInfosViewSource.Source <- this.DriverPackInfos
                    driverPackInfosViewSource.View.CollectionChanged.AddHandler(new NotifyCollectionChangedEventHandler(this.OnCollectionChanged))
                    driverPackInfosViewSource.Filter.AddHandler(new System.Windows.Data.FilterEventHandler(fun sender fe -> 
                    (
                        match fe.Item with
                        | :? DriverPackInfoViewModel -> 
                            let driverPack = fe.Item :?> DriverPackInfoViewModel
                            fe.Accepted <- 
                                System.String.IsNullOrEmpty(this.SearchText) || driverPack.Model.Contains(this.SearchText) || driverPack.ModelCodes.Contains(this.SearchText) || driverPack.OsBuild.Contains(this.SearchText)
                            ()
                        | _ -> ()
                    )))
                    driverPackInfosViewSource.View
                |_ -> driverPackInfosViewSource.View

        member this.SelectedDriverPackInfos
            with get() = 
                match selectedDriverPackInfos with
                | null ->
                    selectedDriverPackInfos <- new ObservableRangeCollection<DriverPackInfoViewModel>()                    
                    selectedDriverPackInfos.CollectionChanged.AddHandler(new NotifyCollectionChangedEventHandler(this.OnCollectionChanged))                    
                    selectedDriverPackInfos
                |_ -> selectedDriverPackInfos
                
        member this.ToBePackagedDriverPackInfos
            with get() = 
                match tobePackagedDriverPackInfos with
                | null ->
                    tobePackagedDriverPackInfos <- new ObservableRangeCollection<DriverPackInfoViewModel>()                    
                    tobePackagedDriverPackInfos.CollectionChanged.AddHandler(new NotifyCollectionChangedEventHandler(this.OnCollectionChanged))
                    tobePackagedDriverPackInfos
                |_ -> tobePackagedDriverPackInfos

        member this.SelectedToBePackagedDriverPackInfos
            with get() = 
                match selectedToBePackagedDriverPackInfos with
                | null ->
                    selectedToBePackagedDriverPackInfos <- new ObservableRangeCollection<DriverPackInfoViewModel>()                    
                    selectedToBePackagedDriverPackInfos.CollectionChanged.AddHandler(new NotifyCollectionChangedEventHandler(this.OnCollectionChanged))                    
                    selectedToBePackagedDriverPackInfos
                |_ -> selectedToBePackagedDriverPackInfos

        member this.LoadCommand
            with get() =
                match loadCommand with
                |null ->
                    loadCommand <- createAsyncCommand (fun _ -> 
                        async{                            
                            this.ReportProgress "Loading CM driver package infos from all supported vendors..."  String.Empty String.Empty None true None
                            match(result{
                                let! cacheFolderPath = getCacheFolderPath()                                
                                let! sccmPackages = (loadDriverPackInfos cacheFolderPath this.ReportProgress)
                                let driverPackInfoViewModels = sccmPackages |> Array.map toDriverPackInfoViewModel
                                updateUi (fun () -> 
                                        this.DriverPackInfos.ReplaceRange(driverPackInfoViewModels)
                                    )
                                return ()
                            })with
                            |Result.Ok _ -> this.ReportProgress "Successfully loaded sccm packages"  String.Empty String.Empty None false None
                            |Result.Error ex -> logger.Error(sprintf "Failed to load sccm packages due to %s" ex.Message)
                            this.ReportProgress "Done loading CM driver package infos!"  String.Empty String.Empty None false None                           
                            this.ReportProgress "Ready"  String.Empty String.Empty None false None
                            }|> Async.startAsPlainTask
                    ) (fun _ -> this.IsNotBusy) (fun ex -> logger.Error(ex.Message)) 
                    loadCommand
                |_ -> loadCommand


        member this.AddPackageCommand
            with get() =
                match addPackageCommand with
                |null ->
                    addPackageCommand <- createCommand (fun _ -> 
                                                            this.DriverPackInfos.Where(fun cmp -> cmp.IsSelected)
                                                            |> Seq.toArray
                                                            |> Array.map(fun cmp -> 
                                                                                    logger.Info(sprintf "Adding %s" cmp.Model)
                                                                                    this.ToBePackagedDriverPackInfos.Add(cmp)|> ignore
                                                                                    cmp.IsSelected <- false
                                                                                    this.DriverPackInfos.Remove(cmp)
                                                                            ) |> ignore
                                                       ) (fun _ -> this.IsNotBusy && this.DriverPackInfos.Where(fun cmp -> cmp.IsSelected).Count() > 0)
                    addPackageCommand
                |_ -> addPackageCommand

        member this.RemovePackageCommand
            with get() =
                match removePackageCommand with
                |null ->
                    removePackageCommand <- createCommand (fun _ -> 
                                                            this.ToBePackagedDriverPackInfos.Where(fun cmp -> cmp.IsSelected)
                                                            |> Seq.toArray
                                                            |> Array.map(fun cmp -> 
                                                                                    logger.Info(sprintf "Removing %s" cmp.Model)
                                                                                    this.ToBePackagedDriverPackInfos.Remove(cmp)|> ignore
                                                                                    cmp.IsSelected <- false
                                                                                    this.DriverPackInfos.Add(cmp)
                                                                            ) |> ignore
                                                       ) (fun _ -> this.IsNotBusy && this.ToBePackagedDriverPackInfos.Where(fun cmp -> cmp.IsSelected).Count() > 0)
                    removePackageCommand
                |_ -> removePackageCommand

        member this.PackageCommand
            with get() = 
                match packageCommand with
                |null ->
                    packageCommand <-                    
                        createAsyncCommand (fun _ ->                                        
                                        async{
                                            this.ReportProgress "TODO: Packaging CM driver packages..."  String.Empty String.Empty None true None
                                            let logPackagingResult (r:Result<_,Exception>) =
                                                match r with
                                                |Result.Ok p -> this.ReportProgress (sprintf "Succesfully created CM driver package: %A" p) String.Empty String.Empty None true None
                                                |Result.Error ex -> logger.Error(sprintf "Failed to create CM driver package due to: %s" (getAccumulatedExceptionMessages ex))
                                                r                                                                                                                                
                                            match(result{
                                                let! cacheFolderPath = getCacheFolderPath()                                
                                                let! downloadedDriverPackInfos =
                                                    this.ToBePackagedDriverPackInfos.Select(fun cmp-> cmp) 
                                                    |> Seq.toArray 
                                                    |> Seq.map DriverPackInfoViewModel.ToDriverPackInfo
                                                    |> Seq.map (packageSccmPackage cacheFolderPath this.ReportProgress)
                                                    |> Seq.map logPackagingResult                                                    
                                                    |> toAccumulatedResult                                                    
                                                return downloadedDriverPackInfos |> Seq.toArray
                                            })with
                                            |Result.Ok _ -> this.ReportProgress "Successfully packaged CM packages" String.Empty String.Empty  None false None
                                            |Result.Error ex -> logger.Error(sprintf "Failed to package CM driver packages due to %s" (getAccumulatedExceptionMessages ex))
                                            this.ReportProgress "TODO: Done packaging CM driver packages!" String.Empty String.Empty None true None
                                            this.ReportProgress "Ready"  String.Empty String.Empty None false None
                                        } |> Async.startAsPlainTask                                    
                                    ) (fun _ -> this.IsNotBusy && this.ToBePackagedDriverPackInfos.Count > 0) (fun ex -> logger.Error(ex.Message))
                    packageCommand
                | _ -> packageCommand
        
        member this.CopyInfoCommand
            with get() =
                match copyInfoCommand with
                |null ->
                    copyInfoCommand <- createCommand (fun _ ->                                     
                                    match selectedDriverPackInfo with
                                    |null -> 
                                        ()
                                    |_ ->                                                                     
                                        let info = sprintf "%s" (selectedDriverPackInfo.ToString())
                                        logger.Info(info)
                                        Clipboard.SetText(info)|>ignore
                                  ) (fun _ -> this.IsNotBusy && this.SelectedDriverPackInfo <> null)
                    copyInfoCommand
                |_ -> copyInfoCommand

        member internal this.OnCollectionChanged sender e =            
            this.RaiseCanExecuteChanged()            
            ()

        member this.IsBusy
            with get() = base.IsBusy
            and set(value) =
                base.IsBusy <- value
                this.RaiseCanExecuteChanged()

        member this.ProgressValue
            with get() = progressValue
            and set(value) =
                base.SetProperty(&progressValue,value)|>ignore

        member this.ProgressIsIndeterminate
            with get() = progressIsIndeterminate
            and set(value) =
                base.SetProperty(&progressIsIndeterminate,value)|>ignore

        member private this.ReportProgress :reportProgressFunction = (fun activity status currentOperation percentComplete isBusy id ->
            updateUi (fun () -> 
                    this.IsBusy <- isBusy
                    match percentComplete with
                    |Some p ->
                        this.ProgressValue <- p
                        this.ProgressIsIndeterminate <-false
                        reportProgressStdOut' activity status currentOperation percentComplete isBusy id
                    |None ->
                        this.ProgressValue <- 0.0
                        this.ProgressIsIndeterminate <-isBusy
                        if(activity.Contains("TODO:")) then
                            logger.Warn(activity)
                        else
                            logger.Info(activity)                        
                    this.StatusMessage <- activity
                    ()
                )
            ()
            )

        member this.RaiseCanExecuteChanged() =
            this.LoadCommand.RaiseCanExecuteChanged()
            this.PackageCommand.RaiseCanExecuteChanged()
            this.AddPackageCommand.RaiseCanExecuteChanged()
            this.RemovePackageCommand.RaiseCanExecuteChanged()
            this.CopyInfoCommand.RaiseCanExecuteChanged()

    type ExampleDriverPackInfosViewModel() =
        inherit  DriverPackInfosViewModel()
        do
            let cmPackage1 =
                {                    
                    Model = "ThinkPad T480S Type 20L7 20L8"
                    ModelCodes = [|"20L7";"20L8"|]
                    Manufacturer = "Lenovo"
                    Os = "Windows 10"
                    OsBuild = "1909"
                    InstallerFile = {Url = "https://download.lenovo.com/pccbbs/mobiles/tp_t480s_w1064_1909_202012.exe";Checksum="";Size=0L;FileName="tp_t480s_w1064_1909_202012.exe"}
                    ReadmeFile = Some {Url = "https://download.lenovo.com/pccbbs/mobiles/tp_t480s_w1064_1909_202012.txt";Checksum="";Size=0L;FileName="tp_t480s_w1064_1909_202012.txt"}
                    Released = new DateTime (2020,12,01)
                    ModelWmiQuery = 
                        {
                            Name = "ThinkPad T480S Type 20L7 20L8"
                            NameSpace = "root\cimv2"
                            Query="SELECT * FROM Win32_ComputerSystemProduct WHERE ( (Name LIKE \"20L7%\") OR (Name LIKE \"20L8%\")"
                        }
                    ManufacturerWmiQuery=
                        {
                            Name = "Lenovo"
                            NameSpace=""
                            Query=""
                        }

                }
            let cmPackage2 =
                {                    
                    Model = "ThinkPad X1 EXTREME Gen 2"
                    ModelCodes = [|"20QW";"20QV"|]
                    Manufacturer = "Lenovo"
                    Os = "Windows 10"
                    OsBuild = "1909"
                    InstallerFile = {Url = "https://download.lenovo.com/pccbbs/mobiles/tp_x1extreme_mt20qv-20qw-p1_mt20qt-20qu_w1064_1909_202009.exe";Checksum="";Size=0L;FileName="tp_x1extreme_mt20qv-20qw-p1_mt20qt-20qu_w1064_1909_202009.exe"}
                    ReadmeFile = Some {Url = "https://download.lenovo.com/pccbbs/mobiles/tp_x1extreme_mt20qv-20qw-p1_mt20qt-20qu_w1064_1909_202009.txt";Checksum="";Size=0L;FileName="tp_x1extreme_mt20qv-20qw-p1_mt20qt-20qu_w1064_1909_202009.txt"}
                    Released = new DateTime (2020,09,01)
                    ModelWmiQuery = 
                        {
                            Name = "ThinkPad X1 EXTREME Gen 2"
                            NameSpace = "root\cimv2"
                            Query="SELECT * FROM Win32_ComputerSystemProduct WHERE ( (Name LIKE \"20QW%\") OR (Name LIKE \"20QV%\")"
                        }
                    ManufacturerWmiQuery=
                        {
                            Name = "Lenovo"
                            NameSpace=""
                            Query=""
                        }
                    
                }

            let cmPackage3 =
                {                    
                    Model = "ThinkPad X1 Yoga Type 20JD 20JE 20JF 20JG"
                    ModelCodes = [|"20JE";"20JG";"20JD";"20JF"|]
                    Manufacturer = "Lenovo"
                    Os = "Windows 10"
                    OsBuild = "1909"
                    InstallerFile = {Url = "https://download.lenovo.com/pccbbs/mobiles/tp_x1yoga_mt20jd-20je-20jf-20jg_w1064_1909_201911.exe";Checksum="";Size=0L;FileName="tp_x1yoga_mt20jd-20je-20jf-20jg_w1064_1909_201911.exe"}
                    ReadmeFile = Some {Url = "https://download.lenovo.com/pccbbs/mobiles/tp_x1yoga_mt20jd-20je-20jf-20jg_w1064_1909_201911.txt";Checksum="";Size=0L;FileName="tp_x1yoga_mt20jd-20je-20jf-20jg_w1064_1909_201911.txt"}
                    Released = new DateTime (2019,11,01)
                    ModelWmiQuery = 
                        {
                            Name = "ThinkPad X1 Yoga Type 20JD 20JE 20JF 20JG"
                            NameSpace = "root\cimv2"
                            Query="SELECT * FROM Win32_ComputerSystemProduct WHERE ( (Name LIKE \"20JE%\") OR (Name LIKE \"20JG%\") OR (Name LIKE \"20JD%\") OR (Name LIKE \"20JF%\")"
                        }
                    ManufacturerWmiQuery=
                        {
                            Name = "Lenovo"
                            NameSpace=""
                            Query=""
                        }
                }

            base.DriverPackInfos.AddRange([|new DriverPackInfoViewModel(cmPackage1);new DriverPackInfoViewModel(cmPackage2)|])
            base.ToBePackagedDriverPackInfos.AddRange([|new DriverPackInfoViewModel(cmPackage3)|])


        
