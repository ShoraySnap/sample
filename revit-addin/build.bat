@echo starting
dotnet restore -p:Configuration=2019 -p:TargetFramework=net47
MSBuild .\SnaptrudeManagerAddin\SnaptrudeManagerAddin.csproj /t:Rebuild -p:Configuration=2019 -p:Platform="Any CPU" -p:TargetFramework="net47"
MSBuild .\SnaptrudeForgeExport\SnaptrudeForgeExport.csproj /t:Rebuild -p:Configuration=2019 -p:Platform="Any CPU" -p:TargetFramework="net47"
dotnet restore -p:Configuration=2020 -p:TargetFramework=net47
MSBuild .\SnaptrudeManagerAddin\SnaptrudeManagerAddin.csproj /t:Rebuild -p:Configuration=2020 -p:Platform="Any CPU" -p:TargetFramework="net47"
MSBuild .\SnaptrudeForgeExport\SnaptrudeForgeExport.csproj /t:Rebuild -p:Configuration=2020 -p:Platform="Any CPU" -p:TargetFramework="net47"
dotnet restore -p:Configuration=2021 -p:TargetFramework=net48
MSBuild .\SnaptrudeManagerAddin\SnaptrudeManagerAddin.csproj /t:Rebuild -p:Configuration=2021 /p:Platform="Any CPU" /p:TargetFramework="net48"
MSBuild .\SnaptrudeForgeExport\SnaptrudeForgeExport.csproj /t:Rebuild -p:Configuration=2021 /p:Platform="Any CPU" /p:TargetFramework="net48"
dotnet restore -p:Configuration=2022 -p:TargetFramework=net48
MSBuild .\SnaptrudeManagerAddin\SnaptrudeManagerAddin.csproj /t:Rebuild -p:Configuration=2022 /p:Platform="Any CPU" /p:TargetFramework="net48"
MSBuild .\SnaptrudeForgeExport\SnaptrudeForgeExport.csproj /t:Rebuild -p:Configuration=2022 /p:Platform="Any CPU" /p:TargetFramework="net48"
dotnet restore -p:Configuration=2023 -p:TargetFramework=net48
MSBuild .\SnaptrudeManagerAddin\SnaptrudeManagerAddin.csproj /t:Rebuild -p:Configuration=2023 /p:Platform="Any CPU" /p:TargetFramework="net48"
MSBuild .\SnaptrudeForgeExport\SnaptrudeForgeExport.csproj /t:Rebuild -p:Configuration=2023 /p:Platform="Any CPU" /p:TargetFramework="net48"
dotnet restore -p:Configuration=2024 -p:TargetFramework=net48
MSBuild .\SnaptrudeManagerAddin\SnaptrudeManagerAddin.csproj /t:Rebuild -p:Configuration=2024 /p:Platform="Any CPU" /p:TargetFramework="net48"
MSBuild .\SnaptrudeForgeExport\SnaptrudeForgeExport.csproj /t:Rebuild -p:Configuration=2024 /p:Platform="Any CPU" /p:TargetFramework="net48"
@echo finished