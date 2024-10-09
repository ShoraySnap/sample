function Clear-Line {
    Write-Host "`r$( ' ' * (Get-Host).UI.RawUI.WindowSize.Width )`r" -NoNewline
}
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

    $restoreOutput = & $dotnetPath restore $projectPath -p:Configuration=$config -p:TargetFramework=$targetFramework
    if ($LASTEXITCODE -ne 0) {
        Write-Host " - Error:" -ForegroundColor Red
        Write-Host $restoreOutput
        exit 1
    }

    $buildOutput = & $msBuildPath $projectPath /t:Rebuild /p:Configuration=$config /p:Platform="Any CPU" /p:TargetFramework=$targetFramework /m /clp:NoSummary
    if ($LASTEXITCODE -ne 0) {
        Write-Host " - Error: failed for ${projectName} (${config}). Check for the error messages in this configuration." -ForegroundColor Red
        exit 1
    }

    Write-Host "✅" -ForegroundColor Green
    return $true
}
function Sign-File {
    param (
        [string]$filePath,
        [string]$certPath,
        [string]$plainPwd
    )


    $fileName = Split-Path $filePath -Leaf

    Write-Host "Signing $fileName... " -NoNewline
    $output = & {
        signtool.exe sign /f $certPath /fd SHA256 /p $plainPwd /t http://timestamp.digicert.com "$filePath" 2>&1
    }
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅" -ForegroundColor Green
    } else {
        Write-Host " - Error" -ForegroundColor Red
        Write-Host $output
        exit 1
    }
}
function Run-InnoSetup {
    param (
	    [string]$name,
        [string]$script = "..\build-installer\snaptrude-manager.iss",
        [string]$version, 
	    [string]$urlPath,
        [string]$outputDir = ".\out\$version_number"
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
    & "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" $script /DMyAppVersion=$version /DUrlPath=$urlPath /DIncludeDownloadSection=$includeDownloadSection /DOutputBaseFileName=$outputBaseFileName /DUIBuildPath=$uiRelativePath /DOutDir=$outputDir -quiet
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅" -ForegroundColor Green
        return $outputFilePath
    } else {
        Write-Host " - Error" -ForegroundColor Red
        exit 1
    }
}
function UploadFileToS3 {
    param (
        [Parameter(Mandatory=$true)]
        [string]$BucketName,

        [Parameter(Mandatory=$true)]
        [string]$AWSRegion,

        [Parameter(Mandatory=$true)]
        [string]$FilePath,

        [Parameter(Mandatory=$true)]
        [string]$KeyName,

        [string]$CannedACL = "public-read"
    )
    
    try {
        if (-not (Test-Path $FilePath)) {
            throw "File not found: $FilePath"
        }
        Write-S3Object -BucketName $BucketName -File $FilePath -Key $KeyName -CannedACLName $CannedACL
        
        $s3Url = "https://$BucketName.s3.$AWSRegion.amazonaws.com/$KeyName"
        return $s3Url 
    }
    catch {

        Write-Host " - Error:" -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
        exit 1
    }
}
function CheckMandatoryVersion {
    param (
        [Parameter(Mandatory=$true)]
        [string]$NewVersion
    )
    do {
        Write-Host "This version ($NewVersion) is a mandatory one? (Y/N) " -NoNewline -ForegroundColor Yellow
        $response = Read-Host
    } until ($response -eq "Y" -or $response -eq "N")

    if ($response -eq "Y") {
        return 
    } else {
        $finalMinVersion = $currentMinVersion
    }
}

function GenerateAppcast {
    param (
        [Parameter(Mandatory=$true)]
        [string]$AppPath,

        [Parameter(Mandatory=$true)]
        [string]$OutputFolder,

        [Parameter(Mandatory=$true)]
        [string]$AppcastFolderUrl,

        [Parameter(Mandatory=$false)]
        [switch]$MandatoryUpdate = $false
    )
    $criticalVersion = "1.0.0"
    Write-Host "Generating appcast... " -NoNewline

    try {
        $output = netsparkle-generate-appcast -a .\publish -e exe -b $AppPath -o windows -x true --description-tag "Addin for Revit/Snaptrude interoperability" -u $AppcastFolderUrl -n "Snaptrude Manager" --critical-versions $criticalVersion --overwrite-old-items true --reparse-existing true --key-path .\publish --human-readable true *> $null 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅" -ForegroundColor Green
            return $DestinationPath
        } else {
            Write-Host " - Error: $LASTEXITCODE" -ForegroundColor Red
            Write-Host $_.Exception.Message -ForegroundColor Red
            exit 1
        }
    } catch {
        Write-Host "An error occurred while running netsparkle-generate-appcast" -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
        exit 1
    }
}
function DownloadAppcast {
    param (
        [Parameter(Mandatory=$true)]
        [string]$AppcastUrl,

        [Parameter(Mandatory=$true)]
        [string]$DestinationPath
    )

    try {
        Write-Host "Downloading existing appcast... " -NoNewline
        Invoke-WebRequest -Uri $AppcastUrl -OutFile $DestinationPath
        Write-Host "✅" -ForegroundColor Green
    } catch {
        Write-Host " - Error:" -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
        exit 1
    }
}
function CheckKeyFiles {
    param (
        [Parameter(Mandatory=$true)]
        [string]$FolderPath,

        [string[]]$KeyFiles = @("NetSparkle_Ed25519.priv", "NetSparkle_Ed25519.pub")
    )

    $missingFiles = @()

    foreach ($keyFile in $KeyFiles) {
        $filePath = Join-Path -Path $FolderPath -ChildPath $keyFile
        if (-not (Test-Path $filePath)) {
            Write-Host "Missing key file: $keyFile in $FolderPath" -ForegroundColor Red
            $missingFiles += $keyFile
        }
    }

    if ($missingFiles.Count -gt 0) {
        Write-Host "Some key files are missing. Please ensure all necessary key files are in place (build-installer\publish)." -ForegroundColor Red
        exit 1
    }
}



$branch = & git rev-parse --abbrev-ref HEAD
$date = Get-Date -format "yyyyMMdd"
$uiRelativePath = "..\revit-addin\SnaptrudeManagerUI\bin\Debug\net48"
$dllRelativePath = "..\revit-addin\SnaptrudeManagerAddin\bin\Debug"
$version_number = (Get-Item "$uiRelativePath\SnaptrudeManagerUI.exe").VersionInfo.FileVersion
$version = -join($branch, "_", $date, "_", $version_number)

if ($branch -eq "feature-update-netsparkle") {
    $ProgressPreference = 'SilentlyContinue'
    $publishFolder = ".\publish"
    CheckKeyFiles -FolderPath $publishFolder
    $AppcastUrl = "https://updatemanager.s3.us-east-2.amazonaws.com/AutomatedDeployTest/appcast.xml"
    $extraConfig = "Release"
    $version = $version_number
    $uiRelativePath = "..\revit-addin\SnaptrudeManagerUI\bin\Release\net48"
    Write-Host "[Certificate Signing] Enter pfx file path: " -NoNewline -ForegroundColor Yellow
    $certPath = Read-Host
    Write-Host "[Certificate Signing] Enter certificate password: " -NoNewline -ForegroundColor Yellow
    $certPwd = Read-Host -AsSecureString
    $plainPwd = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($certPwd))
    $isMandatory = CheckMandatoryVersion -NewVersion $version;
    DownloadAppcast -AppcastUrl $AppcastUrl -DestinationPath ".\publish\appcast.xml"
}

$msBuildPath = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
$dotnetPath = "C:\Program Files\dotnet\dotnet.exe"
$currentScriptPath = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent

$projects = @{
    "SnaptrudeManagerAddin" = "..\revit-addin\SnaptrudeManagerAddin\SnaptrudeManagerAddin.csproj"
}

$extraProject = @{
    "SnaptrudeManagerUI" = "..\revit-addin\SnaptrudeManagerUI\SnaptrudeManagerUI.csproj"
}

$configurations = @("2019", "2020", "2021", "2022", "2023", "2024", "2025")
$extraConfig = "Debug"

foreach ($projectName in $extraProject.Keys) {
    $projectPath = Join-Path -Path $currentScriptPath -ChildPath ${extraProject}[$projectName]
    if (-not (Restore-And-Build-Project -projectName $projectName -projectPath $projectPath -config $extraConfig)) {
        return
    }
}

foreach ($config in $configurations) {
    $projectName = "SnaptrudeManagerAddin"
    $projectPath = Join-Path -Path $currentScriptPath -ChildPath ${projects}[$projectName]
    if (-not (Restore-And-Build-Project -projectName $projectName -projectPath $projectPath -config $config)) {
        return
    }
}

$stagingUrlPath = "..\build-installer\misc\urlsstaging.json"
$prodUrlPath = "..\build-installer\misc\urls.json"
$weworkUrlPath = "..\build-installer\misc\urlswework.json"

if ($branch -eq "feature-update-netsparkle") {
    

    foreach ($config in $configurations) {
        Sign-File -filePath "$dllRelativePath\$config\SnaptrudeManagerAddin.dll" -certPath $certPath -plainPwd $plainPwd
    }

    Get-ChildItem -Path $uiRelativePath -File | Where-Object { $_.Extension -eq ".dll" -or $_.Extension -eq ".exe" } | ForEach-Object {
    	Sign-File -filePath $_.FullName -certPath $certPath -plainPwd $plainPwd
    }
    
    $prodInstallerPath = Run-InnoSetup -name "Prod" `
                                    -version $version `
                                    -urlPath $prodUrlPath `
                                    -includeDownloadSection "true"

    $weworkInstallerPath = Run-InnoSetup -name "Wework" `
                                    -version $version `
                                    -urlPath $weworkUrlPath `
                                    -includeDownloadSection "true"

    $updateInstallerPath = Run-InnoSetup -name "Update" `
                                    -version $version `
                                    -urlPath $prodUrlPath `
                                    -includeDownloadSection "false"

    $prodInstallerFileName = Split-Path $prodInstallerPath -Leaf
    $weworkInstallerFileName = Split-Path $weworkInstallerPath -Leaf
    $updateInstallerFileName = Split-Path $updateInstallerPath -Leaf

    $updateInstallerFolderPath = Split-Path -Path $updateInstallerPath
    $appcastOutputPath = ".\publish\appcast.xml"
    GenerateAppcast -AppPath $updateInstallerFolderPath -OutputFolder $appcastOutputPath -AppcastFolderUrl $AppcastFolderUrl

    Sign-File -filePath "..\build-installer\out\snaptrude-manager-setup-$version.exe" -certPath $certPath -plainPwd $plainPwd
    Sign-File -filePath "..\build-installer\out\snaptrude-manager-setup-$version-WeWork.exe" -certPath $certPath -plainPwd $plainPwd
    Sign-File -filePath "..\build-installer\out\snaptrude-manager-setup-$version-Update.exe" -certPath $certPath -plainPwd $plainPwd
    
    $bucketName = "updatemanager"
    $s3ProdSetupKeyName = "AutomatedDeployTest/$version/$prodInstallerFileName"
    $s3UpdateSetupKeyName = "AutomatedDeployTest/$version/$updateInstallerFileName"

    Write-Host "Uploading Prod installer to S3 bucket... " -NoNewline
    $s3ProdUrl = UploadFileToS3 -BucketName $bucketName -AWSRegion $AWSRegion -FilePath $prodInstallerPath -KeyName $s3ProdSetupKeyName
    Write-Host "✅" -ForegroundColor Green

    Write-Host "Uploading Update installer to S3 bucket... " -NoNewline
    $s3UpdateUrl = UploadFileToS3 -BucketName $bucketName -AWSRegion $AWSRegion -FilePath $updateInstallerPath -KeyName $s3UpdateSetupKeyName
    Write-Host "✅" -ForegroundColor Green



    $s3AppCastKeyName = "AutomatedDeployTest/appcast.xml"
    Write-Host "Uploading AppCast file to S3 bucket... " -NoNewline
    $s3AppCastUrl = UploadFileToS3 -BucketName $bucketName -AWSRegion $AWSRegion  -FilePath $appcastOutputPath -KeyName $s3AppCastKeyName
    Write-Host "✅" -ForegroundColor Green  

    #git tag -a $version -m $version
    Write-Host "Snaptrude Manager sucessfully published!" -ForegroundColor Green  
    $ProgressPreference = 'Continue'


} elseif ($branch -eq "dev") {
    $version = -join("dev-", $version_number)
    Run-InnoSetup -name "Staging" -script "..\build-installer\snaptrude-manager.iss" -version $version -urlPath $stagingUrlPath -includeDownloadSection "true"
    Run-InnoSetup -name "Update" -script "..\build-installer\snaptrude-manager.iss" -version $version -urlPath $stagingUrlPath -includeDownloadSection "false"
    
    #git tag -a $version -m $version

} else {
    Run-InnoSetup -name "Staging" -script "..\build-installer\snaptrude-manager.iss" -version $version -urlPath $stagingUrlPath -includeDownloadSection "true"
    Run-InnoSetup -name "Update" -script "..\build-installer\snaptrude-manager.iss" -version $version -urlPath $stagingUrlPath -includeDownloadSection "false"
}
