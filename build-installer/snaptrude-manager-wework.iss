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
#define BaseDynamoScripts Base + "\dynamo-scripts"
#define BaseInstallers Base + "\installers"
#define BaseMisc Base + "\misc"
#define BaseRevitAddinFiles Base + "\revit-addin-files"
#define RevitAddinDllPath "..\revit-addin\SnaptrudeManagerAddin\bin\Debug"
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

var
  CheckListBoxPage: TInputOptionWizardPage;
  DownloadPage: TDownloadWizardPage;
  FileURLs: TArrayOfString;
  Versions: TArrayOfString;

function OnDownloadProgress(const Url, FileName: String; const Progress, ProgressMax: Int64): Boolean;
begin
  if Progress = ProgressMax then
    Log(Format('Successfully downloaded file to {tmp}: %s', [FileName]));
  Result := True;
end;

procedure InitializeWizard;
var
  I: Integer;
begin
    AllFileURLs := [
    'https://snaptrude-prod-data.s3.ap-south-1.amazonaws.com/media/manager/rfas/2019.zip',
    'https://snaptrude-prod-data.s3.ap-south-1.amazonaws.com/media/manager/rfas/2020.zip',
    'https://snaptrude-prod-data.s3.ap-south-1.amazonaws.com/media/manager/rfas/2021.zip',
    'https://snaptrude-prod-data.s3.ap-south-1.amazonaws.com/media/manager/rfas/2022.zip',
    'https://snaptrude-prod-data.s3.ap-south-1.amazonaws.com/media/manager/rfas/2023.zip',
    'https://snaptrude-prod-data.s3.ap-south-1.amazonaws.com/media/manager/rfas/2024.zip'
    ];
    AllVersions := ['2019','2020','2021','2022','2023','2024'];

  // Create a page with checkboxes for each file
  CheckListBoxPage := CreateInputOptionPage(wpSelectTasks, 'Select Revit versions', 'ATTENTION: Only select the versions that you are going to use the Snaptrude <-> Revit Link. Several families will be downloaded for each selected Revit version.', '', False, True);
  InstalledVersions := TStringList.Create;
  InstalledVersionsURLs := TStringList.Create;
  for I := 0 to High(AllVersions) do
    begin
      InstalledVersions.Add(AllVersions[I]);
      InstalledVersionsURLs.Add(AllFileURLs[I]);
      CheckListBoxPage.Add('Revit ' + AllVersions[I]);
    end;
  // Create a download page
  DownloadPage := CreateDownloadPage(SetupMessage(msgWizardPreparing), SetupMessage(msgPreparingDesc), @OnDownloadProgress);
end;

procedure unzip(ZipFile, TargetFldr: PAnsiChar);
var
  shellobj: variant;
  ZipFileV, TargetFldrV: variant;
  SrcFldr, DestFldr: variant;
  shellfldritems: variant;
begin
  if FileExists(ZipFile) then begin
    ForceDirectories(TargetFldr);
    shellobj := CreateOleObject('Shell.Application');
    ZipFileV := string(ZipFile);
    TargetFldrV := string(TargetFldr);
    SrcFldr := shellobj.NameSpace(ZipFileV);
    DestFldr := shellobj.NameSpace(TargetFldrV);
    shellfldritems := SrcFldr.Items;
    DestFldr.CopyHere(shellfldritems);  
  end;
end;

procedure ExtractMe(src, target : AnsiString);
begin
  unzip(ExpandConstant(src), ExpandConstant(target));
end;

function InstallVersion (VersionToCheck: String): Boolean;
var
  I: Integer;
  install: Boolean;
begin
  install := false;
  for I := 0 to InstalledVersions.Count - 1 do
    begin
      if VersionToCheck = InstalledVersions[I] then
        if CheckListBoxPage.Values[I] then
          install := true;
    end;
  if (install) then
    Result := true
  else
    Result := false;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  I: Integer;
  CheckedCount: Integer;
begin
  Result := True;
  if CurPageID = CheckListBoxPage.ID then
    begin
      CheckedCount := 0;
      
      for I := 0 to InstalledVersions.Count - 1 do
      
      if CheckListBoxPage.Values[I] then
        Inc(CheckedCount);
      
      if CheckedCount = 0 then
        begin
        MsgBox('Please select at least one version to install.', mbError, MB_OK);
        Result := False;
        end
      else
        begin
          DownloadPage.Clear;
          for I := 0 to InstalledVersions.Count - 1 do
            if CheckListBoxPage.Values[I] then
              try
                DownloadPage.Add(InstalledVersionsURLs[I], InstalledVersions[I] + '.zip', '');
                Result := True;
              except
                Log(GetExceptionMessage);
                Result := False;
              end;
          DownloadPage.Show;
          try
            try
              DownloadPage.Download; // This downloads the files to {tmp}
              Result := True;
              for I := 0 to InstalledVersions.Count - 1 do
                if CheckListBoxPage.Values[I] then
                  ExtractMe('{tmp}\' + InstalledVersions[I] + '.zip','{tmp}\' + InstalledVersions[I] + '\');
            except
              if DownloadPage.AbortedByUser then
                Log('Aborted by user.')
              else
                SuppressibleMsgBox(AddPeriod(GetExceptionMessage), mbCriticalError, MB_OK, IDOK);
              Result := False;
            end;
            finally
              DownloadPage.Hide;
            end;
      end;
    end;
  end;
end.

[InstallDelete]
Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2019\Revit2Snaptrude\Revit2Snaptrude.dll"
Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2020\Revit2Snaptrude\Revit2Snaptrude.dll"; 
Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2021\Revit2Snaptrude\Revit2Snaptrude.dll"; 
Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2022\Revit2Snaptrude\Revit2Snaptrude.dll"; 
Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2023\Revit2Snaptrude\Revit2Snaptrude.dll"; 

Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2019\Revit2Snaptrude.addin"
Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2020\Revit2Snaptrude.addin"; 
Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2021\Revit2Snaptrude.addin"; 
Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2022\Revit2Snaptrude.addin"; 
Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2023\Revit2Snaptrude.addin"; 

[Files]
Source: "{#BaseMisc}\urlswework.json"; DestDir: "{userappdata}\snaptrude-manager"; DestName: "urls.json"; Flags: ignoreversion;

;dynamo scripts
Source: "{#BaseDynamoScripts}\revit-snaptrude-{#DynamoScriptVersion}-2019.dyn"; DestDir: "{userappdata}\snaptrude-manager"; DestName: "revit-snaptrude-2019.dyn"; Flags: ignoreversion
Source: "{#BaseDynamoScripts}\revit-snaptrude-{#DynamoScriptVersion}-2020.dyn"; DestDir: "{userappdata}\snaptrude-manager"; DestName: "revit-snaptrude-2020.dyn"; Flags: ignoreversion
Source: "{#BaseDynamoScripts}\revit-snaptrude-{#DynamoScriptVersion}.dyn"; DestDir: "{userappdata}\snaptrude-manager"; DestName: "revit-snaptrude.dyn"; Flags: ignoreversion

;2019
Source: "{#RevitAddinDllPath}\2019\SnaptrudeManagerAddin.dll"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2019\SnaptrudeManagerAddin"; Flags: ignoreversion; Check: InstallVersion('2019');
Source: "{#BaseRevitAddinFiles}\SnaptrudeManagerAddin.addin"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2019"; Flags: ignoreversion; Check: InstallVersion('2019');
Source: "{tmp}\2019\*"; DestDir: "{commonappdata}\Snaptrude\resourceFile\2019"; Flags: external  recursesubdirs; Check: InstallVersion('2019');
;2020
Source: "{#RevitAddinDllPath}\2020\SnaptrudeManagerAddin.dll"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2020\SnaptrudeManagerAddin"; Flags: ignoreversion; Check: InstallVersion('2020');
Source: "{#BaseRevitAddinFiles}\SnaptrudeManagerAddin.addin"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2020"; Flags: ignoreversion; Check: InstallVersion('2020');
Source: "{tmp}\2020\*"; DestDir: "{commonappdata}\Snaptrude\resourceFile\2020"; Flags: external  recursesubdirs; Check: InstallVersion('2020');
;2021
Source: "{#RevitAddinDllPath}\2021\SnaptrudeManagerAddin.dll"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2021\SnaptrudeManagerAddin"; Flags: ignoreversion; Check: InstallVersion('2021');
Source: "{#BaseRevitAddinFiles}\SnaptrudeManagerAddin.addin"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2021"; Flags: ignoreversion; Check: InstallVersion('2021');
Source: "{tmp}\2021\*"; DestDir: "{commonappdata}\Snaptrude\resourceFile\2021"; Flags: external  recursesubdirs; Check: InstallVersion('2021');
;2022
Source: "{#RevitAddinDllPath}\2022\SnaptrudeManagerAddin.dll"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2022\SnaptrudeManagerAddin"; Flags: ignoreversion; Check: InstallVersion('2022');
Source: "{#BaseRevitAddinFiles}\SnaptrudeManagerAddin.addin"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2022"; Flags: ignoreversion; Check: InstallVersion('2022');
Source: "{tmp}\2022\*"; DestDir: "{commonappdata}\Snaptrude\resourceFile\2022"; Flags: external  recursesubdirs; Check: InstallVersion('2022');
;2023
Source: "{#RevitAddinDllPath}\2023\SnaptrudeManagerAddin.dll"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2023\SnaptrudeManagerAddin"; Flags: ignoreversion; Check: InstallVersion('2023');
Source: "{#BaseRevitAddinFiles}\SnaptrudeManagerAddin.addin"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2023"; Flags: ignoreversion; Check: InstallVersion('2023');
Source: "{tmp}\2023\*"; DestDir: "{commonappdata}\Snaptrude\resourceFile\2023"; Flags: external  recursesubdirs; Check: InstallVersion('2023');
;2024
Source: "{#RevitAddinDllPath}\2024\SnaptrudeManagerAddin.dll"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2024\SnaptrudeManagerAddin"; Flags: ignoreversion; Check: InstallVersion('2024');
Source: "{#BaseRevitAddinFiles}\SnaptrudeManagerAddin.addin"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2024"; Flags: ignoreversion; Check: InstallVersion('2024');
Source: "{tmp}\2024\*"; DestDir: "{commonappdata}\Snaptrude\resourceFile\2024"; Flags: external  recursesubdirs; Check: InstallVersion('2024');

; NOTE: Don't use "Flags: ignoreversion" on any shared system files



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

