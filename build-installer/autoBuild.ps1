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
    $outputBaseFileName = "snaptrude-manager-setup";
    if ($name -eq "Update") {
	$includeDownloadSection = "false";
        $outputBaseFileName += "-Update"
        $outputDir += "\Update"
    }
    elseif ($name -eq "Wework") {
        $outputDir += "\Wework"
        $outputBaseFileName += "-WeWork"
    }
    elseif ($name -eq "Staging") {
        $outputDir += "\Staging"
        $outputBaseFileName += "-Staging"
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

function GetForgeAccessTokens {
    param (
        [string]$clientId,
        [string]$clientSecret
    )
    # Write-Host "Client ID: $clientId"
    # Write-Host "Client Secret: $clientSecret"

    $bytes = [System.Text.Encoding]::UTF8.GetBytes($clientId + ":" + $clientSecret)
    $base64EncodedString = [Convert]::ToBase64String($bytes)
    # Write-Host "Base64 encoded string: $base64EncodedString"
    $response = Invoke-RestMethod -Uri 'https://developer.api.autodesk.com/authentication/v2/token' -Method Post -Headers @{Authorization=("Basic $base64EncodedString")} -Body @{grant_type="client_credentials"; scope="code:all bucket:create bucket:read data:create data:write data:read"}
    $accessToken = $response.access_token
    # Write-Host "Access token: $accessToken"
    return $accessToken
}

function UpdateForgeBundle {
    param (
        [string]$bundleId,
        [string]$bundleName,
        [string]$bundlePath,
        [string]$accessToken,
        [string]$config
    )
    $bundle = Invoke-RestMethod -Uri "https://developer.api.autodesk.com/da/us-east/v3/appbundles/$bundleId" -Headers @{Authorization=("Bearer $accessToken")}
    $bundleIdStripped = $bundleName + "AppBundle"
    $responseBundleId = $bundle.id
    if ($responseBundleId -eq $bundleId) {
        $bundleVersion = $bundle.version
        Write-Host "Bundle $bundleId already exists, v$bundleVersion. Updating..."

        $body = @{
            "engine" = "Autodesk.Revit+$config"
            "description" = "Updated Direct Import Addin AppBundle for Revit via autodeploy"
        } | ConvertTo-Json

        $headers = @{
            Authorization = "Bearer $accessToken"
            'Content-Type' = 'application/json'
        }

        $newbundleData = Invoke-RestMethod `
            -Uri "https://developer.api.autodesk.com/da/us-east/v3/appbundles/${bundleIdStripped}/versions" `
            -Headers $headers `
            -Method Post `
            -Body $body
        
        Write-Host "New bundle data: $newbundleData"

        $newbundleVersion = $newbundleData.version
        Write-Host "New bundle version: $newbundleVersion"

        $aliasSpec = @{
            version = $newbundleVersion
        } | ConvertTo-Json

        $aliasUpdate = Invoke-RestMethod `
            -Uri "https://developer.api.autodesk.com/da/us-east/v3/appbundles/${bundleIdStripped}/aliases/dev" `
            -Headers $headers `
            -Method Patch `
            -Body $aliasSpec
        
        # Write-Host "Alias update: $aliasUpdate"

        $endpointURL = $newbundleData.uploadParameters.endpointURL
        # Write-Host "Endpoint URL: $endpointURL"

        $formData = $newbundleData.uploadParameters.formData
        # Write-Host "Form data: $formData" 

        $uploadParams = @{}
        foreach ($property in $formData.PSObject.Properties) {
            $uploadParams[$property.Name] = $property.Value
        }

        try {
            $uploadResponse = Upload-FileWithFormData `
                -FilePath $bundlePath `
                -Endpoint $endpointURL `
                -Parameters $uploadParams

            if ($uploadResponse.StatusCode -eq 200) {
                Write-Host "Bundle upload successful!"
            } else {
                Write-Host "Bundle upload completed with status code: $($uploadResponse.StatusCode)"
            }
        } catch {
            Write-Host "Failed to upload bundle: $($_.Exception.Message)"
            throw
        }
    }
    else {
        Write-Host "Bundle $bundleId does not exist, creating..."
        CreateForgeBundle -bundleName $bundleName -bundlePath $bundlePath -accessToken $accessToken -config $config
    }

    CreateActivity -bundleId $bundleId -bundleName $bundleName -accessToken $accessToken -config $config
}

function CreateForgeBundle{
    param (
        [string]$bundleName,
        [string]$bundlePath,
        [string]$accessToken,
        [string]$config
    )
    Write-Host "Bundle $bundleId does not exist, creating..."

        $bundleIdStripped = $bundleName + "AppBundle"
        $body = @{
            id = $bundleIdStripped
            engine = "Autodesk.Revit+$config"
            description = "New Direct Import Addin AppBundle for Revit via autodeploy"
        } | ConvertTo-Json

        $headers = @{
            Authorization = "Bearer $accessToken"
            'Content-Type' = 'application/json'
        }

        # Invoke-RestMethod `
        #     -Uri "https://developer.api.autodesk.com/da/us-east/v3/appbundles/$bundleIdStripped" `
        #     -Headers $headers `
        #     -Method Delete

        $newBundle = Invoke-RestMethod `
            -Uri "https://developer.api.autodesk.com/da/us-east/v3/appbundles" `
            -Headers $headers `
            -Method Post `
            -Body $body

        Write-Host "New bundle: $newBundle"

        $newBundleId = $newBundle.id
        Write-Host "New bundle ID: $newBundleId"

        $newbundleVersion = $newBundle.version
        Write-Host "New bundle version: $newbundleVersion"

        $aliasSpec = @{
            version = $newbundleVersion
            id = "dev"
        } | ConvertTo-Json

        $aliasUpdate = Invoke-RestMethod `
            -Uri "https://developer.api.autodesk.com/da/us-east/v3/appbundles/${bundleIdStripped}/aliases" `
            -Headers $headers `
            -Method Post `
            -Body $aliasSpec

        $endpointURL = $newBundle.uploadParameters.endpointURL
        # Write-Host "Endpoint URL: $endpointURL"

        $formData = $newBundle.uploadParameters.formData
        # Write-Host "Form data: $formData" 

        $uploadParams = @{}
        foreach ($property in $formData.PSObject.Properties) {
            $uploadParams[$property.Name] = $property.Value
        }

        try {
            $uploadResponse = Upload-FileWithFormData `
                -FilePath $bundlePath `
                -Endpoint $endpointURL `
                -Parameters $uploadParams

            if ($uploadResponse.StatusCode -eq 200) {
                Write-Host "Bundle upload successful!"
            } else {
                Write-Host "Bundle upload completed with status code: $($uploadResponse.StatusCode)"
            }
        } catch {
            Write-Host "Failed to upload bundle: $($_.Exception.Message)"
            throw
        }
}

function Upload-FileWithFormData {
    param (
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        
        [Parameter(Mandatory = $true)]
        [string]$Endpoint,
        
        [Parameter(Mandatory = $false)]
        [hashtable]$Parameters
    )
    
    try {
        # Generate a unique boundary for multipart/form-data
        $boundary = [System.Guid]::NewGuid().ToString()
        $LF = "`r`n"
        
        # Create the multipart/form-data content
        $bodyLines = New-Object System.Collections.ArrayList
        
        # Add additional parameters if provided
        if ($Parameters) {
            foreach ($param in $Parameters.GetEnumerator()) {
                [void]$bodyLines.Add("--$boundary")
                [void]$bodyLines.Add("Content-Disposition: form-data; name=`"$($param.Key)`"$LF")
                [void]$bodyLines.Add($param.Value)
            }
        }
        
        # Add the file content
        $fileName = Split-Path $FilePath -Leaf
        $fileBytes = [System.IO.File]::ReadAllBytes($FilePath)
        $fileContent = [System.Convert]::ToBase64String($fileBytes)
        
        [void]$bodyLines.Add("--$boundary")
        [void]$bodyLines.Add("Content-Disposition: form-data; name=`"file`"; filename=`"$fileName`"")
        [void]$bodyLines.Add("Content-Type: application/octet-stream$LF")
        [void]$bodyLines.Add($fileContent)
        [void]$bodyLines.Add("--$boundary--")
        
        # Join all lines to create the body
        $body = $bodyLines -join $LF
        
        # Create the web request
        $uri = New-Object System.Uri($Endpoint)
        $request = [System.Net.HttpWebRequest]::Create($uri)
        $request.Method = "POST"
        $request.ContentType = "multipart/form-data; boundary=$boundary"
        $request.Headers.Add("Cache-Control", "no-cache")
        
        # Write the body to the request stream
        $requestStream = $request.GetRequestStream()
        $writer = New-Object System.IO.StreamWriter($requestStream)
        $writer.Write($body)
        $writer.Close()
        $requestStream.Close()
        
        # Get the response
        try {
            $response = $request.GetResponse()
            $responseStream = $response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($responseStream)
            $responseContent = $reader.ReadToEnd()
            
            # Create response object
            $result = @{
                StatusCode = [int]$response.StatusCode
                StatusDescription = $response.StatusDescription
                Content = $responseContent
            }
            
            return $result
        }
        catch [System.Net.WebException] {
            $errorResponse = $_.Exception.Response
            $errorStream = $errorResponse.GetResponseStream()
            $errorReader = New-Object System.IO.StreamReader($errorStream)
            $errorContent = $errorReader.ReadToEnd()
            
            Write-Error "Request failed with status code $($errorResponse.StatusCode): $errorContent"
            throw
        }
        finally {
            if ($response) { $response.Close() }
            if ($responseStream) { $responseStream.Close() }
            if ($reader) { $reader.Close() }
        }
    }
    catch {
        Write-Error "An error occurred: $($_.Exception.Message)"
        throw
    }
}

function CreateActivity { 
    param (
        [string]$bundleId,
        [string]$bundleName,
        [string]$accessToken,
        [string]$config
    )
    $activityName = $bundleName + "Activity"
    $activityId = $bundleId.Replace("AppBundle", "Activity")
    $engine = "Autodesk.Revit+" + $config

    Write-Host "Activity name: $activityName"
    Write-Host "Activity ID: $activityId"
    Write-Host "Engine: $engine"

    $headers = @{
        Authorization = "Bearer $accessToken"
        'Content-Type' = 'application/json'
    }
    
    # Invoke-RestMethod `
    #     -Uri "https://developer.api.autodesk.com/da/us-east/v3/activities/$activityName" `
    #     -Headers $headers `
    #     -Method Delete
    try {
        $activity = Invoke-RestMethod -Uri "https://developer.api.autodesk.com/da/us-east/v3/activities/$activityId" -Headers @{Authorization=("Bearer $accessToken")}
        Write-Host "Activity $activityId already exists"
    }
    catch {
       if ($_.Exception.Response.StatusCode.value__ -eq 404) {
            Write-Host "Activity $activityId does not exist, creating..."
            $commandline = 
            '$(engine.path)\\revitcoreconsole.exe /i "$(args[inputFile].path)" /al "$(appbundles[{0}].path)"'
            $commandline = $commandline.Replace("{0}", $bundleName + "AppBundle")
            Write-Host "Command line: $commandline"
            $activitySpec = @{
                id = $activityName
                appbundles = @($bundleId)
                commandLine = @($commandline)
                engine = $engine
                parameters = @{
                    inputFile = @{
                        description = "input file"
                        localName = '$(inputFile)'
                        ondemand = $false
                        required = $true
                        verb = "get"
                        zip = $false
                    }
                result = @{
                    description = "result file"
                    localName = "result.trude"
                    ondemand = $false
                    required = $true
                    verb = "put"
                    zip = $false
                }
                logFile = @{
                    description = "log file"
                    localName = "log.json"
                    ondemand = $false
                    required = $true
                    verb = "put"
                    zip = $false
                }
            }
            settings = @{
                script = @{
                    value = ""
                }
            }} | ConvertTo-Json

            $newActivity = Invoke-RestMethod -Uri "https://developer.api.autodesk.com/da/us-east/v3/activities" -Headers $headers -Method Post -Body $activitySpec
            Write-Host "New activity: $newActivity"
            
            if ($newActivity.version -ne $null) {
                Write-Host "Activity $activityId created successfully"
                #crete alias
                $aliasSpec = @{
                    version = $newActivity.version
                    id = "dev"
                } | ConvertTo-Json
            
            $aliasUpdate = Invoke-RestMethod -Uri "https://developer.api.autodesk.com/da/us-east/v3/activities/${activityName}/aliases" -Headers $headers -Method Post -Body $aliasSpec

                Write-Host "Alias $aliasUpdate created successfully"
            } else {
                Write-Host "Activity $activityId creation failed with status code: $($newActivity.StatusCode)"
            }
        }
        else {
            Write-Error "Failed to retrieve activity: $($_.Exception.Message)"
            throw
        }
    }
}

$direct_import_enabled = $env:IS_DIRECT_IMPORT_ENABLED
$branch = $env:GITHUB_BRANCH
$date = Get-Date -format "yyyyMMdd"
$dllPath = "C:\workspace\revit-addin\SnaptrudeManagerAddin\bin\Debug"
$currentScriptPath = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
$base64Cert = $env:CERT_BASE64
$certPwd = $env:CERT_PASSWORD
$certPath = "C:\snaptrude_inc.pfx"
$publishFolder = "C:\"
DecodeAndSaveCert -base64Cert $base64Cert -outputPath $certPath         
$bucketName = $env:AWS_S3_BUCKET_NAME
$awsRegion = $env:AWS_REGION
aws configure set region $awsRegion
SaveNetSparkleKeys -outputPath $publishFolder -privKey $env:NETSPARJKE_PRIV_KEY -pubKey $env:NETSPARJKE_PUB_KEY

# if ($branch -eq "master") {
#     $uiBuildConfig = "Release"
# }
# else {
#     $uiBuildConfig = "Debug"
# }
# $uiBuildPath = "C:\workspace\revit-addin\SnaptrudeManagerUI\bin\$uiBuildConfig\net48"


# $uiProjects = @{
#     "SnaptrudeManagerUI" = "C:\workspace\revit-addin\SnaptrudeManagerUI\SnaptrudeManagerUI.csproj"
# }

# foreach ($projectName in $uiProjects.Keys) {
#     $projectPath = ${uiProjects}[$projectName]
#     if (-not (Restore-And-Build-Project -projectName $projectName -projectPath $projectPath -config $uiBuildConfig)) {
#         return
#     }
# }
# $version_number = (Get-Item "$uiBuildPath\SnaptrudeManagerUI.exe").VersionInfo.FileVersion

# $addinProjects = @{
#     "SnaptrudeManagerAddin" = "C:\workspace\revit-addin\SnaptrudeManagerAddin\SnaptrudeManagerAddin.csproj"
#     "DirectImport" = "C:\workspace\revit-addin\DirectImport\DirectImport.csproj"
# }

# $configurations = @("2019","2020","2021","2022","2023","2024","2025")

# foreach ($config in $configurations) {
#     $projectName = "SnaptrudeManagerAddin"
#     $projectPath = ${addinProjects}[$projectName]
#     if (-not (Restore-And-Build-Project -projectName $projectName -projectPath $projectPath -config $config)) {
#         return
#     }
# }

# foreach ($projectName in $uiProjects.Keys) {
#     Get-ChildItem -Path $uiBuildPath -File | Where-Object { $_.Extension -eq ".dll" -or $_.Extension -eq ".exe" } | ForEach-Object {
#     	SignFile -filePath $_.FullName -certPath $certPath -certPwd $certPwd
#     }
# }
# foreach ($config in $configurations) {
#     SignFile -filePath "$dllPath\$config\SnaptrudeManagerAddin.dll" -certPath $certPath -certPwd $certPwd
# }

# $stagingUrlPath = "C:\workspace\build-installer\misc\urlsstaging.json"
# $prodUrlPath = "C:\workspace\build-installer\misc\urls.json"

# $version = $version_number

# if ($branch -eq "master") 
# {
#     $branchFolder = "Prod"
#     $defaultInstallerPath = RunInnoSetup -name "Prod" `
#                 -version $version `
#                 -urlPath $prodUrlPath `
#                 -includeDownloadSection "true"
                
#     $updateInstallerPath = RunInnoSetup -name "Update" `
#                 -version $version `
#                 -urlPath $prodUrlPath `
#                 -includeDownloadSection "false"
# }
# else
# {
#     $branchFolder = "Staging"
#     $defaultInstallerPath = RunInnoSetup -name "Staging" `
#                 -version $version `
#                 -urlPath $stagingUrlPath `
#                 -includeDownloadSection "true"
                
#     $updateInstallerPath = RunInnoSetup -name "Update" `
#                 -version $version `
#                 -urlPath $stagingUrlPath `
#                 -includeDownloadSection "false"
# }


# SignFile -filePath $defaultInstallerPath -certPath $certPath -certPwd $certPwd
# SignFile -filePath $updateInstallerPath -certPath $certPath -certPwd $certPwd

# $updateInstallerFolderPath = Split-Path -Path $updateInstallerPath
# $AppcastFolderUrl = "https://$bucketName.s3.$awsRegion.amazonaws.com/CICD-tests/$branchFolder"
# GenerateAppcast -AppPath $updateInstallerFolderPath -OutputFolder $publishFolder -AppcastFolderUrl $AppcastFolderUrl

# aws s3 cp $defaultInstallerPath s3://$bucketName/CICD-tests/$branchFolder/
# aws s3 cp $updateInstallerPath s3://$bucketName/CICD-tests/$branchFolder/

# $appcastOutputPath = "$publishFolder\appcast.xml"
# aws s3 cp $appcastOutputPath s3://$bucketName/CICD-tests/$branchFolder/
# $appcastSignatureOutputPath = "$publishFolder\appcast.xml.signature"
# aws s3 cp $appcastSignatureOutputPath s3://$bucketName/CICD-tests/$branchFolder/


# direct import
if ($direct_import_enabled -eq "true") {
    Write-Host "Building DirectImport"
    $directImportConfigurations = @("2022","2023","2024","2025")
    foreach ($config in $directImportConfigurations) {
        Write-Host "Building DirectImport $config"
        $projectName = "DirectImport"
        $projectPath = ${addinProjects}[$projectName]
        if (-not (Restore-And-Build-Project -projectName $projectName -projectPath $projectPath -config $config)) {
            Write-Host 
            return
        }
        $sourceDir = "C:\workspace\revit-addin\DirectImport\bin\Debug\Forge$config"
        $bundleDir = "C:\workspace\revit-addin\DirectImport\assets\DirectImport.bundle\Contents"
        $zipPath = "C:\workspace\revit-addin\DirectImport\assets\DirectImport$config.zip"

        New-Item -ItemType Directory -Force -Path $bundleDir
        Copy-Item "$sourceDir/DirectImport.dll" -Destination $bundleDir -Force
        Copy-Item "$sourceDir/DirectImport.pdb" -Destination $bundleDir -Force
        Compress-Archive -Path "C:\workspace\revit-addin\DirectImport\assets\DirectImport.bundle" -DestinationPath $zipPath -Force
        $zipSize = (Get-Item $zipPath).length
        Write-Host "DirectImport$config.zip size: $zipSize bytes"

        if ($config -eq "2025") {
            Copy-Item $zipPath -Destination "C:\workspace\revit-addin\DirectImport\assets\DirectImport.zip" -Force
            Write-Host "DirectImport.zip created in assets from DirectImport$config.zip"
        }
    }

    # $environments = @("Staging", "Prod")
    $environments = @("Testing")
    foreach ($environment in $environments) {
        Write-Host "Building DirectImport for $environment"
        if ($environment -eq "Staging") {
            $FORGE_CLIENT_ID = $env:STAGING_CLIENT_ID
            $FORGE_CLIENT_SECRET = $env:STAGING_CLIENTSECRET
        } elseif ($environment -eq "Prod") {
            $FORGE_CLIENT_ID = $env:PROD_CLIENT_ID
            $FORGE_CLIENT_SECRET = $env:PROD_CLIENTSECRET
        }
        elseif ($environment -eq "Testing") {
            $FORGE_CLIENT_ID = $env:TESTING_CLIENT_ID
            $FORGE_CLIENT_SECRET = $env:TESTING_CLIENTSECRET
        }
        $accessToken = GetForgeAccessTokens -clientId $FORGE_CLIENT_ID -clientSecret $FORGE_CLIENT_SECRET -environment $environment

        $bundlePaths = Get-ChildItem -Path "C:\workspace\revit-addin\DirectImport\assets" -File | Where-Object { $_.Extension -eq ".zip" }
        foreach ($bundlePath in $bundlePaths) {
            $bundleName = Split-Path $bundlePath -Leaf
            $bundleName = $bundleName.Replace(".zip", "")
            $bundleId = $FORGE_CLIENT_ID + $bundleName + "AppBundle" + "+dev"
            UpdateForgeBundle -bundleId $bundleId -bundleName $bundleName -bundlePath $bundlePath -accessToken $accessToken
        }
    }
}