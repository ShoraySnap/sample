$branch= &git rev-parse --abbrev-ref HEAD
$date= Get-Date -format "yyyyMMdd"
$version= -join($branch, "_", $date)

if ($branch -eq "master")
{
    $version= Get-Content -Path .\version.txt -TotalCount 1
    C:\"Program Files (x86)"\"Inno Setup 6"\ISCC.exe snaptrude-manager-prod.iss /DMyAppVersion=$version
    C:\"Program Files (x86)"\"Inno Setup 6"\ISCC.exe snaptrude-manager-wework.iss /DMyAppVersion=$version
    C:\"Program Files (x86)"\"Inno Setup 6"\ISCC.exe snaptrude-manager-update.iss /DMyAppVersion=$version
}
elseif ($branch -eq "dev")
{
    $version= Get-Content -Path .\version.txt -TotalCount 1
    C:\"Program Files (x86)"\"Inno Setup 6"\ISCC.exe snaptrude-manager-staging.iss /DMyAppVersion=$version
    C:\"Program Files (x86)"\"Inno Setup 6"\ISCC.exe snaptrude-manager-update.iss /DMyAppVersion=$version
}
else
{
    C:\"Program Files (x86)"\"Inno Setup 6"\ISCC.exe snaptrude-manager-staging.iss /DMyAppVersion=$version
    C:\"Program Files (x86)"\"Inno Setup 6"\ISCC.exe snaptrude-manager-update.iss /DMyAppVersion=$version
}

