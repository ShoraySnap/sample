function Restore-And-Build-Project {
    param (
        [string]$projectName,
        [string]$projectPath,
        [string]$config
    )

    if ($projectName -eq "SnaptrudeManagerUI") {
        $targetFramework = "net48"
    } elseif ($config -in @("2019", "2020")) {
        $targetFramework = "net47"
    } elseif ($config -eq "2025") {
        $targetFramework = "net8.0-windows"
    } else {
        $targetFramework = "net48"
    }

    Write-Host "Building ${projectName} (${config})... " -NoNewline

    $restoreOutput = dotnet restore $projectPath -p:Configuration=$config -p:TargetFramework=$targetFramework
    if ($LASTEXITCODE -ne 0) {
        Write-Host " - Error:" -ForegroundColor Red
        Write-Host $restoreOutput
        exit 1
    }

    $buildOutput = msbuild $projectPath /t:Rebuild /p:Configuration=$config /p:Platform="Any CPU" /p:TargetFramework=$targetFramework /m /clp:NoSummary
    if ($LASTEXITCODE -ne 0) {
        Write-Host " - Error: failed for ${projectName} (${config}). Check for the error messages in this configuration." -ForegroundColor Red
        Write-Host $buildOutput
        exit 1
    }

    Write-Host "Done" -ForegroundColor Green
    return $true
}

function Run-InnoSetup {
    param (
	    [string]$name,
        [string]$script = "..\build-installer\snaptrude-manager.iss",
        [string]$version, 
	    [string]$urlPath,
        [string]$outputDir = ".\out\$version"
    )
    $includeDownloadSection = "true";
    $outputBaseFileName = "snaptrude-manager-setup-" + $version;
    if ($name -eq "Update") {
	$includeDownloadSection = "false";
        $outputBaseFileName += "-Update"
        $outputDir += "\Update"
    }
    elseif ($name -eq "Wework") {
        $outputDir += "\Wework"
        $outputBaseFileName += "-WeWork"
    }
    else{
        $outputDir += "\Prod"
    }
    
    $outputFilePath = Join-Path $outputDir ($outputBaseFileName + ".exe")

    Write-Host "Creating $name installer... " -NoNewline
    & "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" $script /DMyAppVersion=$version /DUrlPath=$urlPath /DIncludeDownloadSection=$includeDownloadSection /DOutputBaseFileName=$outputBaseFileName /DUIBuildPath=$uiRelativePath /DOutDir=$outputDir /DBuildName=$name -quiet
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Done" -ForegroundColor Green
        return $outputFilePath
    } else {
        Write-Host " - Error" -ForegroundColor Red
        exit 1
    }
}

$branch = "dev"
$date = Get-Date -format "yyyyMMdd"
$dllRelativePath = "..\revit-addin\SnaptrudeManagerAddin\bin\Debug"
$currentScriptPath = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent

if ($branch -eq "master") {
    $uiBuildConfig = "Release"
}
else {
    $uiBuildConfig = "Debug"
}
$uiRelativePath = "..\revit-addin\SnaptrudeManagerUI\bin\$uiBuildConfig\net48"


$uiProjects = @{
    "SnaptrudeManagerUI" = "..\revit-addin\SnaptrudeManagerUI\SnaptrudeManagerUI.csproj"
}

foreach ($projectName in $uiProjects.Keys) {
    $projectPath = Join-Path -Path $currentScriptPath -ChildPath ${uiProjects}[$projectName]
    if (-not (Restore-And-Build-Project -projectName $projectName -projectPath $projectPath -config $uiBuildConfig)) {
        return
    }
}
$version_number = (Get-Item "$uiRelativePath\SnaptrudeManagerUI.exe").VersionInfo.FileVersion

$addinProjects = @{
    "SnaptrudeManagerAddin" = "..\revit-addin\SnaptrudeManagerAddin\SnaptrudeManagerAddin.csproj"
}

$configurations = @("2019","2020","2021","2022","2023","2024","2025")

foreach ($config in $configurations) {
    $projectName = "SnaptrudeManagerAddin"
    $projectPath = Join-Path -Path $currentScriptPath -ChildPath ${addinProjects}[$projectName]
    if (-not (Restore-And-Build-Project -projectName $projectName -projectPath $projectPath -config $config)) {
        return
    }
}

$stagingUrlPath = "..\build-installer\misc\urlsstaging.json"

$version = $version_number

$stagingInstallerPath = Run-InnoSetup -name "Staging" `
                -version $version `
                -urlPath $stagingUrlPath `
                -includeDownloadSection "true"
                
$updateInstallerPath = Run-InnoSetup -name "Update" `
                -version $version `
                -urlPath $stagingUrlPath `
                -includeDownloadSection "false"

