namespace DriverTool.Library.CmUi
    open System
    open MvvmHelpers
    open DriverTool.Library.CmUi.UiModels
    open System.Windows.Threading

    type CmPackageViewModel(cmPackage:CmPackage) =
        inherit BaseViewModel()        
        member this.Model = cmPackage.Model
        member this.ModelCodes = cmPackage.ModelCodes |> String.concat "|"
        member this.Manufacturer = cmPackage.Manufacturer
        member this.Os = cmPackage.Os
        member this.OsBuild = cmPackage.OsBuild
        member this.Released = cmPackage.Released
        member this.InstallerFile = cmPackage.InstallerFile
        member this.ReadmeFile = cmPackage.ReadmeFile

    type CmPackagesViewModel() =
        inherit BaseViewModel()
    
        let mutable cmPackages = new ObservableRangeCollection<CmPackageViewModel>()
        let mutable selectedCmPackages = new ObservableRangeCollection<CmPackageViewModel>()
        let mutable searchText = "Lenovo"
        do
            base.Title <- "Sccm Packages"

        member this.SearchText
            with get() = searchText
            and set(value) =
                base.SetProperty(ref searchText,value)|>ignore

        member this.CmPackages
            with get() = cmPackages
                
        member this.SelectedCmPackages
            with get() = selectedCmPackages

    type ExmapleCmPackagesViewModel() =
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


        
