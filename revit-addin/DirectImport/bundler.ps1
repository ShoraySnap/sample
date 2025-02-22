param (
    [string]$RevitVersion = "2024"
)

# Define paths
$sourceDir = "./bin/Debug/Forge$RevitVersion"
$bundleDir = "./assets/DirectImport.bundle/Contents"
$zipPath = "./assets/DirectImport$RevitVersion.zip"
# $forgeserverPath="C:\Users\Shroy\Documents\forgeserver\bundles"

# Ensure the bundle directory exists
New-Item -ItemType Directory -Force -Path $bundleDir

# Copy files
Copy-Item "$sourceDir/DirectImport.dll" -Destination $bundleDir -Force
Copy-Item "$sourceDir/DirectImport.pdb" -Destination $bundleDir -Force

Compress-Archive -Path "./assets/DirectImport.bundle" -DestinationPath $zipPath -Force
 
# copy the file to $forgeserverPath
# Copy-Item $zipPath -Destination $forgeserverPath -Force
# Write-Host "Bundle copied to $forgeserverPath"

