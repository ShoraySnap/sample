param (
    [string]$configuration = "Debug2022",
    [string]$projectName = "Revit.TestRunner",
    [string]$addinSourceDir = "",
    [string]$targetPath = ""
)

Write-Host "##### Run InstallAddinForDebuggingScript.ps1 #####"

# Exit Codes
$errorIncorrectparamConfiguration = 101
$errorIncorrectparamProjectName = 102
$errorIncorrectparamAddinDir = 103
$errorIncorrectparamTargetPath = 104

$errorAddinFileDoesNotExist = 111
$errorAddinTargetDoesNotExist = 113

# General Parameters
$addinRootPath = $env:ProgramData
$addinPathRelative = "Autodesk\Revit\Addins\"

# Validate Input parameters
if ($configuration.length -lt 4){
    Write-Host "param configuration must have at least 4 characters"
    exit $errorIncorrectparamConfiguration
}
$revitVersion = $configuration.Substring($configuration.Length-4, 4)
Write-Host "Install for Revit version "$revitVersion

if($projectName.Equals("")){
    Write-Host "param projectName must not be empty"
    exit $errorIncorrectparamProjectName
}

if(!(Test-Path $addinSourceDir)){
    Write-Host "addin does not exist exist '$addinSourceDir'"
    exit $errorIncorrectparamAddinDir
}

if(!(Test-Path $targetPath)){
    Write-Host "target path does not exist '$targetPath'"
    exit $errorIncorrectparamTargetPath
}



# addin source file
$addinFileName = "{0}.addin" -f $projectName
$sourceAddinFile = Join-Path -Path $addinSourceDir -ChildPath $addinFileName
#Write-Host "Source addin File "$sourceAddinFile

if(!(Test-Path $sourceAddinFile)){
    Write-Host "source addin file does not exist '$sourceAddinFile'"
    exit $errorAddinFileDoesNotExist
}

$addinPath = Join-Path -Path $addinRootPath -ChildPath $addinPathRelative
$addinVersionPath = Join-Path -Path $addinPath -ChildPath $revitVersion
if(!(Test-Path $addinVersionPath)){
    Write-Host "addin target path does not exist '$addinVersionPath'"
    exit $errorAddinTargetDoesNotExist
}

$targetAddinFile = "{0}\{1}" -f $addinVersionPath, $addinFileName

# Copy addin file to target path
Write-Host "Install addin file "$targetAddinFile
Copy-Item $sourceAddinFile -Destination $addinVersionPath -Force

# Manipulate target addin File
Write-Host "Executing assembly "$targetPath
(Get-Content $targetAddinFile).Replace('###insertExecutingAssembly###', $targetPath) | Set-Content $targetAddinFile
