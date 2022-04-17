Framework '4.8'

properties {
    $rootFolder = $(if([System.String]::IsNullOrWhiteSpace($PSScriptRoot)){(Get-Location).Path}else{$PSScriptRoot})    
	$projectName = "DriverTool"
    $artifactsFolder = [System.IO.Path]::Combine($rootFolder,"artifacts")
    $buildFolder = [System.IO.Path]::Combine($rootFolder,"build")
    $buildAppFolder = [System.IO.Path]::Combine($rootFolder,"build","app")
    $srcFolder = [System.IO.Path]::Combine($rootFolder,"src")
    $buildSetupFolder = [System.IO.Path]::Combine($buildFolder,"setup")
    $PSRepositoryName = "DriverToolPSRepository"    
    $myDocuments = [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::MyDocuments)
    $templatesStoreFolder = [System.IO.Path]::Combine($myDocuments,"Templates",$templatesStoreName)
}

task default -depends Publish

task Clean {    
    Unregister-PSRepository -Name $PSRepositoryName -ErrorAction SilentlyContinue
    Remove-Item -Path $artifactsFolder -Force -Recurse -ErrorAction SilentlyContinue
    New-Item -Path $artifactsFolder -ItemType Directory -Force | Out-Null
    #Remove-Item -Path $buildFolder -Force -Recurse -ErrorAction SilentlyContinue
    New-Item -Path $buildFolder -ItemType Directory -Force | Out-Null
    Remove-Item -Path "$artifactsFolder\DriverTool.*.zip" -Force
    Get-ChildItem -LiteralPath $rootFolder -Filter "nunit-agent_*.log" | Remove-Item -Force -ErrorAction SilentlyContinue
    Write-Host "Cleaned!" -ForegroundColor Green
}

task Build -depends Clean {
    Exec{        
        fake run "Build.fsx" target "Default"        
    }    
}

task ModuleRepository -depends Clean {    
    $PSModuleRepositoryDirectory = New-Item -Path $artifactsFolder -Name $PSRepositoryName -ItemType Directory -Force
    Register-PSRepository -Name $PSRepositoryName -SourceLocation "$($PSModuleRepositoryDirectory.FullName)" -PublishLocation "$($PSModuleRepositoryDirectory.FullName)" -InstallationPolicy Trusted
    Write-Host "Module repository registered!" -ForegroundColor Green
}

task PublishModule -depends ModuleRepository {
    $moduleFolder = [System.IO.Path]::Combine($rootFolder,"modules","DriverTool.PowerCLI")
    Publish-Module -Path $moduleFolder -Repository $PSRepositoryName
    Write-Host "DriverTool.PowerCLI module published!" -ForegroundColor Green
}

task PublishApp -depends UnitTests  {
    $assemblyVersion = (Get-Item "$buildAppFolder\DriverTool.exe").VersionInfo.FileVersion    
    Compress-Archive -Path "$buildAppFolder\**"  -DestinationPath "$artifactsFolder\DriverTool.$assemblyVersion.zip"
}

task Publish -depends PublishModule, PublishApp {
    Write-Host "Published!" -ForegroundColor Green
}

task UnitTests -depends Build {    
    $nunitConsoleRunner = Get-ChildItem -LiteralPath "$rootFolder\packages\nunit.consolerunner" -Filter "nunit3-console.exe" -Recurse | Select-Object -Last 1
    if($null -eq $nunitConsoleRunner){ throw "Not found: nunit3-console.exe"}    
	$tests = (Get-ChildItem -LiteralPath $buildFolder -Recurse -Filter "*Tests.dll") | ForEach-Object {$_.FullName}    
	Exec {
        Write-Host "Running: . $($nunitConsoleRunner.FullName) --noheader --where=cat==UnitTests --trace=Off $tests"
        & $($nunitConsoleRunner.FullName) --noheader --where=cat==UnitTests --trace=Off $tests    
    }
}

function Update-DtFsProjectVersion
{
    param(
        [Parameter(Mandatory=$true)] 
        [string]
        $Path,
        [Parameter(Mandatory=$true)]
        [string]
        $ModuleVersion,
        [Parameter(Mandatory=$false)]
        [int]
        $Revision=0
    )
    Write-Host "Updating project: $Path"
    $xml = [xml](Get-Content $Path)
    #Write-Host "$($xml.OuterXml)"
    $xml.Project.PropertyGroup[0].AssemblyVersion = "$($ModuleVersion).$($Revision)"
    $xml.Project.PropertyGroup[0].FileVersion = "$($ModuleVersion).$($Revision)"
    $xml.Project.PropertyGroup[0].Version = $ModuleVersion
    $xml.Save($Path)
}

function Update-DtPSModuleVersion
{
    param(
        [Parameter(Mandatory=$true)] 
        [string]
        $Path,
        [Parameter(Mandatory=$true)]
        [string]
        $ModuleVersion        
    )
    Write-Host "Updating Module File: $Path"
    $content = Get-Content -Path $Path
    $content | ForEach-Object {
        [System.Text.RegularExpressions.Regex]::Replace($_,"^\s*ModuleVersion\s*=\s*'\d+\.\d+\.\d+'\s*$","    ModuleVersion = '$ModuleVersion'")
    } | Set-Content -Path $Path
}

function Get-DtRevisionVersion
{
    param(
        [Parameter(Mandatory=$true)] 
        [string]
        $Path               
    )
    $content = Get-Content -Path $Path
    try {
        $revision = [System.Convert]::ToInt16($content.Trim())
    }
    catch {
        $revision = 0
    }
    "$revision" | Set-Content -Path $Path | Out-Null
    "$revision"
}

task UpdateVersion {
    $DayOfYear = "$((Get-Date).DayOfYear + $AddDaysToBuildVersion)".PadLeft(3,'0')
    $ModuleVersion = "1.0.$((Get-Date).Year - 2000)$DayOfYear"
	$psModuleFile = [System.IO.Path]::Combine($rootFolder,"modules","DriverTool.PowerCLI","DriverTool.PowerCLI.psd1")    
    Update-DtPSModuleVersion -Path $psModuleFile -ModuleVersion $ModuleVersion
    $revisionFile = [System.IO.Path]::Combine($rootFolder,"revision.txt")
    $revision = Get-DtRevisionVersion -Path $revisionFile
    $projectFiles = @(
            [System.IO.Path]::Combine($rootFolder,"src","app","DriverTool","DriverTool.fsproj")            
            [System.IO.Path]::Combine($rootFolder,"src","app","DriverTool.PowerCLI.Library.FSharp","DriverTool.PowerCLI.Library.FSharp.fsproj"),
            [System.IO.Path]::Combine($rootFolder,"src","app","DriverTool.Library","DriverTool.Library.fsproj"),
            [System.IO.Path]::Combine($rootFolder,"src","app","DriverTool.DupExitCode2ExitCode","DriverTool.DupExitCode2ExitCode.fsproj"),
            [System.IO.Path]::Combine($rootFolder,"src","app","DriverTool.DpInstExitCode2ExitCode","DriverTool.DpInstExitCode2ExitCode.fsproj"),
            [System.IO.Path]::Combine($rootFolder,"src","test","DriverTool.Tests","DriverTool.Tests.fsproj"),
            [System.IO.Path]::Combine($rootFolder,"src","test","DriverTool.PowerCLI.Library.Tests","DriverTool.PowerCLI.Library.Tests.fsproj"),
            [System.IO.Path]::Combine($rootFolder,"src","app","DriverTool.PowerCLI.Library.CSharp","DriverTool.PowerCLI.Library.CSharp.csproj"),
            [System.IO.Path]::Combine($rootFolder,"src","app","DriverTool.CSharpLib","DriverTool.CSharpLib.csproj")
        )
    $projectFiles | Foreach-Object{ Update-DtFsProjectVersion -Path $_ -ModuleVersion $ModuleVersion -Revision $revision}
}