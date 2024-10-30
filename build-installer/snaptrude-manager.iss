; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "Snaptrude"
#define MyAppPublisher "Snaptrude, Inc."
#define MyAppURL "https://www.snaptrude.com/"
#define MyAppExeName "SnaptrudeManagerUI.exe"
#define MyAppAssocName MyAppName + " File"
#define MyAppAssocExt ".trude"
#define MyAppAssocKey StringChange(MyAppAssocName, " ", "") + MyAppAssocExt
#define Base "..\build-installer"
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
ChangesAssociations=yes
Compression=lzma
CreateAppDir=no
OutputBaseFilename={#OutputBaseFileName}
DisableWelcomePage=no
OutputDir={#OutDir}
SetupIconFile={#Base}\snaptrude.ico
SolidCompression=yes
UninstallDisplayName=Snaptrude Manager
UninstallDisplayIcon={commonappdata}\SnaptrudeManager\UI\SnaptrudeManagerUI.exe
WizardStyle=modern
WizardImageFile=misc\background.bmp

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[UninstallRun]
Filename: "{localappdata}\snaptrude_manager\Update.exe"; Parameters: "--uninstall"; Flags: runhidden; Check: ShouldRunUninstaller

[Code]

var
  CheckListBoxPage: TInputOptionWizardPage;
  DownloadPage: TDownloadWizardPage;
  AllFileURLs: TArrayOfString;
  AllVersions: TArrayOfString;
  InstalledVersions: TStringList;
  InstalledVersionsURLs: TStringList;
  IncludeDownloadSectionStr: String;
  IncludeDownloadSection: Boolean;

function ShouldRunUninstaller(): Boolean;
begin
  Result := FileExists(ExpandConstant('{localappdata}\snaptrude_manager\Update.exe'));
end;

function IsInList(Item: string; Excludes: array of string): Boolean;
var
  I: Integer;
begin
  Result := False;
  for I := 0 to GetArrayLength(Excludes) - 1 do
  begin
    if CompareText(Item, Excludes[I]) = 0 then
    begin
      Result := True;
      Exit;
    end;
  end;
end;

function IsRevitOrSnaptrudeManagerRunning(): Boolean;
var
  ResultCode: Integer;
  OutputFilePath: string;
  Output: TStringList;
begin
  OutputFilePath := ExpandConstant('{tmp}\output.txt');
  Output := TStringList.Create();
  try
    Exec('cmd.exe', '/C tasklist | findstr /I "Revit.exe SnaptrudeManagerUI.exe snaptrude-manager.exe" > "' + OutputFilePath + '"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    if (ResultCode = 0) and FileExists(OutputFilePath) then
    begin
      Output.LoadFromFile(OutputFilePath);
      if (Pos('Revit.exe', Output.Text) > 0) or (Pos('SnaptrudeManagerUI.exe', Output.Text) > 0) or (Pos('snaptrude-manager.exe', Output.Text) > 0) then
      begin
        Result := True;
        Exit;
      end;
    end;
    Result := False;
  finally
    Output.Free;
    if FileExists(OutputFilePath) then
      DeleteFile(OutputFilePath);
  end;
end;

procedure ClearRFAFolders;
var
  Year: Integer;
  FolderPath: String;
begin
  for Year := 2019 to 2025 do
  begin
    FolderPath := ExpandConstant('{tmp}\') + IntToStr(Year);
    if DirExists(FolderPath) then
    begin
      DelTree(FolderPath, True, True, True);
    end;
  end;
end;

procedure DeleteFolderExcept(Dir: string; ExcludeFilesAndFolders: array of string);
var
  FindRec: TFindRec;
  FilePath: string;
begin
  if FindFirst(ExpandConstant(Dir + '\*'), FindRec) then
  begin
    try
      repeat
        FilePath := Dir + '\' + FindRec.Name;
        if (FindRec.Name <> '.') and (FindRec.Name <> '..') then
        begin
          if not IsInList(FindRec.Name, ExcludeFilesAndFolders) then
          begin
            if FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY <> 0 then
            begin
              DelTree(FilePath, True, True, True);
            end
            else
            begin
              DeleteFile(FilePath);
            end;
          end;
        end;
      until not FindNext(FindRec);
    finally
      FindClose(FindRec);
    end;
  end;
end;

var
  UninstallDone: Boolean;
  ResultCode: Integer;

procedure CurStepChanged(CurStep: TSetupStep);
var
  Excludes: array of string;
begin
  if CurStep = ssInstall then
  begin
    if ShouldRunUninstaller then
      begin
        WizardForm.StatusLabel.Caption := 'Uninstalling previous version...';
        WizardForm.ProgressGauge.Position := 0;
        if not UninstallDone then
        begin
          Exec(ExpandConstant('{localappdata}\snaptrude_manager\Update.exe'), '--uninstall', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
          UninstallDone := True;
        end;
        WizardForm.StatusLabel.Caption := SetupMessage(msgInstallingLabel);
      end;
    SetArrayLength(Excludes, 3);
    Excludes[0] := 'userPreferences.json';
    Excludes[1] := 'config.json';
    Excludes[2] := 'logs';

    DeleteFolderExcept(ExpandConstant('{userappdata}\snaptrude-manager'), Excludes);
  end
  else if CurStep = ssPostInstall then
  begin
    ClearRFAFolders;
  end;
end;

function IsRevitVersionInstalled(Version: string): Boolean;
var
  RegKey: string;
  Value: string;
begin
  Result := False;
  if RegKeyExists(HKCU, 'Software\Autodesk\Revit\Autodesk Revit ' + Version) then
  begin
    Result := True;
  end
end;

function IsSnaptrudeAddinVersionInstalled(Version: string): Boolean;
var
  RegKey: string;
  Value: string;
  AddinInstallPath: string;
begin
  Result := False;
  AddinInstallPath := ExpandConstant('{commonappdata}') + '\Autodesk\Revit\Addins\' + Version + '\SnaptrudeManagerAddin.addin';
  if FileExists(AddinInstallPath) then
  begin
    Result := True;
  end
end;

function InstallVersion (VersionToCheck: String; RFAs: Boolean): Boolean;
var
  I: Integer;
  install: Boolean;
begin
#if BuildName = "Update"
    begin
      Result := false;
      if RFAs then
        Result := false
      else if IsSnaptrudeAddinVersionInstalled(VersionToCheck) then
        Result := true;
    end;
#else
  if IncludeDownloadSection then
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
    end
  else
    begin
      if RFAs then
        Result := false
      else
        Result := true
    end
#endif
end;

function OnDownloadProgress(const Url, FileName: String; const Progress, ProgressMax: Int64): Boolean;
begin
  if Progress = ProgressMax then
    Log(Format('Successfully downloaded file to {tmp}: %s', [FileName]));
  Result := True;
end;

var
  ShowMessage: Boolean;

function IsExecutablePathProvided(): Boolean;
var
  ExecutablePath: String;
begin
  ExecutablePath := ExpandConstant('{param:ExecutablePath}');
  Result := ExecutablePath <> '';
end;

procedure InitializeWizard;
var
  ShowMessageStr: String;
  I: Integer;
begin
  WizardForm.WelcomeLabel2.Caption := 'This will install Snaptrude Manager version {#MyAppVersion} on your computer' + #13#10 + #13#10 + 'Please close all your Revit instances before continuing.' + #13#10 + #13#10 +  'Click Next to continue, or Cancel to exit setup.';
  IncludeDownloadSectionStr := ExpandConstant('{#IncludeDownloadSection}');
  IncludeDownloadSection := CompareText(IncludeDownloadSectionStr, 'true') = 0;
  if IncludeDownloadSection then
  begin

    AllFileURLs := [
    'https://snaptrude-prod-data.s3.ap-south-1.amazonaws.com/media/manager/rfas/2019.zip',
    'https://snaptrude-prod-data.s3.ap-south-1.amazonaws.com/media/manager/rfas/2020.zip',
    'https://snaptrude-prod-data.s3.ap-south-1.amazonaws.com/media/manager/rfas/2021.zip',
    'https://snaptrude-prod-data.s3.ap-south-1.amazonaws.com/media/manager/rfas/2022.zip',
    'https://snaptrude-prod-data.s3.ap-south-1.amazonaws.com/media/manager/rfas/2023.zip',
    'https://snaptrude-prod-data.s3.ap-south-1.amazonaws.com/media/manager/rfas/2024.zip',
    'https://snaptrude-prod-data.s3.ap-south-1.amazonaws.com/media/manager/rfas/2025.zip'
    ];
    AllVersions := ['2019','2020','2021','2022','2023','2024','2025'];

  CheckListBoxPage := CreateInputOptionPage(wpSelectTasks, 'Select Revit versions', 'Important: Please select the Revit versions that you are going to use with Snaptrude.'+ #13#10 +'Revit families will be downloaded for selected Revit versions.', '', False, True);
  InstalledVersions := TStringList.Create;
  InstalledVersionsURLs := TStringList.Create;
  for I := 0 to High(AllVersions) do
    begin
      if IsRevitVersionInstalled(AllVersions[I]) then
      begin
          InstalledVersions.Add(AllVersions[I]);
          InstalledVersionsURLs.Add(AllFileURLs[I]);
          CheckListBoxPage.Add('Revit ' + AllVersions[I]);
      end
    end;
  DownloadPage := CreateDownloadPage('Downloading Snaptrude default Revit families', 'Please wait while the setup downloads the required files. This can take a little while.', @OnDownloadProgress);
  DownloadPage.Msg1Label.Top := 20
  DownloadPage.Msg2Label.Top := -400
  DownloadPage.Msg2Label.Visible := False;
  end
end;

procedure unzip(ZipFile, TargetFldr: PAnsiChar);
var
  shellobj: variant;
  ZipFileV, TargetFldrV: variant;
  SrcFldr, DestFldr: variant;
  shellfldritems: variant;
  noUIOptions: Integer;
begin
  if FileExists(ZipFile) then begin
    ForceDirectories(TargetFldr);
    shellobj := CreateOleObject('Shell.Application');
    ZipFileV := string(ZipFile);
    TargetFldrV := string(TargetFldr);
    SrcFldr := shellobj.NameSpace(ZipFileV);
    DestFldr := shellobj.NameSpace(TargetFldrV);
    shellfldritems := SrcFldr.Items;
    noUIOptions := 20;
    DestFldr.CopyHere(shellfldritems, noUIOptions);  
  end;
end;

procedure ExtractMe(src, target : AnsiString);
begin
  unzip(ExpandConstant(src), ExpandConstant(target));
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpFinished then
  begin
    WizardForm.FinishedLabel.Caption := 'Setup has finished installing Snaptrude Manager v{#MyAppVersion} on your computer.' + #13#10 + #13#10 + 'Click Finish to exit the setup.';
  end;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  I: Integer;
  CheckedCount: Integer;
  FileToDownload, TargetZip, ExtractFolder: string;
  FilePath: String;
  ExtractFolderPath: String;
begin
  Result := True;
  if IncludeDownloadSection then
  begin
    if CurPageID = wpWelcome then
    begin
      while IsRevitOrSnaptrudeManagerRunning do
      begin
        case MsgBox('To proceed, please ensure that all instances of Revit and Snaptrude Manager are closed.', mbCriticalError, MB_RETRYCANCEL) of
          IDRETRY:
            if not IsRevitOrSnaptrudeManagerRunning then
              Break;
          IDCANCEL:
            begin
              Result := False;
              Exit;
            end;
        end;
      end;
    end;
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
        DownloadPage.Show;
        ClearRFAFolders;
        try
          for I := 0 to InstalledVersions.Count - 1 do
            if CheckListBoxPage.Values[I] then
            begin
              DownloadPage.SetText('Downloading Revit ' + InstalledVersions[I] + ' RFAs...', '');
              WizardForm.Update;
              FileToDownload := InstalledVersionsURLs[I];
              TargetZip := InstalledVersions[I] + '.zip';
              DownloadPage.Clear;
              DownloadPage.Add(FileToDownload, TargetZip, '');
              try
                  FilePath := ExpandConstant('{tmp}\' + InstalledVersions[I] + '.zip');
                  if FileExists(FilePath) then
                  begin
                    Log('File already exists: ' + FilePath);
                  end
                  else
                  begin
                    DownloadPage.Download;
                    DownloadPage.ProgressBar.Style := npbstMarquee;
                    DownloadPage.SetProgress(90, 100);
                  end;
                  
                  ExtractFolderPath := ExpandConstant('{tmp}\' + InstalledVersions[I] + '\');
                  if DirExists(ExtractFolderPath) then
                  begin
                    Log('Extraction folder already exists: ' + ExtractFolderPath);
                  end
                  else
                  begin
                    DownloadPage.SetText('Extracting ' + InstalledVersions[I] + ' RFAs...', '');
                    WizardForm.Update;
                    ExtractMe('{tmp}\' + InstalledVersions[I] + '.zip', ExtractFolderPath);
                  end;
              except
                if DownloadPage.AbortedByUser then
                begin
                  Log('Aborted by user.');
                  Result := False;
                  Exit;
                end
                else if GetExceptionMessage = 'Error sending data: (12007) The server name or address could not be resolved' then
                begin
                  Log('No internet connection');
                  MsgBox('Error: It seems like you are not connected to the internet. Please check your network settings and try again.', mbCriticalError, MB_OK);
                end
                else if GetExceptionMessage = 'Error reading data: (12030) The connection with the server was terminated abnormally' then
                begin
                  Log('No internet connection');
                  MsgBox('Error: Lost Internet Connection. Please check your network settings and try again.', mbCriticalError, MB_OK);
                end
                else if GetExceptionMessage = 'Error reading data: (12002) The operation timed out' then
                begin
                  Log('No internet connection');
                  MsgBox('Error: Lost Internet Connection. Please check your network settings and try again.', mbCriticalError, MB_OK);
                end
                else if GetExceptionMessage = 'Stream write error' then
                begin
                  Log('No internet connection');
                  MsgBox('Error: Not enough disk space. Please clean some space to install Snaptrude Manager.', mbCriticalError, MB_OK);
                end
                else
                begin
                  Log('An unknown error occurred.');
                  SuppressibleMsgBox('An unknown error occurred during the download: ' + AddPeriod(GetExceptionMessage), mbCriticalError, MB_OK, IDOK);
                end;
                Result := False;
              end;
            end;
        finally
          DownloadPage.Hide;
        end;
      end;
    end;
  end;
end;



var
  UninstallCheckBoxPage: TNewNotebookPage;
  UninstallNextButton: TNewButton;

procedure UpdateUninstallWizard;
begin
  if UninstallProgressForm.InnerNotebook.ActivePage = UninstallCheckBoxPage then
  begin
    UninstallProgressForm.PageNameLabel.Caption := 'We�re sorry to see you go.';
    UninstallProgressForm.PageDescriptionLabel.Caption :=
      'Thank you for being a valuable part of our community.';
  end;
  UninstallNextButton.Caption := 'Uninstall';
  UninstallNextButton.ModalResult := mrOk;
end;  

procedure UninstallNextButtonClick(Sender: TObject);
begin
  UpdateUninstallWizard;
  UninstallNextButton.Visible := False;
end;

var 
  RemoveUserDataCheckBox: TNewCheckBox;

procedure InitializeUninstallProgressForm();
var
  PageText: TNewStaticText;
  PageNameLabel: string;
  PageDescriptionLabel: string;
  CancelButtonEnabled: Boolean;
  CancelButtonModalResult: Integer;
  
begin
  PageNameLabel := UninstallProgressForm.PageNameLabel.Caption;
  PageDescriptionLabel := UninstallProgressForm.PageDescriptionLabel.Caption;
  UninstallCheckBoxPage := TNewNotebookPage.Create(UninstallProgressForm);
  UninstallCheckBoxPage.Notebook := UninstallProgressForm.InnerNotebook;
  UninstallCheckBoxPage.Parent := UninstallProgressForm.InnerNotebook;
  UninstallCheckBoxPage.Align := alClient;
  UninstallCheckBoxPage.Color := clWindow;
  RemoveUserDataCheckBox := TNewCheckBox.Create(UninstallProgressForm);
  with RemoveUserDataCheckBox do
  begin
      Parent := UninstallCheckBoxPage;
      Left := UninstallProgressForm.StatusLabel.Left;
      Top := UninstallProgressForm.StatusLabel.Top;
      Width := UninstallProgressForm.StatusLabel.Width;
      Height := ScaleY(30);
      Caption := 'Delete user preferences and logs (NOT RECOMENDED)';
  end;
  UninstallNextButton := TNewButton.Create(UninstallProgressForm);
  UninstallNextButton.Parent := UninstallProgressForm;
  UninstallNextButton.Left :=
      UninstallProgressForm.CancelButton.Left -
      UninstallProgressForm.CancelButton.Width -
      ScaleX(10);
  UninstallNextButton.Top := UninstallProgressForm.CancelButton.Top;
  UninstallNextButton.Width := UninstallProgressForm.CancelButton.Width;
  UninstallNextButton.Height := UninstallProgressForm.CancelButton.Height;
  UninstallNextButton.OnClick := @UninstallNextButtonClick;
  UninstallProgressForm.InnerNotebook.ActivePage := UninstallCheckBoxPage;
  UpdateUninstallWizard;
  CancelButtonEnabled := UninstallProgressForm.CancelButton.Enabled
  UninstallProgressForm.CancelButton.Enabled := True;
  CancelButtonModalResult := UninstallProgressForm.CancelButton.ModalResult;
  UninstallProgressForm.CancelButton.ModalResult := mrCancel;

  if UninstallProgressForm.ShowModal = mrCancel then Abort;
  UninstallProgressForm.CancelButton.Enabled := CancelButtonEnabled;
  UninstallProgressForm.CancelButton.ModalResult := CancelButtonModalResult;
  UninstallProgressForm.PageNameLabel.Caption := PageNameLabel;
  UninstallProgressForm.PageDescriptionLabel.Caption := PageDescriptionLabel;
  UninstallProgressForm.InnerNotebook.ActivePage :=
    UninstallProgressForm.InstallingPage;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  case CurUninstallStep of
    usUninstall:
      begin
        while IsRevitOrSnaptrudeManagerRunning do
        begin
          case MsgBox('To proceed, please ensure that all instances of Revit and Snaptrude Manager are closed.', mbCriticalError, MB_RETRYCANCEL) of
            IDRETRY:
              begin
                if not IsRevitOrSnaptrudeManagerRunning then
                  Break;
              end;
            IDCANCEL:Abort;
          end;
        end;
        if RemoveUserDataCheckBox.Checked then
        begin
          if DirExists(ExpandConstant('{userappdata}\snaptrude-manager')) then
          begin
            DelTree(ExpandConstant('{userappdata}\snaptrude-manager'), True, True, True);
          end;
          if DirExists(ExpandConstant('{userappdata}\SnaptrudeManager')) then
          begin
            DelTree(ExpandConstant('{userappdata}\SnaptrudeManager'), True, True, True)
          end;
        end;
      end;
  end;
end;

[InstallDelete]
Type: files; Name: "{autoappdata}\Autodesk\Revit\Addins\2019\Revit2Snaptrude\Revit2Snaptrude.dll"
Type: files; Name: "{autoappdata}\Autodesk\Revit\Addins\2020\Revit2Snaptrude\Revit2Snaptrude.dll"; 
Type: files; Name: "{autoappdata}\Autodesk\Revit\Addins\2021\Revit2Snaptrude\Revit2Snaptrude.dll"; 
Type: files; Name: "{autoappdata}\Autodesk\Revit\Addins\2022\Revit2Snaptrude\Revit2Snaptrude.dll"; 
Type: files; Name: "{autoappdata}\Autodesk\Revit\Addins\2023\Revit2Snaptrude\Revit2Snaptrude.dll"; 

Type: files; Name: "{autoappdata}\Autodesk\Revit\Addins\2019\Revit2Snaptrude.addin"
Type: files; Name: "{autoappdata}\Autodesk\Revit\Addins\2020\Revit2Snaptrude.addin"; 
Type: files; Name: "{autoappdata}\Autodesk\Revit\Addins\2021\Revit2Snaptrude.addin"; 
Type: files; Name: "{autoappdata}\Autodesk\Revit\Addins\2022\Revit2Snaptrude.addin"; 
Type: files; Name: "{autoappdata}\Autodesk\Revit\Addins\2023\Revit2Snaptrude.addin"; 
Type: filesandordirs; Name: "{commonappdata}\Snaptrude";
Type: filesandordirs; Name: "{localappdata}\snaptrude_manager";
Type: filesandordirs; Name: "{localappdata}\snaptrude-manager";
Type: filesandordirs; Name: "{commonappdata}\snaptrude-manager";

[UninstallDelete]
Type: filesandordirs; Name: "{commonappdata}\Snaptrude";
Type: filesandordirs; Name: "{userappdata}\snaptrude_manager";
Type: filesandordirs; Name: "{commonappdata}\snaptrudeTemp";
Type: filesandordirs; Name: "{commonappdata}\snaptrude-manager";
Type: filesandordirs; Name: "{userappdata}\SnaptrudeManager\resourceFile";

[Files]
Source: "{#UrlPath}"; DestDir: "{commonappdata}\SnaptrudeManager"; DestName: "urls.json"; Flags: ignoreversion;
Source: "{#UIBuildPath}\SnaptrudeManagerUI.exe"; DestDir: "{commonappdata}\SnaptrudeManager\UI"; Flags: ignoreversion;
Source: "{#UIBuildPath}\*.dll"; DestDir: "{commonappdata}\SnaptrudeManager\UI"; Flags: ignoreversion;
Source: "{#BaseRevitAddinFiles}\SnaptrudeTemplate.rte"; DestDir: "{userappdata}\SnaptrudeManager\resourceFile"; Flags: ignoreversion;


;2019
Source: "{#RevitAddinDllPath}\2019\*.dll"; DestDir: "{commonappdata}\Autodesk\Revit\Addins\2019\SnaptrudeManagerAddin"; Flags: ignoreversion; Check: InstallVersion('2019', false);
Source: "{#BaseRevitAddinFiles}\SnaptrudeManagerAddin.addin"; DestDir: "{commonappdata}\Autodesk\Revit\Addins\2019"; Flags: ignoreversion; Check: InstallVersion('2019', false);
Source: "{tmp}\2019\*"; DestDir: "{userappdata}\SnaptrudeManager\resourceFile"; Flags: external  recursesubdirs; Check: InstallVersion('2019', true);
;2020
Source: "{#RevitAddinDllPath}\2020\*.dll"; DestDir: "{commonappdata}\Autodesk\Revit\Addins\2020\SnaptrudeManagerAddin"; Flags: ignoreversion; Check: InstallVersion('2020', false);
Source: "{#BaseRevitAddinFiles}\SnaptrudeManagerAddin.addin"; DestDir: "{commonappdata}\Autodesk\Revit\Addins\2020"; Flags: ignoreversion; Check: InstallVersion('2020', false);
Source: "{tmp}\2020\*"; DestDir: "{userappdata}\SnaptrudeManager\resourceFile"; Flags: external  recursesubdirs; Check: InstallVersion('2020', true);
;2021
Source: "{#RevitAddinDllPath}\2021\*.dll"; DestDir: "{commonappdata}\Autodesk\Revit\Addins\2021\SnaptrudeManagerAddin"; Flags: ignoreversion; Check: InstallVersion('2021', false);
Source: "{#BaseRevitAddinFiles}\SnaptrudeManagerAddin.addin"; DestDir: "{commonappdata}\Autodesk\Revit\Addins\2021"; Flags: ignoreversion; Check: InstallVersion('2021', false);
Source: "{tmp}\2021\*"; DestDir: "{userappdata}\SnaptrudeManager\resourceFile"; Flags: external  recursesubdirs; Check: InstallVersion('2021', true);
;2022
Source: "{#RevitAddinDllPath}\2022\*.dll"; DestDir: "{commonappdata}\Autodesk\Revit\Addins\2022\SnaptrudeManagerAddin"; Flags: ignoreversion; Check: InstallVersion('2022', false);
Source: "{#BaseRevitAddinFiles}\SnaptrudeManagerAddin.addin"; DestDir: "{commonappdata}\Autodesk\Revit\Addins\2022"; Flags: ignoreversion; Check: InstallVersion('2022', false);
Source: "{tmp}\2022\*"; DestDir: "{userappdata}\SnaptrudeManager\resourceFile"; Flags: external  recursesubdirs; Check: InstallVersion('2022', true);
;2023
Source: "{#RevitAddinDllPath}\2023\*.dll"; DestDir: "{commonappdata}\Autodesk\Revit\Addins\2023\SnaptrudeManagerAddin"; Flags: ignoreversion; Check: InstallVersion('2023', false);
Source: "{#BaseRevitAddinFiles}\SnaptrudeManagerAddin.addin"; DestDir: "{commonappdata}\Autodesk\Revit\Addins\2023"; Flags: ignoreversion; Check: InstallVersion('2023', false);
Source: "{tmp}\2023\*"; DestDir: "{userappdata}\SnaptrudeManager\resourceFile"; Flags: external  recursesubdirs; Check: InstallVersion('2023', true);
;2024
Source: "{#RevitAddinDllPath}\2024\*.dll"; DestDir: "{commonappdata}\Autodesk\Revit\Addins\2024\SnaptrudeManagerAddin"; Flags: ignoreversion; Check: InstallVersion('2024', false);
Source: "{#BaseRevitAddinFiles}\SnaptrudeManagerAddin.addin"; DestDir: "{commonappdata}\Autodesk\Revit\Addins\2024"; Flags: ignoreversion; Check: InstallVersion('2024', false);
Source: "{tmp}\2024\*"; DestDir: "{userappdata}\SnaptrudeManager\resourceFile"; Flags: external  recursesubdirs; Check: InstallVersion('2024', true);
;2025
Source: "{#RevitAddinDllPath}\2025\*.dll"; DestDir: "{commonappdata}\Autodesk\Revit\Addins\2025\SnaptrudeManagerAddin"; Flags: ignoreversion; Check: InstallVersion('2025', false);
Source: "{#BaseRevitAddinFiles}\SnaptrudeManagerAddin.addin"; DestDir: "{commonappdata}\Autodesk\Revit\Addins\2025"; Flags: ignoreversion; Check: InstallVersion('2025', false);
Source: "{tmp}\2025\*"; DestDir: "{userappdata}\SnaptrudeManager\resourceFile"; Flags: external  recursesubdirs; Check: InstallVersion('2025', true);

; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Registry]
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocExt}\OpenWithProgids"; ValueType: string; ValueName: "{#MyAppAssocKey}"; ValueData: ""; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}"; ValueType: string; ValueName: ""; ValueData: "{#MyAppAssocName}"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""
Root: HKA; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".myp"; ValueData: ""

#if BuildName = "Update"
[Run]
Filename: "{param:ExecutablePath}"; Flags: nowait; Check: IsExecutablePathProvided
#endif