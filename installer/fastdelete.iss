; FastDelete Inno Setup 安装脚本
#define MyAppName "FastDelete"
#define MyAppVersion "1.0.3"
#define MyAppPublisher "Hui"
#define MyAppExeName "FastDelete.exe"
#define PublishDir "..\src\FastDelete\bin\Release\net8.0-windows\win-x64\publish"

[Setup]
AppId={{B7E3C5A1-2C4D-4E5F-9A0B-1C2D3E4F5A6B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\FastDelete
DefaultGroupName={#MyAppName}
DisableDirPage=no
DisableProgramGroupPage=yes
OutputBaseFilename=FastDelete_Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64os
ArchitecturesAllowed=x64compatible
PrivilegesRequired=admin
UninstallDisplayIcon={app}\FastDelete.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "在桌面生成快捷方式"; GroupDescription: "快捷方式:"
Name: "rightclick"; Description: "注册 Windows 右键菜单 (fastdelete)"; GroupDescription: "右键菜单:"

[Dirs]
Name: "{app}\log"; Permissions: users-modify

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "log\*"
Source: "任务管理器说明.txt"; DestDir: "{app}"

[Icons]
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "运行 {#MyAppName}"; Flags: nowait postinstall

[Registry]
Root: HKCR; Subkey: "Directory\shell\fastdelete"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\FastDelete.exe,0"; Tasks: rightclick; Flags: uninsdeletevalue
Root: HKCR; Subkey: "Directory\shell\fastdelete"; ValueType: dword; ValueName: "NoWorkingDirectory"; ValueData: 0; Tasks: rightclick; Flags: uninsdeletevalue
Root: HKCR; Subkey: "Directory\shell\fastdelete"; Flags: uninsdeletekey; Tasks: rightclick
Root: HKCR; Subkey: "Directory\shell\fastdelete\command"; Flags: uninsdeletekey; Tasks: rightclick

[UninstallRun]
Filename: "taskkill"; Parameters: "/F /IM FastDelete.exe"; Flags: runhidden; RunOnceId: "killfd"

[UninstallDelete]
Type: filesandordirs; Name: "{app}\log"
Type: filesandordirs; Name: "{localappdata}\FastDelete"

[Code]
procedure SHChangeNotify(wEventId: LongInt; uFlags: LongInt; dwItem1: LongInt; dwItem2: LongInt);
external 'SHChangeNotify@shell32.dll stdcall';
const
  SHCNE_ASSOCCHANGED = $08000000;
procedure CurStepChanged(CurStep: TSetupStep);
var
  Cmd, MenuName: String;
begin
  if (CurStep = ssPostInstall) and IsTaskSelected('rightclick') then
  begin
    MenuName := 'fastdelete - ' + #$95EA + #$7535 + #$5220 + #$9664;
    RegWriteStringValue(HKLM, 'SOFTWAREClassesDirectoryshellastdelete', '', MenuName);
    Cmd := '"' + ExpandConstant('{app}') + 'FastDelete.exe" add "%1"';
    RegWriteStringValue(HKLM, 'SOFTWAREClassesDirectoryshellastdeletecommand', '', Cmd);
    SHChangeNotify(SHCNE_ASSOCCHANGED, 0, 0, 0);
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    RegDeleteKeyIncludingSubkeys(HKLM, 'SOFTWAREClassesDirectoryshellastdelete');
    RegDeleteKeyIncludingSubkeys(HKCU, 'SoftwareFastDelete');
    RegDeleteKeyIncludingSubkeys(HKCU, 'SOFTWAREClassesDirectoryshellastdelete');
    SHChangeNotify(SHCNE_ASSOCCHANGED, 0, 0, 0);
  end;
end;
