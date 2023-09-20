$branch= &git rev-parse --abbrev-ref HEAD
$date= Get-Date -format "yyyyMMdd"
$version= -join($branch, "_", $date)
$dynamo_script_version= Get-Content -Path .\dynamo_script_version.txt -TotalCount 1

if ($branch -eq "master")
{
    $version= Get-Content -Path .\version.txt -TotalCount 1
    C:\"Program Files (x86)"\"Inno Setup 6"\ISCC.exe snaptrude-manager-prod.iss /DMyAppVersion=$version /DDynamoScriptVersion=$dynamo_script_version
    C:\"Program Files (x86)"\"Inno Setup 6"\ISCC.exe snaptrude-manager-wework.iss /DMyAppVersion=$version /DDynamoScriptVersion=$dynamo_script_version
    C:\"Program Files (x86)"\"Inno Setup 6"\ISCC.exe snaptrude-manager-preset.iss /DMyAppVersion=$version /DDynamoScriptVersion=$dynamo_script_version
    C:\"Program Files (x86)"\"Inno Setup 6"\ISCC.exe snaptrude-manager-update.iss /DMyAppVersion=$version /DDynamoScriptVersion=$dynamo_script_version
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
