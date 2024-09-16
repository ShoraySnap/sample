# Define the paths to the MSBuild and dotnet executables
$msBuildPath = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
$dotnetPath = "C:\Program Files\dotnet\dotnet.exe"

# Get the current script's directory to base all other paths on it
$currentScriptPath = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent

# Names of the projects and their directories (relative paths to the project files)
$projects = @{
    "SnaptrudeManagerAddin" = "..\revit-addin\SnaptrudeManagerAddin\SnaptrudeManagerAddin.csproj"
}

# Additional project with its own configuration
$extraProject = @{
    "SnaptrudeManagerUI" = "..\revit-addin\SnaptrudeManagerUI\SnaptrudeManagerUI.csproj"
}

# Build configurations from 2019 to 2024
$configurations = @("2019", "2020", "2021", "2022", "2023", "2024", "2025")
# Configuration for the extra project
$extraConfig = "Debug"

$branch = & git rev-parse --abbrev-ref HEAD
if ($branch -eq "master") {
    $extraConfig = "Release"
}

# Function to clear the line
function Clear-Line {
    Write-Host "`r$( ' ' * (Get-Host).UI.RawUI.WindowSize.Width )`r" -NoNewline
}

# Function to restore and build a project
function Restore-And-Build-Project {
    param (
        [string]$projectName,
        [string]$projectPath,
        [string]$config
    )

    $compileMessage = "Building ${projectName} (${config})..."

    # Determine the TargetFramework based on the project and configuration year
    if ($projectName -eq "SnaptrudeManagerUI") {
        $targetFramework = "net48"
    } elseif ($config -in @("2019", "2020")) {
        $targetFramework = "net47"
    } elseif ($config -eq "2025") {
        $targetFramework = "net8.0-windows"
    } else {
        $targetFramework = "net48"
    }

    # Display the initial restore message
    Clear-Line
    Write-Host "Restoring ${projectName} (${config})..." -NoNewline

    # Run dotnet restore and capture the output
    $restoreOutput = & $dotnetPath restore $projectPath -p:Configuration=$config -p:TargetFramework=$targetFramework
    if ($LASTEXITCODE -ne 0) {
        Clear-Line
        Write-Host "Restoring ${projectName} (${config}) - Error:" -ForegroundColor Red
        Write-Host $restoreOutput
        exit 1
    }

    # Display the initial compilation message
    Clear-Line
    Write-Host "$compileMessage" -NoNewline

    # Run MSBuild and capture the output
    $buildOutput = & $msBuildPath $projectPath /t:Rebuild /p:Configuration=$config /p:Platform="Any CPU" /p:TargetFramework=$targetFramework /m /clp:NoSummary
    if ($LASTEXITCODE -ne 0) {
        Clear-Line
        Write-Host "Build failed for ${projectName} (${config}). Check for the error messages in this configuration." -ForegroundColor Red
        exit 1
    }

    # Clear the line and output success message
    Clear-Line
    Write-Host "Building ${projectName} (${config}) - Done" -ForegroundColor Green
    return $true
}

# Build the extra project with its specific configuration first
foreach ($projectName in $extraProject.Keys) {
    $projectPath = Join-Path -Path $currentScriptPath -ChildPath ${extraProject}[$projectName]
    if (-not (Restore-And-Build-Project -projectName $projectName -projectPath $projectPath -config $extraConfig)) {
        return # Stop processing if an error is found
    }
}

# Main compilation logic for SnaptrudeManagerAddin projects
foreach ($config in $configurations) {
    $projectName = "SnaptrudeManagerAddin"
    $projectPath = Join-Path -Path $currentScriptPath -ChildPath ${projects}[$projectName]
    if (-not (Restore-And-Build-Project -projectName $projectName -projectPath $projectPath -config $config)) {
        return # Stop processing if an error is found
    }
}

Write-Host "Compilation process completed for all projects." -ForegroundColor Green

# Additional post-build tasks
$date = Get-Date -format "yyyyMMdd"
$uiRelativePath = "..\revit-addin\SnaptrudeManagerUI\bin\Debug\net48"
if ($branch -eq "master") {
    $uiRelativePath = "..\revit-addin\SnaptrudeManagerUI\bin\Release\net48"
}
$dllRelativePath = "..\revit-addin\SnaptrudeManagerAddin\bin\Debug"
$version_number = (Get-Item "$uiRelativePath\SnaptrudeManagerUI.exe").VersionInfo.FileVersion
$version = -join($branch, "_", $date, "_", $version_number)

function Sign-File {
    param (
        [string]$filePath,
        [string]$certPath,
        [string]$plainPwd
    )

    function Clear-Line {
        Write-Host "`r" -NoNewline
    }

    Write-Host "Signing $filePath ..." -NoNewline
    $output = & {
        signtool.exe sign /f $certPath /fd SHA256 /p $plainPwd /t http://timestamp.digicert.com "$filePath" 2>&1
    }
    
    Clear-Line
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Signing $filePath - Done" -ForegroundColor Green
    } else {
        Write-Host "Signing $filePath - Error" -ForegroundColor Red
        Write-Host $output
        exit 1
    }
}

function Run-InnoSetup {
    param (
	    [string]$name,
        [string]$script,
        [string]$version, 
	    [string]$urlPath
    )
    $includeDownloadSection = "true";
    $outputBaseFileName = "snaptrude-manager-setup-" + $version;
    if ($name -eq "Update") {
	$includeDownloadSection = "false";
        $outputBaseFileName += "-Update"
    }
    if ($name -eq "Wework") {
        $outputBaseFileName += "-WeWork"
    }
    Write-Host "Creating $name installer ..." -NoNewline
    & "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" $script /DMyAppVersion=$version /DUrlPath=$urlPath /DIncludeDownloadSection=$includeDownloadSection /DOutputBaseFileName=$outputBaseFileName /DUIBuildPath=$uiRelativePath -quiet
    if ($LASTEXITCODE -eq 0) {
        Clear-Line
        Write-Host "Creating $name installer - Done" -ForegroundColor Green
    } else {
        Clear-Line
        Write-Host "Creating $name installer - Error" -ForegroundColor Red
        exit 1
    }
}

$stagingUrlPath = "..\build-installer\misc\urlsstaging.json"
$prodUrlPath = "..\build-installer\misc\urls.json"
$weworkUrlPath = "..\build-installer\misc\urlswework.json"

if ($branch -eq "master") {
    $certPath = Read-Host "Enter pfx file path"
    $certPwd = Read-Host "Enter certificate password" -AsSecureString
    $plainPwd = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($certPwd))

    foreach ($config in $configurations) {
        Sign-File -filePath "$dllRelativePath\$config\SnaptrudeManagerAddin.dll" -certPath $certPath -plainPwd $plainPwd
    }

    Get-ChildItem -Path $uiRelativePath -File | Where-Object { $_.Extension -eq ".dll" -or $_.Extension -eq ".exe" } | ForEach-Object {
    	Sign-File -filePath $_.FullName -certPath $certPath -plainPwd $plainPwd
    }

    $version = $version_number
    
    Run-InnoSetup -name "Prod" -script "..\build-installer\snaptrude-manager.iss" -version $version -urlPath $prodUrlPath -includeDownloadSection "true"
    Run-InnoSetup -name "Wework" -script "..\build-installer\snaptrude-manager.iss" -version $version -urlPath $weworkUrlPath -includeDownloadSection "true"
    Run-InnoSetup -name "Update" -script "..\build-installer\snaptrude-manager.iss" -version $version -urlPath $prodUrlPath -includeDownloadSection "false"

    Sign-File -filePath "..\build-installer\out\snaptrude-manager-setup-$version.exe" -certPath $certPath -plainPwd $plainPwd
    Sign-File -filePath "..\build-installer\out\snaptrude-manager-setup-$version-WeWork.exe" -certPath $certPath -plainPwd $plainPwd
    Sign-File -filePath "..\build-installer\out\snaptrude-manager-setup-$version-Update.exe" -certPath $certPath -plainPwd $plainPwd

} elseif ($branch -eq "dev") {
    $version = -join("dev-", $version_number)
    Run-InnoSetup -name "Staging" -script "..\build-installer\snaptrude-manager.iss" -version $version -urlPath $stagingUrlPath -includeDownloadSection "true"
    Run-InnoSetup -name "Update" -script "..\build-installer\snaptrude-manager.iss" -version $version -urlPath $stagingUrlPath -includeDownloadSection "false"
} else {
    Run-InnoSetup -name "Staging" -script "..\build-installer\snaptrude-manager.iss" -version $version -urlPath $stagingUrlPath -includeDownloadSection "true"
    Run-InnoSetup -name "Update" -script "..\build-installer\snaptrude-manager.iss" -version $version -urlPath $stagingUrlPath -includeDownloadSection "false"
}

git tag -a $version -m $version

$folderPath = Join-Path -Path $currentScriptPath -ChildPath "..\build-installer\out"
Start-Process explorer.exe -ArgumentList $folderPath
