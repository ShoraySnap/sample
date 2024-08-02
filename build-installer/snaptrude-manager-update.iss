; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "Snaptrude"
#define MyAppPublisher "Snaptrude, Inc."
#define MyAppURL "https://www.snaptrude.com/"
#define MyAppExeName "MyProg.exe"
#define MyAppAssocName MyAppName + " File"
#define MyAppAssocExt ".trude"
#define MyAppAssocKey StringChange(MyAppAssocName, " ", "") + MyAppAssocExt
#define Base "..\build-installer"
#define BaseInstallers Base + "\installers"
#define BaseMisc Base + "\misc"
#define BaseRevitAddinFiles Base + "\revit-addin-files"
#define RevitAddinDllPath "..\revit-addin\SnaptrudeManagerAddin\bin\Debug"
#define UIBuildPath "..\revit-addin\SnaptrudeManagerUI\bin\Debug\net6.0-windows"
#define BaseOut Base + "\out"

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{04543C37-78DF-490C-B627-7082E682FB68}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
CreateAppDir=no
ChangesAssociations=yes
; Uncomment the following line to run in non administrative install mode (install for current user only.)
;PrivilegesRequired=lowest
OutputDir={#BaseOut}
;OutputBaseFilename=snaptrude-manager-setup-{#MyAppVersion}
SetupIconFile={#Base}\favicon.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
VersionInfoVersion=1.0.0.0

OutputBaseFilename=snaptrude-manager-setup-{#MyAppVersion}-Update

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[InstallDelete]
Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2019\Revit2Snaptrude\Revit2Snaptrude.dll"
Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2020\Revit2Snaptrude\Revit2Snaptrude.dll"; 
Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2021\Revit2Snaptrude\Revit2Snaptrude.dll"; 
Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2022\Revit2Snaptrude\Revit2Snaptrude.dll"; 
Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2023\Revit2Snaptrude\Revit2Snaptrude.dll";
Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2024\Revit2Snaptrude\Revit2Snaptrude.dll"; 


Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2019\Revit2Snaptrude.addin"
Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2020\Revit2Snaptrude.addin"; 
Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2021\Revit2Snaptrude.addin"; 
Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2022\Revit2Snaptrude.addin"; 
Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2023\Revit2Snaptrude.addin"; 
Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2024\Revit2Snaptrude.addin"; 

[Files]

Source: "{#BaseMisc}\urlsstaging.json"; DestDir: "{commonappdata}\snaptrude-manager"; DestName: "urls.json"; Flags: ignoreversion;
Source: "{#UIBuildPath}\SnaptrudeManagerUI.exe"; DestDir: "{commonappdata}\snaptrude-manager\UI"; Flags: ignoreversion;

;2019
Source: "{#RevitAddinDllPath}\2019\*.dll"; DestDir: "{autoappdata}\Autodesk\Revit\Addins\2019\SnaptrudeManagerAddin"; Flags: ignoreversion; Check: DirExists(ExpandConstant('{autoappdata}\Autodesk\Revit\Addins\2019'))
Source: "{#BaseRevitAddinFiles}\SnaptrudeManagerAddin.addin"; DestDir: "{autoappdata}\Autodesk\Revit\Addins\2019"; Flags: ignoreversion; Check: DirExists(ExpandConstant('{autoappdata}\Autodesk\Revit\Addins\2019'))
;2020
Source: "{#RevitAddinDllPath}\2020\*.dll"; DestDir: "{autoappdata}\Autodesk\Revit\Addins\2020\SnaptrudeManagerAddin"; Flags: ignoreversion; Check: DirExists(ExpandConstant('{autoappdata}\Autodesk\Revit\Addins\2020'))
Source: "{#BaseRevitAddinFiles}\SnaptrudeManagerAddin.addin"; DestDir: "{autoappdata}\Autodesk\Revit\Addins\2020"; Flags: ignoreversion; Check: DirExists(ExpandConstant('{autoappdata}\Autodesk\Revit\Addins\2020'))
;2021
Source: "{#RevitAddinDllPath}\2021\*.dll"; DestDir: "{autoappdata}\Autodesk\Revit\Addins\2021\SnaptrudeManagerAddin"; Flags: ignoreversion; Check: DirExists(ExpandConstant('{autoappdata}\Autodesk\Revit\Addins\2021'))
Source: "{#BaseRevitAddinFiles}\SnaptrudeManagerAddin.addin"; DestDir: "{autoappdata}\Autodesk\Revit\Addins\2021"; Flags: ignoreversion; Check: DirExists(ExpandConstant('{autoappdata}\Autodesk\Revit\Addins\2021'))
;2022
Source: "{#RevitAddinDllPath}\2022\*.dll"; DestDir: "{autoappdata}\Autodesk\Revit\Addins\2022\SnaptrudeManagerAddin"; Flags: ignoreversion; Check: DirExists(ExpandConstant('{autoappdata}\Autodesk\Revit\Addins\2022'))
Source: "{#BaseRevitAddinFiles}\SnaptrudeManagerAddin.addin"; DestDir: "{autoappdata}\Autodesk\Revit\Addins\2022"; Flags: ignoreversion; Check: DirExists(ExpandConstant('{autoappdata}\Autodesk\Revit\Addins\2022'))
;2023
Source: "{#RevitAddinDllPath}\2023\*.dll"; DestDir: "{autoappdata}\Autodesk\Revit\Addins\2023\SnaptrudeManagerAddin"; Flags: ignoreversion; Check: DirExists(ExpandConstant('{autoappdata}\Autodesk\Revit\Addins\2023'))
Source: "{#BaseRevitAddinFiles}\SnaptrudeManagerAddin.addin"; DestDir: "{autoappdata}\Autodesk\Revit\Addins\2023"; Flags: ignoreversion; Check: DirExists(ExpandConstant('{autoappdata}\Autodesk\Revit\Addins\2023'))
;2024
Source: "{#RevitAddinDllPath}\2024\*.dll"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2024\SnaptrudeManagerAddin"; Flags: ignoreversion; Check: DirExists(ExpandConstant('{userappdata}\Autodesk\Revit\Addins\2024'))
Source: "{#BaseRevitAddinFiles}\SnaptrudeManagerAddin.addin"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2024"; Flags: ignoreversion; Check: DirExists(ExpandConstant('{userappdata}\Autodesk\Revit\Addins\2024'))
;2025
Source: "{#RevitAddinDllPath}\2025\*.dll"; DestDir: "{autoappdata}\Autodesk\Revit\Addins\2025\SnaptrudeManagerAddin"; Flags: ignoreversion; Check: DirExists(ExpandConstant('{userappdata}\Autodesk\Revit\Addins\2025'))
Source: "{#BaseRevitAddinFiles}\SnaptrudeManagerAddin.addin"; DestDir: "{autoappdata}\Autodesk\Revit\Addins\2025"; Flags: ignoreversion; Check: DirExists(ExpandConstant('{userappdata}\Autodesk\Revit\Addins\2025'))

; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Registry]
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocExt}\OpenWithProgids"; ValueType: string; ValueName: "{#MyAppAssocKey}"; ValueData: ""; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}"; ValueType: string; ValueName: ""; ValueData: "{#MyAppAssocName}"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""
Root: HKA; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".myp"; ValueData: ""

