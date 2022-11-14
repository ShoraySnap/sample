; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "Snaptrude"
#define MyAppVersion "1.0.6"
#define MyAppPublisher "Snaptrude, Inc."
#define MyAppURL "https://www.snaptrude.com/"
#define MyAppExeName "MyProg.exe"
#define MyAppAssocName MyAppName + " File"
#define MyAppAssocExt ".trude"
#define MyAppAssocKey StringChange(MyAppAssocName, " ", "") + MyAppAssocExt
#define Base "C:\Users\nisch\snaptrudemanager\build-installer"
#define BaseDynamoScripts Base + "\dynamo-scripts"
#define BaseInstallers Base + "\installers"
#define BaseMisc Base + "\misc"
#define BaseRevitAddinFiles Base + "\revit-addin-files"
#define BaseOut Base + "\out"
#define DynamoScriptVersion "1.4.9"
;1 is true, 0 is false

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
; PrivilegesRequired=lowest
OutputDir={#BaseOut}
;OutputBaseFilename=snaptrude-manager-setup-{#MyAppVersion}
SetupIconFile={#Base}\favicon.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
VersionInfoVersion=1.0.0.0

OutputBaseFilename=snaptrude-manager-setup-{#MyAppVersion}-WeWork

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Code]

function ShouldInstallDynamo: Boolean;
var
  exists: Boolean;
  path: String;
begin
  path := ExpandConstant('{userappdata}') + '\Autodesk\RVT\2019';
  exists := DirExists(path);
  Result := exists;
end;

[Files]
Source: "{#BaseMisc}\urlswework.json"; DestDir: "{userappdata}\snaptrude-manager"; DestName: "urls.json"; Flags: ignoreversion;

;dynamo scripts
Source: "{#BaseDynamoScripts}\revit-snaptrude-{#DynamoScriptVersion}-2019.dyn"; DestDir: "{userappdata}\snaptrude-manager"; DestName: "revit-snaptrude-2019.dyn"; Flags: ignoreversion
Source: "{#BaseDynamoScripts}\revit-snaptrude-{#DynamoScriptVersion}-2020.dyn"; DestDir: "{userappdata}\snaptrude-manager"; DestName: "revit-snaptrude-2020.dyn"; Flags: ignoreversion
Source: "{#BaseDynamoScripts}\revit-snaptrude-{#DynamoScriptVersion}.dyn"; DestDir: "{userappdata}\snaptrude-manager"; DestName: "revit-snaptrude.dyn"; Flags: ignoreversion

;2019
Source: "{#BaseRevitAddinFiles}\Revit2Snaptrude.dll"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2019\Revit2Snaptrude"; Flags: ignoreversion; Check: DirExists(ExpandConstant('{userappdata}\Autodesk\Revit\Addins\2019'))
Source: "{#BaseRevitAddinFiles}\Revit2Snaptrude.addin"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2019"; Flags: ignoreversion; Check: DirExists(ExpandConstant('{userappdata}\Autodesk\Revit\Addins\2019'))
;2020
Source: "{#BaseRevitAddinFiles}\Revit2Snaptrude.dll"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2020\Revit2Snaptrude"; Flags: ignoreversion; Check: DirExists(ExpandConstant('{userappdata}\Autodesk\Revit\Addins\2020'))
Source: "{#BaseRevitAddinFiles}\Revit2Snaptrude.addin"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2020"; Flags: ignoreversion; Check: DirExists(ExpandConstant('{userappdata}\Autodesk\Revit\Addins\2020'))
;2021
Source: "{#BaseRevitAddinFiles}\Revit2Snaptrude.dll"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2021\Revit2Snaptrude"; Flags: ignoreversion; Check: DirExists(ExpandConstant('{userappdata}\Autodesk\Revit\Addins\2021'))
Source: "{#BaseRevitAddinFiles}\Revit2Snaptrude.addin"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2021"; Flags: ignoreversion; Check: DirExists(ExpandConstant('{userappdata}\Autodesk\Revit\Addins\2021'))
;2022
Source: "{#BaseRevitAddinFiles}\Revit2Snaptrude.dll"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2022\Revit2Snaptrude"; Flags: ignoreversion; Check: DirExists(ExpandConstant('{userappdata}\Autodesk\Revit\Addins\2022'))
Source: "{#BaseRevitAddinFiles}\Revit2Snaptrude.addin"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2022"; Flags: ignoreversion; Check: DirExists(ExpandConstant('{userappdata}\Autodesk\Revit\Addins\2022'))
;2023
Source: "{#BaseRevitAddinFiles}\Revit2Snaptrude.dll"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2023\Revit2Snaptrude"; Flags: ignoreversion; Check: DirExists(ExpandConstant('{userappdata}\Autodesk\Revit\Addins\2023'))
Source: "{#BaseRevitAddinFiles}\Revit2Snaptrude.addin"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2023"; Flags: ignoreversion; Check: DirExists(ExpandConstant('{userappdata}\Autodesk\Revit\Addins\2023'))
; NOTE: Don't use "Flags: ignoreversion" on any shared system files


Source: "{#BaseMisc}\custom_families\*" ; DestDir: "{commonappdata}\Snaptrude\"; Flags: ignoreversion recursesubdirs

Source: "{#BaseInstallers}\*.exe"; DestDir: "{tmp}"; Flags: createallsubdirs recursesubdirs deleteafterinstall ignoreversion uninsremovereadonly; 


[Run]

FileName: "{tmp}\DynamoInstall2.0.4.exe"; Description: "Install Dynamo"; Check: ShouldInstallDynamo; Flags: postinstall shellexec waituntilterminated

Filename: "{tmp}\dynamo-2.6.1.exe"; Description: "Install Dynamo Connector"; Flags: postinstall shellexec waituntilterminated

Filename: "{tmp}\snaptrude-manager-1.0.0 Setup.exe"; Description: "Install Snaptrude Manager"; Flags: postinstall shellexec waituntilterminated

[Registry]
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocExt}\OpenWithProgids"; ValueType: string; ValueName: "{#MyAppAssocKey}"; ValueData: ""; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}"; ValueType: string; ValueName: ""; ValueData: "{#MyAppAssocName}"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""
Root: HKA; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".myp"; ValueData: ""

