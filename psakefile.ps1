Framework '4.8'

properties {
    $rootFolder = $(if([System.String]::IsNullOrWhiteSpace($PSScriptRoot)){(Get-Location).Path}else{$PSScriptRoot})    
	$projectName = "DriverTool"
    $artifactsFolder = [System.IO.Path]::Combine($rootFolder,"artifacts")
    $buildFolder = [System.IO.Path]::Combine($rootFolder,"build")
    $srcFolder = [System.IO.Path]::Combine($rootFolder,"src")
    $buildSetupFolder = [System.IO.Path]::Combine($buildFolder,"setup")
    $PSRepositoryName = "DriverToolPSRepository"    
    $myDocuments = [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::MyDocuments)
    $templatesStoreFolder = [System.IO.Path]::Combine($myDocuments,"Templates",$templatesStoreName)
}

task default -depends Publish

task Clean {    
    Unregister-PSRepository -Name $PSRepositoryName -ErrorAction SilentlyContinue
    #Remove-Item -Path $artifactsFolder -Force -Recurse -ErrorAction SilentlyContinue
    New-Item -Path $artifactsFolder -ItemType Directory -Force | Out-Null
    #Remove-Item -Path $buildFolder -Force -Recurse -ErrorAction SilentlyContinue
    New-Item -Path $buildFolder -ItemType Directory -Force | Out-Null            
    Write-Host "Cleaned!" -ForegroundColor Green
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

task Publish -depends PublishModule {
    Write-Host "Published!" -ForegroundColor Green
}

task UpdateVersion {
    $DayOfYear = "$((Get-Date).DayOfYear + $AddDaysToBuildVersion)".PadLeft(3,'0')
    $ModuleVersion = "1.0.$((Get-Date).Year - 2000)$DayOfYear"
	$psModuleFile = [System.IO.Path]::Combine($rootFolder,"modules","DriverTool.PowerCLI","DriverTool.PowerCLI.psd1")    
    Write-Host "Updating Module File: $psModuleFile"
    $content = Get-Content -Path $psModuleFile
    $content | ForEach-Object {
        [System.Text.RegularExpressions.Regex]::Replace($_,"^\s*ModuleVersion\s*=\s*'\d+\.\d+\.\d+'\s*$","    ModuleVersion = '$ModuleVersion'")
    } | Set-Content -Path $psModuleFile
}