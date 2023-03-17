Framework '4.8'

properties {
    $rootFolder = $(if([System.String]::IsNullOrWhiteSpace($PSScriptRoot)){(Get-Location).Path}else{$PSScriptRoot})
	$projectName = "DriverTool"
    $artifactsFolder = [System.IO.Path]::Combine($rootFolder,"artifacts")
    $buildFolder = [System.IO.Path]::Combine($rootFolder,"build")
    $buildAppFolder = [System.IO.Path]::Combine($rootFolder,"build","app")
    $srcFolder = [System.IO.Path]::Combine($rootFolder,"src")
    $modulesFolder = [System.IO.Path]::Combine($rootFolder,"modules")
    $modulesBinaryFolder = [System.IO.Path]::Combine($rootFolder,"modules","DriverTool.PowerCLI","binary")
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
    Remove-Item -Path $buildFolder -Force -Recurse -ErrorAction SilentlyContinue
    New-Item -Path $buildFolder -ItemType Directory -Force | Out-Null            
    
    Remove-Item -Path $("$srcFolder\app\DriverTool\bin") -Force -Recurse -ErrorAction SilentlyContinue
    Remove-Item -Path $("$srcFolder\app\DriverTool\obj") -Force -Recurse -ErrorAction SilentlyContinue

    Remove-Item -Path $("$srcFolder\app\DriverTool.CSharpLib\bin") -Force -Recurse -ErrorAction SilentlyContinue
    Remove-Item -Path $("$srcFolder\app\DriverTool.CSharpLib\obj") -Force -Recurse -ErrorAction SilentlyContinue

    Remove-Item -Path $("$srcFolder\app\DriverTool.DpInstExitCode2ExitCode\bin") -Force -Recurse -ErrorAction SilentlyContinue
    Remove-Item -Path $("$srcFolder\app\DriverTool.DpInstExitCode2ExitCode\obj") -Force -Recurse -ErrorAction SilentlyContinue

    Remove-Item -Path $("$srcFolder\app\DriverTool.DupExitCode2ExitCode\bin") -Force -Recurse -ErrorAction SilentlyContinue
    Remove-Item -Path $("$srcFolder\app\DriverTool.DupExitCode2ExitCode\obj") -Force -Recurse -ErrorAction SilentlyContinue

    Remove-Item -Path $("$srcFolder\app\DriverTool.Library\bin") -Force -Recurse -ErrorAction SilentlyContinue
    Remove-Item -Path $("$srcFolder\app\DriverTool.Library\obj") -Force -Recurse -ErrorAction SilentlyContinue

    Remove-Item -Path $("$srcFolder\app\DriverTool.UI\bin") -Force -Recurse -ErrorAction SilentlyContinue
    Remove-Item -Path $("$srcFolder\app\DriverTool.UI\obj") -Force -Recurse -ErrorAction SilentlyContinue

    Remove-Item -Path $("$srcFolder\app\DriverTool.PowerCLI.Library.CSharp\bin") -Force -Recurse -ErrorAction SilentlyContinue
    Remove-Item -Path $("$srcFolder\app\DriverTool.PowerCLI.Library.CSharp\obj") -Force -Recurse -ErrorAction SilentlyContinue

    Remove-Item -Path $("$srcFolder\app\DriverTool.PowerCLI.Library.FSharp\bin") -Force -Recurse -ErrorAction SilentlyContinue
    Remove-Item -Path $("$srcFolder\app\DriverTool.PowerCLI.Library.FSharp\obj") -Force -Recurse -ErrorAction SilentlyContinue

    Remove-Item -Path $("$srcFolder\test\DriverTool.Tests\bin") -Force -Recurse -ErrorAction SilentlyContinue
    Remove-Item -Path $("$srcFolder\test\DriverTool.Tests\obj") -Force -Recurse -ErrorAction SilentlyContinue

    Remove-Item -Path $("$srcFolder\test\DriverTool.PowerCLI.Library.Tests\bin") -Force -Recurse -ErrorAction SilentlyContinue
    Remove-Item -Path $("$srcFolder\test\DriverTool.PowerCLI.Library.Tests\obj") -Force -Recurse -ErrorAction SilentlyContinue

    Remove-Item -Path $("$modulesFolder\DriverTool.PowerCLI\internal\tools\DriverTool") -Force -Recurse -ErrorAction SilentlyContinue

    Remove-Item -Path "$artifactsFolder\DriverTool.*.zip" -Force

    Write-Host "Cleaned!" -ForegroundColor Green
}

task RestorePackages -depends Clean {
    exec { msbuild "$rootFolder\$projectName.sln" -t:restore -p:RestorePackagesConfig=true }
}

task BuildApp -depends Clean, RestorePackages {
    exec { msbuild /t:build /v:q /nologo /p:WarningLevel=0 /p:Configuration="Release" /p:Platform=AnyCpu "$srcFolder\app\DriverTool.DpInstExitCode2ExitCode\DriverTool.DpInstExitCode2ExitCode.fsproj" /p:OutputPath="$buildFolder\app\DriverTool.DpInstExitCode2ExitCode\net48" }
    exec { msbuild /t:build /v:q /nologo /p:WarningLevel=0 /p:Configuration="Release" /p:Platform=AnyCpu "$srcFolder\app\DriverTool.DupExitCode2ExitCode\DriverTool.DupExitCode2ExitCode.fsproj" /p:OutputPath="$buildFolder\app\DriverTool.DupExitCode2ExitCode\net48" }
    exec { msbuild /t:build /v:q /nologo /p:WarningLevel=0 /p:Configuration="Release" /p:Platform=AnyCpu "$srcFolder\app\DriverTool.CSharpLib\DriverTool.CSharpLib.csproj" /p:OutputPath="$buildFolder\app\DriverTool.CSharpLib\net48" }
    exec { msbuild /t:build /v:q /nologo /p:WarningLevel=0 /p:Configuration="Release" /p:Platform=AnyCpu "$srcFolder\app\DriverTool.Library\DriverTool.Library.fsproj" /p:OutputPath="$buildFolder\app\DriverTool.Library\net48" }
    exec { msbuild /t:build /v:q /nologo /p:WarningLevel=0 /p:Configuration="Release" /p:Platform=AnyCpu "$srcFolder\app\DriverTool.UI\DriverTool.UI.csproj" /p:OutputPath="$buildFolder\app\DriverTool.UI\net48" }
    exec { msbuild /t:build /v:q /nologo /p:WarningLevel=0 /p:Configuration="Release" /p:Platform=AnyCpu "$srcFolder\app\DriverTool\DriverTool.fsproj" /p:OutputPath="$buildFolder\app\DriverTool\net48" }
    exec { msbuild /t:build /v:q /nologo /p:WarningLevel=0 /p:Configuration="Release" /p:Platform=AnyCpu "$srcFolder\app\DriverTool.PowerCLI.Library.CSharp\DriverTool.PowerCLI.Library.CSharp.csproj" /p:OutputPath="$modulesBinaryFolder\DriverTool.PowerCLI.Library.CSharp" }
    exec { msbuild /t:build /v:q /nologo /p:WarningLevel=0 /p:Configuration="Release" /p:Platform=AnyCpu "$srcFolder\app\DriverTool.PowerCLI.Library.FSharp\DriverTool.PowerCLI.Library.FSharp.fsproj" /p:OutputPath="$modulesBinaryFolder\DriverTool.PowerCLI.Library.FSharp" }
    Write-Host "BuildApp!"
}

task BuildDocumentation -depends BuildApp {
    Write-Host "Building documentation..." -ForegroundColor Green
    
    exec { . ".\tools\XmlDoc2CmdletDoc\XmlDoc2CmdletDoc.exe" -strict  @([System.IO.Path]::Combine($modulesBinaryFolder,"DriverTool.PowerCLI.Library.CSharp","DriverTool.PowerCLI.Library.CSharp.dll"))}
    exec { . ".\tools\XmlDoc2CmdletDoc\XmlDoc2CmdletDoc.exe" -strict  @([System.IO.Path]::Combine($modulesBinaryFolder,"DriverTool.PowerCLI.Library.FSharp","DriverTool.PowerCLI.Library.FSharp.dll"))}

    Write-Host "Documentation build!" -ForegroundColor Green
}

task BuildTest -depends BuildApp {
    
    exec { msbuild /t:build /v:q /nologo /p:WarningLevel=0 /p:Configuration="Release" /p:Platform=AnyCpu "$srcFolder\test\DriverTool.Tests\DriverTool.Tests.fsproj" /p:OutputPath="$buildFolder\test\DriverTool.Tests\net48" }
    exec { msbuild /t:build /v:q /nologo /p:WarningLevel=0 /p:Configuration="Release" /p:Platform=AnyCpu "$srcFolder\test\DriverTool.PowerCLI.Library.Tests\DriverTool.PowerCLI.Library.Tests.fsproj" /p:OutputPath="$buildFolder\test\DriverTool.PowerCLI.Library.Tests\net48" }

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

task UnitTests -depends BuildTest {    
    $nunitConsoleRunner = Get-ChildItem -LiteralPath "$($env:USERPROFILE)\.nuget\packages\nunit.consolerunner" -Filter "nunit3-console.exe" -Recurse | Select-Object -Last 1
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