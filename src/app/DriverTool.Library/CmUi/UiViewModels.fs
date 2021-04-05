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
    type CmPackageViewModel(cmPackage:CmPackage) =
        inherit BaseViewModel()
        let mutable isSelected = false
        let mutable info = null
        member this.Model = cmPackage.Model
        member this.ModelCodes = cmPackage.ModelCodes |> String.concat "|"
        member this.Manufacturer = cmPackage.Manufacturer
        member this.Os = cmPackage.Os
        member this.OsBuild = cmPackage.OsBuild
        member this.Released = cmPackage.Released
        member this.InstallerFile = cmPackage.InstallerFile
        member this.ReadmeFile = cmPackage.ReadmeFile
        member this.IsSelected
            with get() =                
                isSelected
            and set(value) =                
                base.SetProperty(&isSelected,value)|>ignore
        
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
                    
        let toCmPackageViewModel cmPackage =
            new CmPackageViewModel (cmPackage)

        do
            logger <- getLoggerByName "DriverTool.Library.CmUi"
            base.Title <- "Sccm Packages"
            
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
                            let cmPackage = fe.Item :?> CmPackageViewModel
                            fe.Accepted <- 
                                System.String.IsNullOrEmpty(this.SearchText) || cmPackage.Model.Contains(this.SearchText) || cmPackage.ModelCodes.Contains(this.SearchText) || cmPackage.OsBuild.Contains(this.SearchText)
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
                            updateUi (fun () -> this.IsBusy <- true)
                            updateUi (fun () -> this.StatusMessage <- "Loading information about CM driver packages for supported vendors...")
                            logger.Warn("TODO: Loading...")
                            
                            match(result{
                                let! cacheFolderPath = getCacheFolderPath()                                
                                let! sccmPackages = (loadSccmPackages (cacheFolderPath))
                                let cmPackageViewModels = sccmPackages |> Array.map toCmPackageViewModel
                                updateUi (fun () -> 
                                        this.CmPackages.ReplaceRange(cmPackageViewModels)
                                    )
                                return ()
                            })with
                            |Result.Ok _ -> logger.Info("Successfully loaded sccm packages")
                            |Result.Error ex -> logger.Error(sprintf "Failed to load sccm packages due to %s" ex.Message)
                            
                            Async.Sleep 3000 |> Async.RunSynchronously
                            logger.Warn("TODO: Loading finished!")
                            updateUi (fun () -> this.IsBusy <- false)
                            updateUi (fun () -> this.StatusMessage <- "Ready")
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
                                        this.IsBusy <- true
                                        async{
                                            updateUi (fun () -> this.StatusMessage <- "Packaging CM drivers...")
                                            logger.Warn("TODO: Packaging...")
                                            this.ToBePackagedCmPackages.Where(fun cmp-> true) |> Seq.toArray |> Array.map(fun cmp -> 
                                            logger.Warn(sprintf "TODO: Packaging '%s'..." cmp.Model)
                                            Async.Sleep 3000 |> Async.RunSynchronously                                            
                                            ) |> ignore                                            
                                            logger.Warn("TODO: Packaging done!")
                                            updateUi (fun () -> this.IsBusy <- false)
                                            updateUi (fun () -> this.StatusMessage <- "Ready")
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
                    WmiQuery = "SELECT * FROM Win32_ComputerSystemProduct WHERE ( (Name LIKE \"20L7%\") OR (Name LIKE \"20L8%\")"
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
                    WmiQuery = "SELECT * FROM Win32_ComputerSystemProduct WHERE ( (Name LIKE \"20QW%\") OR (Name LIKE \"20QV%\")"
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
                    WmiQuery = "SELECT * FROM Win32_ComputerSystemProduct WHERE ( (Name LIKE \"20JE%\") OR (Name LIKE \"20JG%\") OR (Name LIKE \"20JD%\") OR (Name LIKE \"20JF%\")"
                }

            base.CmPackages.AddRange([|new CmPackageViewModel(cmPackage1);new CmPackageViewModel(cmPackage2)|])
            base.ToBePackagedCmPackages.AddRange([|new CmPackageViewModel(cmPackage3)|])


        
