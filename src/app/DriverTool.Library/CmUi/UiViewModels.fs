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
    
    [<AllowNullLiteral>]
    type CmPackageViewModel(driverPack:DriverPackInfo) =
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
        
        static member ToCmPackage(cmPackageViewModel: CmPackageViewModel) :DriverPackInfo =
            {
                Model = cmPackageViewModel.Model
                ModelCodes = cmPackageViewModel.ModelCodes.Split([|'|'|])
                Manufacturer = cmPackageViewModel.Manufacturer
                Os = cmPackageViewModel.Os
                OsBuild = cmPackageViewModel.OsBuild
                Released = cmPackageViewModel.Released
                InstallerFile = cmPackageViewModel.InstallerFile
                ReadmeFile = cmPackageViewModel.ReadmeFile
                ModelWmiQuery = cmPackageViewModel.ModelWmiQuery
                ManufacturerWmiQuery = cmPackageViewModel.ManufacturerWmiQuery
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


    
    type CmPackagesViewModel() =
        inherit BaseViewModel()
    
        let mutable cmPackages = null
        let mutable selectedCmPackages = null
        let mutable tobePackagedCmPackages = null
        let mutable selectedToBePackagedCmPackages = null
        let mutable searchText=""
        let mutable logger: ILog = null
        let mutable loadCommand = null
        let mutable packageCommand = null
        let mutable addPackageCommand = null
        let mutable removePackageCommand = null
        let mutable copyInfoCommand = null
        let mutable statusMessage = "Ready"
        let mutable selectedCmPackage:CmPackageViewModel = null
        let mutable cmPackagesViewSource:System.Windows.Data.CollectionViewSource = null
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
                    
        let toCmPackageViewModel driverPack =
            new CmPackageViewModel (driverPack)

        do
            logger <- Logger<CmPackagesViewModel>()
            base.Title <- "DriverTool - CM Device Drivers Packaging"
            
        member this.SearchText
            with get() = searchText
            and set(value) =
                if(base.SetProperty(&searchText,value)) then
                    this.CmPackagesView.Refresh()
                    ()

        member this.StatusMessage
            with get() = statusMessage
            and set(value) = 
                base.SetProperty(&statusMessage,value)|>ignore

        member this.SelectedCmPackage
            with get() = selectedCmPackage
            and set(value) =
                base.SetProperty(&selectedCmPackage,value)|>ignore

        member this.CmPackages
            with get() = 
                match cmPackages with
                | null ->
                    cmPackages <- new ObservableRangeCollection<CmPackageViewModel>()                    
                    cmPackages.CollectionChanged.AddHandler(new NotifyCollectionChangedEventHandler(this.OnCollectionChanged))
                    cmPackages
                |_ -> cmPackages

        member this.CmPackagesView
            with get(): System.ComponentModel.ICollectionView =
                match cmPackagesViewSource with
                |null ->
                    cmPackagesViewSource <- new System.Windows.Data.CollectionViewSource()
                    cmPackagesViewSource.Source <- this.CmPackages
                    cmPackagesViewSource.View.CollectionChanged.AddHandler(new NotifyCollectionChangedEventHandler(this.OnCollectionChanged))
                    cmPackagesViewSource.Filter.AddHandler(new System.Windows.Data.FilterEventHandler(fun sender fe -> 
                    (
                        match fe.Item with
                        | :? CmPackageViewModel -> 
                            let driverPack = fe.Item :?> CmPackageViewModel
                            fe.Accepted <- 
                                System.String.IsNullOrEmpty(this.SearchText) || driverPack.Model.Contains(this.SearchText) || driverPack.ModelCodes.Contains(this.SearchText) || driverPack.OsBuild.Contains(this.SearchText)
                            ()
                        | _ -> ()
                    )))
                    cmPackagesViewSource.View
                |_ -> cmPackagesViewSource.View

        member this.SelectedCmPackages
            with get() = 
                match selectedCmPackages with
                | null ->
                    selectedCmPackages <- new ObservableRangeCollection<CmPackageViewModel>()                    
                    selectedCmPackages.CollectionChanged.AddHandler(new NotifyCollectionChangedEventHandler(this.OnCollectionChanged))                    
                    selectedCmPackages
                |_ -> selectedCmPackages
                
        member this.ToBePackagedCmPackages
            with get() = 
                match tobePackagedCmPackages with
                | null ->
                    tobePackagedCmPackages <- new ObservableRangeCollection<CmPackageViewModel>()                    
                    tobePackagedCmPackages.CollectionChanged.AddHandler(new NotifyCollectionChangedEventHandler(this.OnCollectionChanged))
                    tobePackagedCmPackages
                |_ -> tobePackagedCmPackages

        member this.SelectedToBePackagedCmPackages
            with get() = 
                match selectedToBePackagedCmPackages with
                | null ->
                    selectedToBePackagedCmPackages <- new ObservableRangeCollection<CmPackageViewModel>()                    
                    selectedToBePackagedCmPackages.CollectionChanged.AddHandler(new NotifyCollectionChangedEventHandler(this.OnCollectionChanged))                    
                    selectedToBePackagedCmPackages
                |_ -> selectedToBePackagedCmPackages

        member this.LoadCommand
            with get() =
                match loadCommand with
                |null ->
                    loadCommand <- createAsyncCommand (fun _ -> 
                        async{                            
                            this.ReportProgress true None "Loading CM driver package infos from all supported vendors..."
                            match(result{
                                let! cacheFolderPath = getCacheFolderPath()                                
                                let! sccmPackages = (loadSccmPackages (cacheFolderPath))
                                let cmPackageViewModels = sccmPackages |> Array.map toCmPackageViewModel
                                updateUi (fun () -> 
                                        this.CmPackages.ReplaceRange(cmPackageViewModels)
                                    )
                                return ()
                            })with
                            |Result.Ok _ -> this.ReportProgress false None "Successfully loaded sccm packages"
                            |Result.Error ex -> logger.Error(sprintf "Failed to load sccm packages due to %s" ex.Message)
                            this.ReportProgress false None "Done loading CM driver package infos!"                            
                            this.ReportProgress false None "Ready"
                            }|> Async.startAsPlainTask
                    ) (fun _ -> this.IsNotBusy) (fun ex -> logger.Error(ex.Message)) 
                    loadCommand
                |_ -> loadCommand


        member this.AddPackageCommand
            with get() =
                match addPackageCommand with
                |null ->
                    addPackageCommand <- createCommand (fun _ -> 
                                                            this.CmPackages.Where(fun cmp -> cmp.IsSelected)
                                                            |> Seq.toArray
                                                            |> Array.map(fun cmp -> 
                                                                                    logger.Info(sprintf "Adding %s" cmp.Model)
                                                                                    this.ToBePackagedCmPackages.Add(cmp)|> ignore
                                                                                    cmp.IsSelected <- false
                                                                                    this.CmPackages.Remove(cmp)
                                                                            ) |> ignore
                                                       ) (fun _ -> this.IsNotBusy && this.CmPackages.Where(fun cmp -> cmp.IsSelected).Count() > 0)
                    addPackageCommand
                |_ -> addPackageCommand

        member this.RemovePackageCommand
            with get() =
                match removePackageCommand with
                |null ->
                    removePackageCommand <- createCommand (fun _ -> 
                                                            this.ToBePackagedCmPackages.Where(fun cmp -> cmp.IsSelected)
                                                            |> Seq.toArray
                                                            |> Array.map(fun cmp -> 
                                                                                    logger.Info(sprintf "Removing %s" cmp.Model)
                                                                                    this.ToBePackagedCmPackages.Remove(cmp)|> ignore
                                                                                    cmp.IsSelected <- false
                                                                                    this.CmPackages.Add(cmp)
                                                                            ) |> ignore
                                                       ) (fun _ -> this.IsNotBusy && this.ToBePackagedCmPackages.Where(fun cmp -> cmp.IsSelected).Count() > 0)
                    removePackageCommand
                |_ -> removePackageCommand

        member this.PackageCommand
            with get() = 
                match packageCommand with
                |null ->
                    packageCommand <-                    
                        createAsyncCommand (fun _ ->                                        
                                        async{
                                            this.ReportProgress true None "TODO: Packaging CM driver packages..."
                                            let logPackagingResult (r:Result<_,Exception>) =
                                                match r with
                                                |Result.Ok p -> this.ReportProgress true None (sprintf "Succesfully created CM driver package: %A" p)
                                                |Result.Error ex -> logger.Error(sprintf "Failed to create CM driver package due to: %s" (getAccumulatedExceptionMessages ex))
                                                r                                                                                                                                
                                            match(result{
                                                let! cacheFolderPath = getCacheFolderPath()                                
                                                let! downloadedCmPackages =
                                                    this.ToBePackagedCmPackages.Select(fun cmp-> cmp) 
                                                    |> Seq.toArray 
                                                    |> Seq.map CmPackageViewModel.ToCmPackage
                                                    |> Seq.map (packageSccmPackage cacheFolderPath this.ReportProgress)
                                                    |> Seq.map logPackagingResult                                                    
                                                    |> toAccumulatedResult                                                    
                                                return downloadedCmPackages |> Seq.toArray
                                            })with
                                            |Result.Ok _ -> this.ReportProgress false None "Successfully packaged CM packages"
                                            |Result.Error ex -> logger.Error(sprintf "Failed to package CM driver packages due to %s" (getAccumulatedExceptionMessages ex))
                                            this.ReportProgress true None "TODO: Done packaging CM driver packages!"
                                            this.ReportProgress false None "Ready"                                            
                                        } |> Async.startAsPlainTask                                    
                                    ) (fun _ -> this.IsNotBusy && this.ToBePackagedCmPackages.Count > 0) (fun ex -> logger.Error(ex.Message))
                    packageCommand
                | _ -> packageCommand
        
        member this.CopyInfoCommand
            with get() =
                match copyInfoCommand with
                |null ->
                    copyInfoCommand <- createCommand (fun _ ->                                     
                                    match selectedCmPackage with
                                    |null -> 
                                        ()
                                    |_ ->                                                                     
                                        let info = sprintf "%s" (selectedCmPackage.ToString())
                                        logger.Info(info)
                                        Clipboard.SetText(info)|>ignore
                                  ) (fun _ -> this.IsNotBusy && this.SelectedCmPackage <> null)
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

        member private this.ReportProgress (isBusy:bool) (percent:float option) (message:string) =
            updateUi (fun () -> 
                    this.IsBusy <- isBusy
                    match percent with
                    |Some p ->
                        this.ProgressValue <- p
                        this.ProgressIsIndeterminate <-false
                        reportProgressStdOut isBusy percent message
                    |None ->
                        this.ProgressValue <- 0.0
                        this.ProgressIsIndeterminate <-isBusy
                        if(message.Contains("TODO:")) then
                            logger.Warn(message)
                        else
                            logger.Info(message)                        
                    this.StatusMessage <- message
                    ()
                )
            ()

        member this.RaiseCanExecuteChanged() =
            this.LoadCommand.RaiseCanExecuteChanged()
            this.PackageCommand.RaiseCanExecuteChanged()
            this.AddPackageCommand.RaiseCanExecuteChanged()
            this.RemovePackageCommand.RaiseCanExecuteChanged()
            this.CopyInfoCommand.RaiseCanExecuteChanged()

    type ExampleCmPackagesViewModel() =
        inherit  CmPackagesViewModel()
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

            base.CmPackages.AddRange([|new CmPackageViewModel(cmPackage1);new CmPackageViewModel(cmPackage2)|])
            base.ToBePackagedCmPackages.AddRange([|new CmPackageViewModel(cmPackage3)|])


        
