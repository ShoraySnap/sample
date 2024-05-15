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

    $compileMessage = "Compiling ${projectName} (${config})..."

    # Determine the TargetFramework based on the project and configuration year
    if ($projectName -eq "SnaptrudeManagerUI") {
        $targetFramework = "net6.0-windows"
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
        return $false
    }

    # Display the initial compilation message
    Clear-Line
    Write-Host "$compileMessage" -NoNewline

    # Start build job
    $job = Start-Job -ScriptBlock {
        param($msBuildPath, $projectPath, $config, $targetFramework)
        & $msBuildPath $projectPath /t:Rebuild /p:Configuration=$config /p:Platform="Any CPU" /p:TargetFramework=$targetFramework /m /clp:NoSummary
    } -ArgumentList $msBuildPath, $projectPath, $config, $targetFramework

    # Wait for the job to complete
    while ($job.State -eq "Running") {
        Start-Sleep -Milliseconds 100
    }

    # Ensure the job is stopped and removed
    if ($job.State -ne "Completed") {
        Stop-Job -Job $job
    }
    $result = Receive-Job -Job $job
    Remove-Job -Job $job -Force

    # Check for errors
    $errors = ($result | Select-String "error MSB").Count
    if ($errors -gt 0) {
        Clear-Line
        Write-Host "Compiling ${projectName} (${config}) - Error:" -ForegroundColor Red
        $result | Where-Object { $_ -match "error MSB" } | ForEach-Object { Write-Host $_ -ForegroundColor Red }
        return $false
    }

    # Clear the line and output success message
    Clear-Line
    Write-Host "Compiling ${projectName} (${config}) - Done" -ForegroundColor Green
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
$branch = & git rev-parse --abbrev-ref HEAD
$date = Get-Date -format "yyyyMMdd"
$version = -join($branch, "_", $date)
$dllRelativePath = "..\revit-addin\SnaptrudeManagerAddin\bin\Debug"

function Sign-File {
    param (
        [string]$filePath,
        [string]$certPath,
        [string]$plainPwd
    )
    Write-Host "Signing $filePath ..." -NoNewline
    & signtool.exe sign /f $certPath /fd SHA256 /p $plainPwd /t http://timestamp.digicert.com "$filePath"
    if ($LASTEXITCODE -eq 0) {
        Clear-Line
        Write-Host "Signing $filePath - Done" -ForegroundColor Green
    } else {
        Clear-Line
        Write-Host "Signing $filePath - Error" -ForegroundColor Red
        exit 1
    }
}

function Run-InnoSetup {
    param (
	    [string]$name,
        [string]$script,
        [string]$version
    )
    Write-Host "Creating $name installer ..." -NoNewline
    & "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" $script /DMyAppVersion=$version -quiet
    if ($LASTEXITCODE -eq 0) {
        Clear-Line
        Write-Host "Creating $name installer - Done" -ForegroundColor Green
    } else {
        Clear-Line
        Write-Host "Creating $name installer - Error" -ForegroundColor Red
        exit 1
    }
}

if ($branch -eq "master") {
    $certPath = Read-Host "Enter pfx file path"
    $certPwd = Read-Host "Enter certificate password" -AsSecureString
    $plainPwd = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($certPwd))

    foreach ($config in $configurations) {
        Sign-File -filePath "$dllRelativePath\$config\SnaptrudeManagerAddin.dll" -certPath $certPath -plainPwd $plainPwd
    }

    Sign-File -filePath "installers\snaptrude-manager-1.0.0 Setup.exe" -certPath $certPath -plainPwd $plainPwd

    $version = Get-Content -Path .\version.txt -TotalCount 1

    Run-InnoSetup -name "Prod" -script "snaptrude-manager-prod.iss" -version $version
    Run-InnoSetup -name "Wework" -script "snaptrude-manager-wework.iss" -version $version
    Run-InnoSetup -name "Update" -script "snaptrude-manager-update.iss" -version $version

    Sign-File -filePath "out\snaptrude-manager-setup-$version.exe" -certPath $certPath -plainPwd $plainPwd
    Sign-File -filePath "out\snaptrude-manager-setup-$version-WeWork.exe" -certPath $certPath -plainPwd $plainPwd
    Sign-File -filePath "out\snaptrude-manager-setup-$version-Update.exe" -certPath $certPath -plainPwd $plainPwd

} elseif ($branch -eq "dev") {
    $version_number = Get-Content -Path .\version.txt -TotalCount 1
    $version = -join("dev-", $version_number)
    Run-InnoSetup -name "Staging" -script "snaptrude-manager-staging.iss" -version $version
    Run-InnoSetup -name "Update" -script "snaptrude-manager-update.iss" -version $version
} else {
    Run-InnoSetup -name "Staging" -script "snaptrude-manager-staging.iss" -version $version
    Run-InnoSetup -name "Update" -script "snaptrude-manager-update.iss" -version $version
}

git tag -a $version -m $version
