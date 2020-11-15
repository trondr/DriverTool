namespace DriverTool.Tests

module LenovoUpdateTests =
    open DriverTool.Tests.Init
    open DriverTool    
    open DriverTool.Library.PackageXml
    open NUnit.Framework    
    let logger = Common.Logging.Simple.ConsoleOutLogger("LenovoUpdateTests",Common.Logging.LogLevel.Info,true,true,true,"yyyy-MM-dd-HH-mm-ss-ms")
    open DriverTool.Library.F
    open DriverTool.Library
    
    [<Test>]
    [<Category(TestCategory.IntegrationTests)>]
    [<TestCase("nz3gs05w.exe","nz3gs05w.txt","nz3gs05w_2_.xml",
        "ISDAS_NZ3GS",
        "Intel® SGX Device and Software (Windows 10 Version 1709 or later) - 10 [64]",
        "2.3.100.49777"
        
        
        
        )>]
    let extractUpdateTest (installerFileName,readmeFileName,packageXmlFileName,
                            packageName,
                            packageTitle,
                            packageVersion
                            ) =
        match(result{
            let! isAdministrator = DriverTool.Library.Requirements.assertIsAdministrator "Administrative privileges are required to run this integration test."
            use temporaryCacheFolder = new DirectoryOperations.TemporaryFolder(logger)
            let! temproaryCacheFolderPath = temporaryCacheFolder.FolderPath                        
            logger.Info(sprintf "Extract from embeded resource: %s, %s, %s" installerFileName readmeFileName packageXmlFileName)
            let! installerFilePath = EmbeddedResource.extractEmbeddedResourceByFileNameBase (installerFileName,temproaryCacheFolderPath,installerFileName,typeof<ThisTestAssembly>.Assembly)
            let! readmeFilePath = EmbeddedResource.extractEmbeddedResourceByFileNameBase (readmeFileName,temproaryCacheFolderPath,readmeFileName,typeof<ThisTestAssembly>.Assembly)
            let! packageXmlFilePath = EmbeddedResource.extractEmbeddedResourceByFileNameBase (packageXmlFileName,temproaryCacheFolderPath,packageXmlFileName,typeof<ThisTestAssembly>.Assembly)
            logger.Info(sprintf "Construct downloaded package info...")
            let downloadedPackageInfo: DownloadedPackageInfo =
                {
                    InstallerPath = FileSystem.pathValue installerFilePath
                    ReadmePath = FileSystem.pathValue readmeFilePath
                    PackageXmlPath = FileSystem.pathValue packageXmlFilePath
                    Package =
                        {
                            Name = packageName
                            Title = packageTitle
                            Version = packageVersion
                            Installer =
                                {
                                    Url = toOptionalUri "http://someurl" installerFileName
                                    Name = installerFileName
                                    Checksum = "C872A0F1A3159C68B811F31C841153D22E304550D815EDA6464C706247EB7658"
                                    Size = 2780688L
                                    Type = PackageFileType.Installer
                                }
                            ExtractCommandLine = "nz3gs05w.exe /VERYSILENT /DIR=%PACKAGEPATH% /EXTRACT=\"YES\""
                            InstallCommandLine = "%PACKAGEPATH%\\nz3gs05w.exe /verysilent /DIR=%PACKAGEPATH%\\TMP"
                            Category = "SomeCategory"
                            Readme =
                                {
                                    Url = toOptionalUri "http://someurl" readmeFileName
                                    Name = readmeFileName
                                    Checksum = "E6A73AA8DC369C5D16B0F24EB0438FF41305E68E4D91CCB406EF9E5C5FCAC181"
                                    Size = 14275L
                                    Type = PackageFileType.Readme
                                }
                            ReleaseDate = "2019-08-15"
                            PackageXmlName = packageXmlFileName
                            ExternalFiles = None
                        }
                }
            logger.Info("Create package folder path...")
            use temporaryPackageFolder = new DirectoryOperations.TemporaryFolder(logger)
            let! temporaryPackageFolderPath = temporaryPackageFolder.FolderPath
            let! extractedPackageInfo = PackageXml.extractInstaller (downloadedPackageInfo, temporaryPackageFolderPath)
            
            Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(FileSystem.pathValue temporaryPackageFolderPath,installerFileName)),sprintf "Installer '%s' is not copied to package folder." installerFileName)

            return extractedPackageInfo
        })with
        |Result.Ok v -> Assert.IsTrue(true)
        |Result.Error ex -> Assert.Fail(ex.Message)

    [<Test>]
    [<Category(TestCategory.IntegrationTests)>]
    let loadPackagesXmlTest () =
        match(result{
            use temporaryFolder = new DirectoryOperations.TemporaryFolder(logger)
            let! temporaryFolderPath = temporaryFolder.FolderPath
            let! xmlFilePath = DriverTool.Library.EmbeddedResource.extractEmbeddedResourceByFileNameBase ("LenovoCatalog_WithError_20QG_win10.xml",temporaryFolderPath,"LenovoCatalog_WithError_20QG_win10.xml",typeof<ThisTestAssembly>.Assembly)
            let! packages = DriverTool.LenovoUpdates.loadPackagesXml xmlFilePath 
            let! downloadedPackages = DriverTool.LenovoUpdates.downloadPackageXmls temporaryFolderPath packages
            let! packageInfos = 
                (DriverTool.LenovoUpdates.parsePackageXmls downloadedPackages)
                |>toAccumulatedResult
            return packageInfos
        })with
        |Result.Ok v -> Assert.IsTrue(false,"Did not fail as expected.")
        |Result.Error ex -> Assert.AreEqual("Failed to download all package infos due to the following 1 error messages:\r\nUri 'https://download.lenovo.com/pccbbs/mobiles/n2hwe01w.txt' does not represent a xml file.",ex.Message)        

    open DriverTool.Library.UpdatesContext
    open DriverTool.Library.ManufacturerTypes

    [<Test>]
    [<Category(TestCategory.ManualTests)>]
    let getRemoteUpdatesTests () =
        match(result{
            let! manufacturer = getManufacturerForCurrentSystem()
            let! modelCode = ModelCode.create "20QG" false
            let! operatingSystemCode = OperatingSystemCode.create "WIN10X64" false
            let! logDirectory = FileSystem.path @"c:\temp"
            let! patterns = (RegExp.toRegexPatterns [||] true)
            use cacheFolder = new DirectoryOperations.TemporaryFolder(logger)
            let! cacheFolderPath = cacheFolder.FolderPath
            let updatesRetrievalContext = toUpdatesRetrievalContext manufacturer modelCode operatingSystemCode true logDirectory cacheFolderPath false patterns
            let! packageInfos = DriverTool.LenovoUpdates.getRemoteUpdates logger cacheFolderPath updatesRetrievalContext
            return packageInfos        
        })with
        |Result.Ok v -> Assert.IsTrue(true)
        |Result.Error ex -> Assert.Fail(ex.Message)

    let allModels20200521 = [|"20L4";"20L3";"10G9";"10G8";"10GT";"10GS";"10J0";"10HY";"10GR";"10GQ";"10GR";"10GQ";"10F1";"10EY";"10F1";"10EY";"10F1";"10EY";"10QR";"10N3";"10MT";"10MS";"10MR";"10MQ";"10QR";"10MT";"10MS";"10MR";"10MQ";"10QR";"10MT";"10MS";"10MR";"10MQ";"10QR";"10MT";"10MS";"10MR";"10MQ";"10R7";"10QT";"10NC";"10M8";"10M7";"10R7";"10QT";"10M8";"10M7";"10R7";"10QT";"10M8";"10M7";"10R7";"10QT";"10M8";"10M7";"10R8";"10QK";"10MA";"10M9";"10R8";"10QK";"10MA";"10M9";"10R8";"10QK";"10MA";"10M9";"10R8";"10QK";"10MA";"10M9";"10RD";"10RC";"10RB";"10RA";"10M5";"10M4";"10M3";"10M2";"10VQ";"10VN";"10VM";"10VL";"10VK";"10VJ";"10VH";"10VG";"10VQ";"10VN";"10VM";"10VL";"10VK";"10VJ";"10VH";"10VG";"10MC";"10MB";"10MC";"10MB";"10ME";"10MD";"10ME";"10MD";"10TC";"10TA";"10T9";"10T8";"10T7";"10TC";"10TA";"10T9";"10T8";"10T7";"10TC";"10TA";"10T9";"10T8";"10T7";"10TC";"10TA";"10T9";"10T8";"10T7";"10U7";"10U6";"10TR";"10SV";"10SU";"10ST";"10U7";"10U6";"10TR";"10SV";"10SU";"10ST";"10U7";"10U6";"10TR";"10SV";"10SU";"10ST";"10U7";"10U6";"10TR";"10SV";"10SU";"10ST";"10U5";"10U4";"10TQ";"10SS";"10SR";"10SQ";"10U5";"10U4";"10TQ";"10SS";"10SR";"10SQ";"10U5";"10U4";"10TQ";"10SS";"10SR";"10SQ";"10U5";"10U4";"10TQ";"10SS";"10SR";"10SQ";"10VU";"10VT";"10FY";"10FX";"10FY";"10FX";"10FW";"10FV";"10FW";"10FV";"10EW";"10EV";"10EU";"10ET";"10EW";"10EV";"10EU";"10ET";"10EW";"10EV";"10EU";"10ET";"10Q2";"10Q1";"10Q0";"10NY";"10NX";"10Q2";"10Q1";"10Q0";"10NY";"10NX";"10SD";"10SC";"10FH";"10FG";"10NE";"10FM";"10FL";"10FD";"10FC";"10M6";"10LY";"10LX";"10M6";"10LY";"10LX";"10M6";"10LY";"10LX";"10F5";"10F4";"10F3";"10F2";"11AE";"11AD";"10MV";"10MU";"10QM";"10ML";"10MK";"10QM";"10ML";"10MK";"10QM";"10ML";"10MK";"10QM";"10ML";"10MK";"10QM";"10ML";"10MK";"10QL";"10N9";"10MN";"10MM";"10N0";"10MY";"10NV";"10NU";"10NT";"10NS";"10NR";"10NV";"10NU";"10NT";"10NS";"10NR";"10V8";"10UH";"10T2";"10T1";"10SY";"10RU";"10RT";"10RS";"10RR";"10V8";"10UH";"10T2";"10T1";"10SY";"10RU";"10RT";"10RS";"10RR";"10V8";"10UH";"10T2";"10T1";"10SY";"10RU";"10RT";"10RS";"10RR";"10V8";"10UH";"10T2";"10T1";"10SY";"10RU";"10RT";"10RS";"10RR";"10U3";"10U2";"10TN";"10SL";"10SK";"10SJ";"10U3";"10U2";"10TN";"10SL";"10SK";"10SJ";"10U3";"10U2";"10TN";"10SL";"10SK";"10SJ";"10U3";"10U2";"10TN";"10SL";"10SK";"10SJ";"10U1";"10U0";"10TM";"10SH";"10SG";"10SF";"10U1";"10U0";"10TM";"10SH";"10SG";"10SF";"10U1";"10U0";"10TM";"10SH";"10SG";"10SF";"10U1";"10U0";"10TM";"10SH";"10SG";"10SF";"10UJ";"10T6";"10T5";"10T3";"10S3";"10S2";"10S1";"10S0";"10UJ";"10T6";"10T5";"10T3";"10S3";"10S2";"10S1";"10S0";"10UJ";"10T6";"10T5";"10T3";"10S3";"10S2";"10S1";"10S0";"10UJ";"10T6";"10T5";"10T3";"10S3";"10S2";"10S1";"10S0";"10S7";"10S6";"10DH";"10AB";"10AA";"10A5";"10KF";"10KE";"10K0";"10JX";"10HU";"10KF";"10KE";"10K0";"10JX";"10HU";"20E4";"20E3";"20DA";"20D9";"20DA";"20D9";"20E8";"20E6";"20GB";"20G9";"20GB";"20G9";"20GB";"20G9";"20GB";"20G9";"20GB";"20G9";"20GB";"20G9";"20HV";"20HT";"20LR";"20LQ";"20LR";"20LQ";"20LR";"20LQ";"20LR";"20LQ";"20LR";"20LQ";"20LR";"20LQ";"20LR";"20LQ";"20LR";"20LQ";"20GK";"20GJ";"20GK";"20GJ";"20GK";"20GJ";"20J2";"20J1";"20KD";"20KC";"20KD";"20KC";"20KD";"20KC";"20KD";"20KC";"20KD";"20KC";"20KD";"20KC";"20KD";"20KC";"20KD";"20KC";"20MX";"20MW";"20MX";"20MW";"20MX";"20MW";"20MX";"20MW";"20MX";"20MW";"20KM";"20KL";"20KM";"20KL";"20KM";"20KL";"20KM";"20KL";"20KM";"20KL";"20KM";"20KL";"20KM";"20KL";"20MV";"20MU";"20MV";"20MU";"20MV";"20MU";"20MV";"20MU";"20MV";"20MU";"20EU";"20ET";"20EX";"20H2";"20H1";"20H2";"20H1";"20H2";"20H1";"20H2";"20H1";"20H4";"20KQ";"20KN";"20KU";"20N9";"20N8";"20N9";"20N8";"20NG";"20NG";"20NE";"20NE";"20EW";"20EV";"20EY";"20H6";"20H5";"20H6";"20H5";"20H6";"20H5";"20H6";"20H5";"20H8";"20KT";"20KS";"20KV";"20NC";"20NB";"20NC";"20NB";"20NF";"20NF";"20M6";"20M5";"20M6";"20M5";"20M6";"20M5";"20M6";"20M5";"20M8";"20M7";"20M8";"20M7";"20M8";"20M7";"20M8";"20M7";"20NS";"20NR";"20NS";"20NR";"20NS";"20NR";"20NU";"20NT";"20NU";"20NT";"20NU";"20NT";"20DT";"20DS";"20DT";"20DS";"20DT";"20DS";"20DT";"20DS";"20FV";"20FU";"20FV";"20FU";"20J5";"20J4";"20JV";"20JU";"20JV";"20JU";"20JV";"20JU";"20JV";"20JU";"20LT";"20LS";"20LT";"20LS";"20LT";"20LS";"20LT";"20LS";"20LT";"20LS";"20Q6";"20Q5";"20Q6";"20Q5";"20F2";"20F1";"20F2";"20F1";"20F2";"20F1";"20F2";"20F1";"20F2";"20F1";"20F2";"20F1";"20J9";"20J8";"20J9";"20J8";"20J9";"20J8";"20J9";"20J8";"20JR";"20JQ";"20JR";"20JQ";"20JR";"20JQ";"20JR";"20JQ";"20JR";"20JQ";"20JR";"20JQ";"20JR";"20JQ";"20LX";"20LW";"20LX";"20LW";"20LX";"20LW";"20LX";"20LW";"20LX";"20LW";"20Q8";"20Q7";"20Q8";"20Q7";"20ME";"20MD";"20ME";"20MD";"20ME";"20MD";"20ME";"20MD";"20ME";"20MD";"20QU";"20QT";"20QU";"20QT";"20GR";"20GQ";"20GR";"20GQ";"20GR";"20GQ";"20RJ";"20RH";"20RJ";"20RH";"20RJ";"20RH";"20EQ";"20EN";"20EQ";"20EN";"20EQ";"20EN";"20FL";"20FK";"20FL";"20FK";"20FL";"20FK";"20FL";"20FK";"20HJ";"20HH";"20HJ";"20HH";"20HJ";"20HH";"20MN";"20MM";"20MN";"20MM";"20MN";"20MM";"20MN";"20MM";"20HC";"20HB";"20HC";"20HB";"20HC";"20HB";"20HC";"20HB";"20K0";"20JY";"20K0";"20JY";"20K0";"20JY";"20K0";"20JY";"20K0";"20JY";"20K0";"20JY";"20K0";"20JY";"20MA";"20M9";"20MA";"20M9";"20MA";"20M9";"20MA";"20M9";"20MA";"20M9";"20LC";"20LB";"20LC";"20LB";"20LC";"20LB";"20LC";"20LB";"20LC";"20LB";"20QQ";"20QN";"20QQ";"20QN";"20QQ";"20QN";"20QQ";"20QN";"20N7";"20N6";"20N7";"20N6";"20N7";"20N6";"20ES";"20ER";"20ES";"20ER";"20ES";"20ER";"20ES";"20ER";"20HL";"20HK";"20HL";"20HK";"20HL";"20HK";"20MC";"20MB";"20MC";"20MB";"20MC";"20MB";"20MC";"20MB";"20MC";"20MB";"20QS";"20QR";"20QS";"20QR";"20QS";"20QR";"20QS";"20QR";"20B7";"20B6";"20B7";"20B6";"20B7";"20B6";"20B7";"20B6";"20B7";"20B6";"20B7";"20B6";"20AW";"20AN";"20AW";"20AN";"20AR";"20AQ";"20AR";"20AQ";"20AR";"20AQ";"20AR";"20AQ";"20AR";"20AQ";"20AR";"20AQ";"20DJ";"20BV";"20BU";"20BX";"20BW";"20FN";"20FM";"20FN";"20FM";"20FN";"20FM";"20FN";"20FM";"20FX";"20FW";"20FX";"20FW";"20FX";"20FW";"20FA";"20F9";"20FA";"20F9";"20FA";"20F9";"20FA";"20F9";"20HE";"20HD";"20HE";"20HD";"20HE";"20HD";"20HE";"20HD";"20JN";"20JM";"20JN";"20JM";"20JN";"20JM";"20JN";"20JM";"20JN";"20JM";"20JN";"20JM";"20JN";"20JM";"20J7";"20J6";"20HG";"20HF";"20HG";"20HF";"20HG";"20HF";"20HG";"20HF";"20JT";"20JS";"20JT";"20JS";"20JT";"20JS";"20JT";"20JS";"20JT";"20JS";"20JT";"20JS";"20JT";"20JS";"20L6";"20L5";"20L6";"20L5";"20L6";"20L5";"20L6";"20L5";"20L6";"20L5";"20L8";"20L7";"20L8";"20L7";"20L8";"20L7";"20L8";"20L7";"20L8";"20L7";"20N3";"20N2";"20N3";"20N2";"20N3";"20N2";"20QH";"20Q9";"20QH";"20Q9";"20QH";"20Q9";"20NY";"20NX";"20NY";"20NX";"20NY";"20NX";"20NY";"20NX";"20NK";"20NJ";"20NK";"20NJ";"20NK";"20NJ";"20QK";"20QJ";"20QK";"20QJ";"20QK";"20QJ";"20BF";"20BE";"20BF";"20BE";"20BF";"20BE";"20BF";"20BE";"20BF";"20BE";"20CK";"20CJ";"20CK";"20CJ";"20CK";"20CJ";"20CK";"20CJ";"20FJ";"20FH";"20FJ";"20FH";"20FJ";"20FH";"20FJ";"20FH";"20HA";"20H9";"20HA";"20H9";"20HA";"20H9";"20HA";"20H9";"20JX";"20JW";"20JX";"20JW";"20JX";"20JW";"20JX";"20JW";"20JX";"20JW";"20JX";"20JW";"20JX";"20JW";"20LA";"20L9";"20LA";"20L9";"20LA";"20L9";"20LA";"20L9";"20LA";"20L9";"20N5";"20N4";"20N5";"20N4";"20N5";"20N4";"20BH";"20BG";"20BH";"20BG";"20BH";"20BG";"20BH";"20BG";"20BH";"20BG";"20EG";"20EF";"20EG";"20EF";"20EG";"20EF";"20EG";"20EF";"20EG";"20EF";"20E2";"20E1";"20E2";"20E1";"20E2";"20E1";"20E2";"20E1";"20A8";"20A7";"20A8";"20A7";"20A8";"20A7";"20A8";"20A7";"20A8";"20A7";"20BT";"20BS";"20BT";"20BS";"20BT";"20BS";"20BT";"20BS";"20FC";"20FB";"20FC";"20FB";"20FC";"20FB";"20FC";"20FB";"20FC";"20FB";"20FC";"20FB";"20HR";"20HQ";"20HR";"20HQ";"20HR";"20HQ";"20HR";"20HQ";"20K4";"20K3";"20K4";"20K3";"20K4";"20K3";"20K4";"20K3";"20K4";"20K3";"20K4";"20K3";"20K4";"20K3";"20KH";"20KG";"20KH";"20KG";"20KH";"20KG";"20KH";"20KG";"20KH";"20KG";"20QE";"20QD";"20QE";"20QD";"20QE";"20QD";"20QE";"20QD";"20R2";"20R1";"20R2";"20R1";"20R2";"20R1";"20MG";"20MF";"20MG";"20MF";"20MG";"20MF";"20MG";"20MF";"20MG";"20MF";"20QW";"20QV";"20QW";"20QV";"20GH";"20GG";"20GH";"20GG";"20GH";"20GG";"20GH";"20GG";"20JC";"20JB";"20JC";"20JB";"20JC";"20JB";"20JC";"20JB";"20KK";"20KJ";"20KK";"20KJ";"20KK";"20KJ";"20KK";"20KJ";"20KK";"20KJ";"20FR";"20FQ";"20FR";"20FQ";"20FR";"20FQ";"20FR";"20FQ";"20FR";"20FQ";"20FR";"20FQ";"20FR";"20FQ";"20JG";"20JF";"20JE";"20JD";"20JG";"20JF";"20JE";"20JD";"20JG";"20JF";"20JE";"20JD";"20JG";"20JF";"20JE";"20JD";"20LG";"20LF";"20LE";"20LD";"20LG";"20LF";"20LE";"20LD";"20LG";"20LF";"20LE";"20LD";"20LG";"20LF";"20LE";"20LD";"20LG";"20LF";"20LE";"20LD";"20QG";"20QF";"20QG";"20QF";"20QG";"20QF";"20QG";"20QF";"20SB";"20SA";"20SB";"20SA";"20SB";"20SA";"3368";"3367";"20BM";"20BM";"20AM";"20AL";"20AM";"20AL";"20AM";"20AL";"20AM";"20AL";"20AM";"20AL";"20AM";"20AL";"20AK";"20AJ";"20AK";"20AJ";"20AK";"20AJ";"20AK";"20AJ";"20AK";"20AJ";"20AK";"20AJ";"20CM";"20CL";"20CM";"20CL";"20CM";"20CL";"20CM";"20CL";"20CM";"20CL";"20CM";"20CL";"20CM";"20CL";"20F6";"20F5";"20F6";"20F5";"20F6";"20F5";"20F6";"20F5";"20HN";"20HM";"20K6";"20K5";"20K6";"20K5";"20K6";"20K5";"20K6";"20K5";"20KF";"20KE";"20KF";"20KE";"20KF";"20KE";"20KF";"20KE";"20KF";"20KE";"20LL";"20LK";"20LJ";"20LH";"20LL";"20LK";"20LJ";"20LH";"20LL";"20LK";"20LJ";"20LH";"20LL";"20LK";"20LJ";"20LH";"20LL";"20LK";"20LJ";"20LH";"20Q1";"20Q0";"20Q1";"20Q0";"20Q1";"20Q0";"20Q1";"20Q0";"20NQ";"20NN";"20NQ";"20NN";"20NQ";"20NN";"20NM";"20NL";"20NM";"20NL";"20NM";"20NL";"20DA";"20D9";"20DA";"20D9";"20E7";"20E5";"20E7";"20E5";"20E7";"20E5";"20GA";"20G8";"20GA";"20G8";"20GA";"20G8";"20GA";"20G8";"20HU";"20HS";"20LN";"20LM";"20LN";"20LM";"20LN";"20LM";"20LN";"20LM";"20LN";"20LM";"20LN";"20LM";"20LN";"20LM";"20LN";"20LM";"20DL";"20DK";"20DN";"20DM";"20DN";"20DM";"20DN";"20DM";"20DR";"20DQ";"20GT";"20GS";"20FE";"20FD";"20GT";"20GS";"20FE";"20FD";"20GT";"20GS";"20FE";"20FD";"20GT";"20GS";"20FE";"20FD";"20GT";"20GS";"20FE";"20FD";"20JJ";"20JH";"20JJ";"20JH";"20JJ";"20JH";"20JJ";"20JH";"20JJ";"20JH";"20JJ";"20JH";"20EM";"20EL";"20EM";"20EL";"20EM";"20EL";"30AH";"30AG";"30AH";"30AG";"30AV";"30AT";"30AV";"30AT";"30BS";"30BK";"30BJ";"30BS";"30BK";"30BJ";"30BS";"30BK";"30BJ";"30C3";"30C2";"30C1";"30C3";"30C2";"30C1";"30BR";"30BH";"30BG";"30BR";"30BH";"30BG";"30BR";"30BH";"30BG";"30CA";"30C8";"30C7";"30CA";"30C8";"30C7";"30D2";"30D1";"30D2";"30D1";"30CG";"30CF";"30CE";"30CG";"30CF";"30CE";"30CG";"30CF";"30CE";"30CG";"30CF";"30CE";"30C9";"30C6";"30C5";"30D0";"30CY";"30B3";"30B2";"30B3";"30B2";"30A7";"30A6";"30B5";"30B4";"30B5";"30B4";"30BQ";"30BF";"30BE";"30C0";"30BY";"30BX";"30A9";"30A8";"30B7";"30B7";"30BU";"30BB";"30BA";"30A5";"30A4";"30B9";"30B9";"30BV";"30BD";"30BC";"10TC";"10TA";"10T9";"10T8";"10T7";"10U7";"10U6";"10TR";"10SV";"10SU";"10ST";"10U5";"10U4";"10TQ";"10SS";"10SR";"10SQ";"10V8";"10UH";"10T2";"10T1";"10SY";"10RU";"10RT";"10RS";"10RR";"10U3";"10U2";"10TN";"10SL";"10SK";"10SJ";"10U1";"10U0";"10TM";"10SH";"10SG";"10SF";"10UJ";"10T6";"10T5";"10T3";"10S3";"10S2";"10S1";"10S0";"30CG";"30CF";"30CE";"10S7";"10S6";"20MX";"20MW";"20MV";"20MU";"20NS";"20NR";"20NU";"20NT";"20LT";"20LS";"20F2";"20F1";"20F2";"20F1";"20J9";"20J8";"20JR";"20JQ";"20LX";"20LW";"20ME";"20MD";"20EQ";"20EN";"20FL";"20FK";"20HC";"20HB";"20K0";"20JY";"20ES";"20ER";"20FA";"20F9";"20HE";"20HD";"20JN";"20JM";"20HG";"20HF";"20JT";"20JS";"20L6";"20L5";"20L8";"20L7";"20FJ";"20FH";"20HA";"20H9";"20JX";"20JW";"20FC";"20FB";"20HR";"20HQ";"20K4";"20K3";"20KH";"20KG";"20MG";"20MF";"20JC";"20JB";"20FR";"20FQ";"20JG";"20JF";"20JE";"20JD";"20GT";"20GS";"20FE";"20FD";"20LC";"20LB";"20LA";"20L9";"11AE";"11AD";"11AE";"11AD";"11AE";"11AD";"20FN";"20FM";"20FN";"20FM";"20LG";"20LF";"20LE";"20LD";"20F6";"20F5";"20F6";"20F5";"20KF";"20KE";"20NQ";"20NN";"20KQ";"20KN";"20N9";"20N8";"20NG";"20NE";"20KT";"20KS";"20NC";"20NB";"20NF";"20NK";"20NJ";"20QK";"20QJ";"20NM";"20NL";"20QU";"20QT";"20QW";"20QV";"11A7";"11A5";"11A4";"11A7";"11A5";"11A4";"11A7";"11A5";"11A4";"11A7";"11A5";"11A4";"11AW";"11AV";"11AA";"11A9";"11AW";"11AV";"11AA";"11A9";"11AW";"11AV";"11AA";"11A9";"11AW";"11AV";"11AA";"11A9";"20R4";"20R3";"20R4";"20R3";"20R4";"20R3";"20R4";"20R3";"20R6";"20R5";"20R6";"20R5";"20R6";"20R5";"20R6";"20R5";"20ES";"20ER";"20ES";"20ER";"20ES";"20ER";"20JG";"20JF";"20JE";"20JD";"30D2";"30D1";"30D0";"30CY";"30D0";"30CY";"11BE";"11BD";"11BE";"11BD";"11A7";"11A5";"11A4";"11A7";"11A5";"11A4";"11A7";"11A5";"11A4";"11A7";"11A5";"11A4";"11AW";"11AV";"11AA";"11A9";"11AW";"11AV";"11AA";"11A9";"11AW";"11AV";"11AA";"11A9";"11AW";"11AV";"11AA";"11A9";"20RB";"20RA";"20RB";"20RA";"20RB";"20RA";"20RE";"20RD";"20RE";"20RD";"20RE";"20RD";"20Q6";"20Q5";"20Q6";"20Q5";"20Q6";"20Q5";"20Q8";"20Q7";"20Q8";"20Q7";"20Q8";"20Q7";"20RJ";"20RH";"20HJ";"20HH";"20MN";"20MM";"20MA";"20M9";"20N7";"20N6";"20HL";"20HK";"20MC";"20MB";"20RC";"20RC";"20RC";"20FX";"20FW";"20FX";"20FW";"20FX";"20FW";"20FX";"20FW";"20FX";"20FW";"20N3";"20N2";"20RY";"20RX";"20RY";"20RX";"20RY";"20RX";"20QH";"20Q9";"20NY";"20NX";"20N5";"20N4";"20Q1";"20Q0";"20SD";"20SC";"20SF";"20SE";"20SF";"20SE";"11AE";"11AD";"20M6";"20M5";"20M6";"20M5";"20M8";"20M7";"20M8";"20M7";"20QU";"20QT";"20QU";"20QT";"20QE";"20QD";"20R2";"20R1";"20QW";"20QV";"20QW";"20QV";"20KK";"20KJ";"20QG";"20QF";"20SB";"20SA";"20LL";"20LK";"20LJ";"20LH";"20JJ";"20JH";|]

    [<Test>]
    [<Category(TestCategory.ManualTests)>]
    let getRemoteUpdatesDownloadAllUpatesForAllModels () =
        match(result{
            let! manufacturer = getManufacturerForCurrentSystem()
            let allmodelCodes = allModels20200521 |> Array.map(fun mc -> ModelCode.createUnsafe mc false)
            let! operatingSystemCode = OperatingSystemCode.create "WIN10X64" false
            let! logDirectory = FileSystem.path @"c:\temp"
            let! patterns = (RegExp.toRegexPatterns [||] true)            
            let! cacheFolderPath = FileSystem.path @"C:\Temp\LenovoUpdatePackagesXml2"
            let packageInfoResults =
                allmodelCodes|>Array.map(fun modelCode -> 
                    let updatesRetrievalContext = toUpdatesRetrievalContext manufacturer modelCode operatingSystemCode true logDirectory cacheFolderPath false patterns
                    let packageInfos = DriverTool.LenovoUpdates.getRemoteUpdates logger cacheFolderPath updatesRetrievalContext
                    packageInfos
                    )
            return packageInfoResults        
        })with
        |Result.Ok v -> Assert.IsTrue(true)
        |Result.Error ex -> Assert.Fail(ex.Message)

    [<Test>]
    [<Timeout(3600000)>]
    [<Category(TestCategory.ManualTests)>]
    let getLocalUpdatesP50Test () =
        match(result{
            let! manufacturer = getManufacturerForCurrentSystem()
            let! modelCode = ModelCode.create "" true
            let! operatingSystemCode = OperatingSystemCode.create "WIN10X64" false
            let! logDirectory = FileSystem.path @"c:\temp"
            let! patterns = (RegExp.toRegexPatterns [||] true)            
            let! cacheFolderPath = FileSystem.path @"C:\Temp\LenovoUpdatePackagesXml2"
            let updatesRetrievalContext = toUpdatesRetrievalContext manufacturer modelCode operatingSystemCode true logDirectory cacheFolderPath false patterns
            let! updates = DriverTool.LenovoUpdates.getLocalUpdates2 logger cacheFolderPath updatesRetrievalContext        
            return updates
        })with
        |Result.Ok u -> Assert.IsTrue ((Array.length u) > 0,"")
        |Result.Error ex -> Assert.Fail(ex.ToString())