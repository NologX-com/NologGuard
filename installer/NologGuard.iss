#ifndef AppVersion
  #define AppVersion "1.2.0"
#endif

#ifndef AppExePath
  #define AppExePath "..\bin\Release\NologGuard.exe"
#endif

[Setup]
AppId={{D9C6A4E1-5B3F-4E8A-9C12-7F4B8D3A1205}
AppName=NologGuard
AppVerName=NologGuard {#AppVersion}
AppPublisher=NologX.com
AppPublisherURL=https://nologx.com
AppSupportURL=https://github.com/NologX-com/NologGuard/issues
AppUpdatesURL=https://github.com/NologX-com/NologGuard/releases
VersionInfoVersion={#AppVersion}
VersionInfoCompany=NologX.com
VersionInfoDescription=1-Click Windows Privacy Shield (DoH + SNI Fragmentation + AdBlock)
VersionInfoCopyright=Copyright (c) 2026 NologX.com
DefaultDirName={autopf}\NologGuard
DefaultGroupName=NologGuard
AllowNoIcons=yes
LicenseFile=..\LICENSE
SetupIconFile=..\src\NologGuard.ico
UninstallDisplayIcon={app}\NologGuard.exe
UninstallDisplayName=NologGuard
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
CloseApplicationsFilter=*.exe
RestartApplications=no
OutputDir=..\dist
OutputBaseFilename=NologGuard-Setup

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#AppExePath}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.vi.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\PRIVACY.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\NologGuard"; Filename: "{app}\NologGuard.exe"
Name: "{group}\Reset Proxy & Firewall (Recovery)"; Filename: "{app}\NologGuard.exe"; Parameters: "--recover"
Name: "{group}\{cm:UninstallProgram,NologGuard}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\NologGuard"; Filename: "{app}\NologGuard.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\NologGuard.exe"; Description: "{cm:LaunchProgram,NologGuard}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{app}\NologGuard.exe"; Parameters: "--exit"; Flags: runhidden waituntilterminated; RunOnceId: "StopNologGuard"
Filename: "{app}\NologGuard.exe"; Parameters: "--recover"; Flags: runhidden waituntilterminated; RunOnceId: "RecoverNologGuard"

[Code]
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ExeFile: String;
  ResultCode: Integer;
begin
  Result := '';
  ExeFile := ExpandConstant('{app}\NologGuard.exe');
  if FileExists(ExeFile) then
  begin
    Exec(ExeFile, '--exit', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  end;
end;
