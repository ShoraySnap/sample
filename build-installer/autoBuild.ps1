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

function RunInnoSetup {
    param (
	    [string]$name,
        [string]$script = "C:\workspace\build-installer\snaptrude-manager.iss",
        [string]$version, 
	    [string]$urlPath,
        [string]$outputDir = "C:\workspace\build-installer\out\$version"
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
    & "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" $script /DMyAppVersion=$version /DUrlPath=$urlPath /DIncludeDownloadSection=$includeDownloadSection /DOutputBaseFileName=$outputBaseFileName /DUIBuildPath=$uiBuildPath /DOutDir=$outputDir /DBuildName=$name -quiet
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Done" -ForegroundColor Green
        return $outputFilePath
    } else {
        Write-Host " - Error" -ForegroundColor Red
        exit 1
    }
}

function GenerateAppcast {
    param (
        [Parameter(Mandatory=$true)]
        [string]$AppPath,

        [Parameter(Mandatory=$true)]
        [string]$OutputFolder,

        [Parameter(Mandatory=$true)]
        [string]$AppcastFolderUrl
    )

    Write-Host "Generating appcast... " -NoNewline

    netsparkle-generate-appcast `
        -a $OutputFolder `
        -b $AppPath `
        --description-tag "Addin for Revit/Snaptrude interoperability" `
        --human-readable true `
        --key-path $OutputFolder `
        -n "Snaptrude Manager" `
        -u $AppcastFolderUrl `
        *> $null 2>&1

    Write-Host "Done" -ForegroundColor Green
}

function SignFile {
    param (
        [string]$filePath,
        [string]$certPath,
        [string]$certPwd
    )

    $fileName = Split-Path $filePath -Leaf

    Write-Host "Signing $fileName... " -NoNewline
    $output = & {
        signtool.exe sign /f "$certPath" /fd SHA256 /p "$certPwd" /t http://timestamp.digicert.com "$filePath" 2>&1
    }
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Done" -ForegroundColor Green
    } else {
        Write-Host " - Error" -ForegroundColor Red
        Write-Host $output
        exit 1
    }
}

function DecodeAndSaveCert {
    param (
        [string]$base64Cert,
        [string]$outputPath
    )

    # Decode the base64 string and save as a PFX file
    $bytes = [Convert]::FromBase64String($base64Cert)
    [System.IO.File]::WriteAllBytes($outputPath, $bytes)
}

function SaveNetSparkleKeys {
    param (
        [string]$outputPath,
        [string]$privKey,
        [string]$pubKey
    )
    [System.IO.File]::WriteAllText("$outputPath\NetSparkle_Ed25519.priv", $privKey)
    [System.IO.File]::WriteAllText("$outputPath\NetSparkle_Ed25519.pub", $pubKey)
}

$branch = "dev"
$date = Get-Date -format "yyyyMMdd"
$dllPath = "C:\workspace\revit-addin\SnaptrudeManagerAddin\bin\Debug"
$currentScriptPath = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
$base64Cert = $env:CERT_BASE64
$certPwd = $env:CERT_PASSWORD
$certPath = "C:\snaptrude_inc.pfx"
Write-Host "Password ${certPwd}"
$publishFolder = "C:\"
DecodeAndSaveCert -base64Cert $base64Cert -outputPath $certPath         
$bucketName = $env:AWS_S3_BUCKET_NAME
$awsRegion = $env:AWS_REGION
aws configure set region $awsRegion
SaveNetSparkleKeys -outputPath $publishFolder -privKey $env:NETSPARJKE_PRIV_KEY -pubKey $env:NETSPARJKE_PUB_KEY

if ($branch -eq "master") {
    $uiBuildConfig = "Release"
}
else {
    $uiBuildConfig = "Debug"
}
$uiBuildPath = "C:\workspace\revit-addin\SnaptrudeManagerUI\bin\$uiBuildConfig\net48"


$uiProjects = @{
    "SnaptrudeManagerUI" = "C:\workspace\revit-addin\SnaptrudeManagerUI\SnaptrudeManagerUI.csproj"
}

foreach ($projectName in $uiProjects.Keys) {
    $projectPath = ${uiProjects}[$projectName]
    if (-not (Restore-And-Build-Project -projectName $projectName -projectPath $projectPath -config $uiBuildConfig)) {
        return
    }
}
$version_number = (Get-Item "$uiBuildPath\SnaptrudeManagerUI.exe").VersionInfo.FileVersion

$addinProjects = @{
    "SnaptrudeManagerAddin" = "C:\workspace\revit-addin\SnaptrudeManagerAddin\SnaptrudeManagerAddin.csproj"
}

$configurations = @("2019","2020","2021","2022","2023","2024","2025")

foreach ($config in $configurations) {
    $projectName = "SnaptrudeManagerAddin"
    $projectPath = ${addinProjects}[$projectName]
    if (-not (Restore-And-Build-Project -projectName $projectName -projectPath $projectPath -config $config)) {
        return
    }
}

foreach ($projectName in $uiProjects.Keys) {
    Get-ChildItem -Path $uiBuildPath -File | Where-Object { $_.Extension -eq ".dll" -or $_.Extension -eq ".exe" } | ForEach-Object {
    	SignFile -filePath $_.FullName -certPath $certPath -certPwd $certPwd
    }
}
foreach ($config in $configurations) {
    SignFile -filePath "$dllPath\$config\SnaptrudeManagerAddin.dll" -certPath $certPath -certPwd $certPwd
}

$stagingUrlPath = "C:\workspace\build-installer\misc\urlsstaging.json"

$version = $version_number

$stagingInstallerPath = RunInnoSetup -name "Staging" `
                -version $version `
                -urlPath $stagingUrlPath `
                -includeDownloadSection "true"
                
$updateInstallerPath = RunInnoSetup -name "Update" `
                -version $version `
                -urlPath $stagingUrlPath `
                -includeDownloadSection "false"


SignFile -filePath $stagingInstallerPath -certPath $certPath -certPwd $certPwd
SignFile -filePath $updateInstallerPath -certPath $certPath -certPwd $certPwd

$updateInstallerFolderPath = Split-Path -Path $updateInstallerPath
$AppcastFolderUrl = "https://$bucketName.s3.$awsRegion.amazonaws.com/$version_number"
GenerateAppcast -AppPath $updateInstallerFolderPath -OutputFolder $publishFolder -AppcastFolderUrl $AppcastFolderUrl

aws s3 cp $stagingInstallerPath s3://$bucketName/$version_number/
aws s3 cp $updateInstallerPath s3://$bucketName/$version_number/

$appcastOutputPath = "{$publishFolder}\appcast.xml"
aws s3 cp $appcastOutputPath s3://$bucketName/
$appcastSignatureOutputPath = "{$publishFolder}\appcast.xml.signature"
aws s3 cp $appcastSignatureOutputPath s3://$bucketName/