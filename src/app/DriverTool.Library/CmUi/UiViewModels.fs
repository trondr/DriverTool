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

    type CmPackageViewModel(cmPackage:CmPackage) =
        inherit BaseViewModel()
        let mutable isSelected = false
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

    type CmPackagesViewModel() =
        inherit BaseViewModel()
    
        let mutable cmPackages = new ObservableRangeCollection<CmPackageViewModel>()
        let mutable selectedCmPackages = new ObservableRangeCollection<CmPackageViewModel>()
        let mutable searchText = "Lenovo"        
        let mutable logger: ILog = null
        let mutable packageCommand = null
        let mutable addPackageCommand = null
        let mutable removePackageCommand = null

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

        let mutable timer = null

        do
            logger <- getLoggerByName "DriverTool.Library.CmUi"
            base.Title <- "Sccm Packages"            
            
        member this.SearchText
            with get() = searchText
            and set(value) =
                base.SetProperty(&searchText,value)|>ignore

        member this.CmPackages
            with get() = 
                match cmPackages with
                | null ->
                    cmPackages <- new ObservableRangeCollection<CmPackageViewModel>()                    
                    cmPackages.CollectionChanged.AddHandler(new NotifyCollectionChangedEventHandler(this.OnCollectionChanged))                    
                    cmPackages
                |_ -> cmPackages
                
        member this.SelectedCmPackages
            with get() = 
                match selectedCmPackages with
                | null ->
                    selectedCmPackages <- new ObservableRangeCollection<CmPackageViewModel>()                    
                    selectedCmPackages.CollectionChanged.AddHandler(new NotifyCollectionChangedEventHandler(this.OnCollectionChanged))
                    selectedCmPackages
                |_ -> selectedCmPackages

        member this.AddPackageCommand
            with get() =
                match addPackageCommand with
                |null ->
                    addPackageCommand <- createCommand (fun _ -> 
                                                            this.CmPackages.Where(fun cmp -> cmp.IsSelected)
                                                            |> Seq.toArray
                                                            |> Array.map(fun cmp -> 
                                                                                    logger.Info(sprintf "Adding %s" cmp.Model)
                                                                                    this.SelectedCmPackages.Add(cmp)|> ignore
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
                                                            this.SelectedCmPackages.Where(fun cmp -> cmp.IsSelected)
                                                            |> Seq.toArray
                                                            |> Array.map(fun cmp -> 
                                                                                    logger.Info(sprintf "Removing %s" cmp.Model)
                                                                                    this.SelectedCmPackages.Remove(cmp)|> ignore
                                                                                    cmp.IsSelected <- false
                                                                                    this.CmPackages.Add(cmp)
                                                                            ) |> ignore
                                                       ) (fun _ -> this.IsNotBusy && this.SelectedCmPackages.Where(fun cmp -> cmp.IsSelected).Count() > 0)
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
                                            logger.Warn("TODO: Packaging...")                                            
                                            this.SelectedCmPackages.Where(fun cmp-> true) |> Seq.toArray |> Array.map(fun cmp -> 
                                            logger.Warn(sprintf "TODO: Packaging '%s'..." cmp.Model)
                                            Async.Sleep 3000 |> Async.RunSynchronously                                            
                                            ) |> ignore                                            
                                            logger.Warn("TODO: Packaging done!")
                                            updateUi (fun () -> this.IsBusy <- false)
                                        } |> Async.startAsPlainTask                                    
                                    ) (fun _ -> this.IsNotBusy && this.SelectedCmPackages.Count > 0) (fun ex -> logger.Error(ex.Message))
                    packageCommand
                | _ -> packageCommand
        
        member internal this.OnCollectionChanged sender e = 
            this.PackageCommand.RaiseCanExecuteChanged()
            this.AddPackageCommand.RaiseCanExecuteChanged()
            this.RemovePackageCommand.RaiseCanExecuteChanged()            
            ()

        member this.CanPackage
            with get() =
                this.IsNotBusy && this.SelectedCmPackages.Count > 0

        member this.IsBusy
            with get() = base.IsBusy
            and set(value) =
                base.IsBusy <- value
                this.RaiseCanExecuteChanged()

        member this.RaiseCanExecuteChanged() =
            this.PackageCommand.RaiseCanExecuteChanged()
            this.AddPackageCommand.RaiseCanExecuteChanged()
            this.RemovePackageCommand.RaiseCanExecuteChanged()

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
                    ReadmeFile = {Url = "https://download.lenovo.com/pccbbs/mobiles/tp_t480s_w1064_1909_202012.txt";Checksum="";Size=0L;FileName="tp_t480s_w1064_1909_202012.txt"}
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
                    ReadmeFile = {Url = "https://download.lenovo.com/pccbbs/mobiles/tp_x1extreme_mt20qv-20qw-p1_mt20qt-20qu_w1064_1909_202009.txt";Checksum="";Size=0L;FileName="tp_x1extreme_mt20qv-20qw-p1_mt20qt-20qu_w1064_1909_202009.txt"}
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
                    ReadmeFile = {Url = "https://download.lenovo.com/pccbbs/mobiles/tp_x1yoga_mt20jd-20je-20jf-20jg_w1064_1909_201911.txt";Checksum="";Size=0L;FileName="tp_x1yoga_mt20jd-20je-20jf-20jg_w1064_1909_201911.txt"}
                    Released = new DateTime (2019,11,01)
                    WmiQuery = "SELECT * FROM Win32_ComputerSystemProduct WHERE ( (Name LIKE \"20JE%\") OR (Name LIKE \"20JG%\") OR (Name LIKE \"20JD%\") OR (Name LIKE \"20JF%\")"
                }

            base.CmPackages.AddRange([|new CmPackageViewModel(cmPackage1);new CmPackageViewModel(cmPackage2)|])
            base.SelectedCmPackages.AddRange([|new CmPackageViewModel(cmPackage3)|])


        
