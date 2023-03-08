## Compress and copy bundle

# copyfiles
Copy-Item "./bin/Debug/SnaptrudeForgeExport.dll" -Destination "./assets/UpdateRVTParam.bundle/Contents/"
Copy-Item "./bin/Debug/SnaptrudeForgeExport.pdb" -Destination "./assets/UpdateRVTParam.bundle/Contents/"

# zip UpdateRVTParam.bundle directory and move to bundles directory
$compressUpdateRvtParamBundle = @{
  Path = "./assets/UpdateRVTParam.bundle"
  DestinationPath = "./assets/UpdateRVTParam.zip"
}
Compress-Archive @compressUpdateRvtParamBundle -Force

Copy-Item "./assets/UpdateRVTParam.zip" -Destination "./assets/UpdateRVTParamPdf.zip"
