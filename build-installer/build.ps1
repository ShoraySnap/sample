$branch= &git rev-parse --abbrev-ref HEAD
$date= Get-Date -format "yyyyMMdd"
$version= -join($branch, "_", $date)
$dynamo_script_version= Get-Content -Path .\dynamo_script_version.txt -TotalCount 1
$dllRelativePath = "..\revit-addin\SnaptrudeManagerAddin\bin\Debug"

if ($branch -eq "master")
{
    $certPath = Read-Host "Enter pfx file path"
    $certPwd = Read-Host "Enter certificate password" -AsSecureString
    $plainPwd =[Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($certPwd))

    signtool.exe sign /f $certPath /fd SHA256 /p $plainPwd.ToString() /t http://timestamp.digicert.com "$dllRelativePath\2019\SnaptrudeManagerAddin.dll"
    signtool.exe sign /f $certPath /fd SHA256 /p $plainPwd.ToString() /t http://timestamp.digicert.com "$dllRelativePath\2020\SnaptrudeManagerAddin.dll"
    signtool.exe sign /f $certPath /fd SHA256 /p $plainPwd.ToString() /t http://timestamp.digicert.com "$dllRelativePath\2021\SnaptrudeManagerAddin.dll"
    signtool.exe sign /f $certPath /fd SHA256 /p $plainPwd.ToString() /t http://timestamp.digicert.com "$dllRelativePath\2022\SnaptrudeManagerAddin.dll"
    signtool.exe sign /f $certPath /fd SHA256 /p $plainPwd.ToString() /t http://timestamp.digicert.com "$dllRelativePath\2023\SnaptrudeManagerAddin.dll"
    signtool.exe sign /f $certPath /fd SHA256 /p $plainPwd.ToString() /t http://timestamp.digicert.com "$dllRelativePath\2024\SnaptrudeManagerAddin.dll"
    signtool.exe sign /f $certPath /fd SHA256 /p $plainPwd.ToString() /t http://timestamp.digicert.com "installers\snaptrude-manager-1.0.0 Setup.exe"

    $version= Get-Content -Path .\version.txt -TotalCount 1
    C:\"Program Files (x86)"\"Inno Setup 6"\ISCC.exe snaptrude-manager-prod.iss /DMyAppVersion=$version /DDynamoScriptVersion=$dynamo_script_version
    C:\"Program Files (x86)"\"Inno Setup 6"\ISCC.exe snaptrude-manager-wework.iss /DMyAppVersion=$version /DDynamoScriptVersion=$dynamo_script_version
    #C:\"Program Files (x86)"\"Inno Setup 6"\ISCC.exe snaptrude-manager-preset.iss /DMyAppVersion=$version /DDynamoScriptVersion=$dynamo_script_version
    C:\"Program Files (x86)"\"Inno Setup 6"\ISCC.exe snaptrude-manager-update.iss /DMyAppVersion=$version /DDynamoScriptVersion=$dynamo_script_version

    signtool.exe sign /f $certPath /fd SHA256 /p $plainPwd.ToString() /t http://timestamp.digicert.com "out\snaptrude-manager-setup-v3.1.0.exe"
    signtool.exe sign /f $certPath /fd SHA256 /p $plainPwd.ToString() /t http://timestamp.digicert.com "out\snaptrude-manager-setup-v3.1.0-WeWork.exe"
}
elseif ($branch -eq "dev")
{
    $version_number= Get-Content -Path .\version.txt -TotalCount 1
    $version= -join("dev-", $version_number)
    C:\"Program Files (x86)"\"Inno Setup 6"\ISCC.exe snaptrude-manager-staging.iss /DMyAppVersion=$version /DDynamoScriptVersion=$dynamo_script_version
    C:\"Program Files (x86)"\"Inno Setup 6"\ISCC.exe snaptrude-manager-update.iss /DMyAppVersion=$version /DDynamoScriptVersion=$dynamo_script_version
}
else
{
    C:\"Program Files (x86)"\"Inno Setup 6"\ISCC.exe snaptrude-manager-staging.iss /DMyAppVersion=$version /DDynamoScriptVersion=$dynamo_script_version
    C:\"Program Files (x86)"\"Inno Setup 6"\ISCC.exe snaptrude-manager-update.iss /DMyAppVersion=$version /DDynamoScriptVersion=$dynamo_script_version
}

git tag -a $version -m $version
