namespace DriverTool.Tests

module CreateDriverPackageActorTests =
    open NUnit.Framework
    open DriverTool.Library.Messages

    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    let ``isPackagingFinished progress all fifty percent return false`` () =
        
        let packagingProgress = 
            {
                PackagingProgress.Started=true
                PackageDownloads = { Total = 2; Value= 1; Name= "Package Downloads"}
                PackageExtracts = { Total = 2; Value= 1; Name= "Package Extracts"}
                SccmPackageDownloads = { Total = 2; Value= 1; Name= "Sccm Package Download"}
                SccmPackageExtracts = { Total = 2; Value= 1; Name= "Sccm Package Extracts"}                
                Finished=false
            }
        let actual = DriverTool.CreateDriverPackageActor.isPackagingFinished packagingProgress
        Assert.IsFalse(actual)  


    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    let ``isPackagingFinished progress all 100 percent return true`` () =
            
        let packagingProgress = 
            {
                PackagingProgress.Started=true
                PackageDownloads = { Total = 2; Value= 2; Name= "Package Downloads"}
                PackageExtracts = { Total = 2; Value= 2; Name= "Package Extracts"}
                SccmPackageDownloads = { Total = 2; Value= 2; Name= "Sccm Package Download"}
                SccmPackageExtracts = { Total = 2; Value= 2; Name= "Sccm Package Extracts"}                
                Finished=false
            }
        let actual = DriverTool.CreateDriverPackageActor.isPackagingFinished packagingProgress
        Assert.IsTrue(actual)


    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    let ``start and done progress tests`` () =
               
        let intialPackagingProgress = 
            {
                PackagingProgress.Started=false
                PackageDownloads = { Total = 0; Value= 0; Name= "Package Downloads"}
                PackageExtracts = { Total = 0; Value= 0; Name= "Package Extracts"}
                SccmPackageDownloads = { Total = 0; Value= 0; Name= "Sccm Package Download"}
                SccmPackageExtracts = { Total = 0; Value= 0; Name= "Sccm Package Extracts"}                
                Finished=false
            }
        Assert.IsFalse(DriverTool.CreateDriverPackageActor.isPackagingFinished intialPackagingProgress,"intialPackagingProgress")
        let startProgress = startPackageDownload' intialPackagingProgress
        Assert.IsFalse(DriverTool.CreateDriverPackageActor.isPackagingFinished startProgress,"startPackageDownloadProgress")
        Assert.AreEqual(1,startProgress.PackageDownloads.Total)
        Assert.AreEqual(0,startProgress.PackageDownloads.Value)
        let doneProgress = donePackageDownload' startProgress
        Assert.IsTrue(DriverTool.CreateDriverPackageActor.isPackagingFinished doneProgress,"donePackageDownloadProgress")
        Assert.AreEqual(1,doneProgress.PackageDownloads.Total)
        Assert.AreEqual(1,doneProgress.PackageDownloads.Value)


        Assert.IsFalse(DriverTool.CreateDriverPackageActor.isPackagingFinished intialPackagingProgress,"intialPackagingProgress")
        let startProgress = startSccmPackageDownload' intialPackagingProgress
        Assert.IsFalse(DriverTool.CreateDriverPackageActor.isPackagingFinished startProgress,"startSccmPackageDownloads")
        Assert.AreEqual(1,startProgress.SccmPackageDownloads.Total)
        Assert.AreEqual(0,startProgress.SccmPackageDownloads.Value)
        let doneProgress = doneSccmPackageDownload' startProgress
        Assert.IsTrue(DriverTool.CreateDriverPackageActor.isPackagingFinished doneProgress,"doneSccmPackageDownloads")
        Assert.AreEqual(1,doneProgress.SccmPackageDownloads.Total)
        Assert.AreEqual(1,doneProgress.SccmPackageDownloads.Value)

        Assert.IsFalse(DriverTool.CreateDriverPackageActor.isPackagingFinished intialPackagingProgress,"intialPackagingProgress")
        let startProgress = startPackageExtract' intialPackagingProgress
        Assert.IsFalse(DriverTool.CreateDriverPackageActor.isPackagingFinished startProgress,"startPackageExtracts")
        Assert.AreEqual(1,startProgress.PackageExtracts.Total)
        Assert.AreEqual(0,startProgress.PackageExtracts.Value)
        let doneProgress = donePackageExtract' startProgress
        Assert.IsTrue(DriverTool.CreateDriverPackageActor.isPackagingFinished doneProgress,"donePackageExtracts")
        Assert.AreEqual(1,doneProgress.PackageExtracts.Total)
        Assert.AreEqual(1,doneProgress.PackageExtracts.Value)

        Assert.IsFalse(DriverTool.CreateDriverPackageActor.isPackagingFinished intialPackagingProgress,"intialPackagingProgress")
        let startProgress = startSccmPackageExtract' intialPackagingProgress
        Assert.IsFalse(DriverTool.CreateDriverPackageActor.isPackagingFinished startProgress,"startSccmPackageExtracts")
        Assert.AreEqual(1,startProgress.SccmPackageExtracts.Total)
        Assert.AreEqual(0,startProgress.SccmPackageExtracts.Value)
        let doneProgress = doneSccmPackageExtract' startProgress
        Assert.IsTrue(DriverTool.CreateDriverPackageActor.isPackagingFinished doneProgress,"doneSccmPackageExtracts")
        Assert.AreEqual(1,doneProgress.SccmPackageExtracts.Total)
        Assert.AreEqual(1,doneProgress.SccmPackageExtracts.Value)
