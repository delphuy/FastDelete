; FastDelete Inno Setup 安装脚本
#define MyAppName "FastDelete"
#define MyAppVersion "1.0.14"
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
SetupIconFile=..\src\FastDelete\app.ico
ArchitecturesInstallIn64BitMode=x64os
ArchitecturesAllowed=x64compatible
PrivilegesRequired=admin
UninstallDisplayIcon={app}\FastDelete.exe

[Languages]
Name: "chinesesimplified"; MessagesFile: "ChineseSimplified.isl"

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
Filename: "{app}\FastDelete.exe"; Description: "现在运行 FastDelete"; Flags: postinstall nowait
[UninstallRun]
Filename: "taskkill"; Parameters: "/F /IM FastDelete.exe"; Flags: runhidden; RunOnceId: "killfd"
[Registry]
Root: HKLM; Subkey: "SOFTWARE\Classes\Directory\shell\fastdelete"; ValueType: string; ValueName: "(Default)"; ValueData: "fastdelete - 闪电删除"; Tasks: rightclick; Flags: uninsdeletekey
Root: HKLM; Subkey: "SOFTWARE\Classes\Directory\shell\fastdelete"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\FastDelete.exe,0"; Tasks: rightclick; Flags: uninsdeletekey
Root: HKLM; Subkey: "SOFTWARE\Classes\Directory\shell\fastdelete\command"; ValueType: string; ValueName: "(Default)"; ValueData: """{app}\FastDelete.exe"" add ""%1"""; Tasks: rightclick; Flags: uninsdeletekey
Root: HKLM; Subkey: "SOFTWARE\Classes\Directory\Background\shell\fastdelete"; ValueType: string; ValueName: "(Default)"; ValueData: "fastdelete - 闪电删除"; Tasks: rightclick; Flags: uninsdeletekey
Root: HKLM; Subkey: "SOFTWARE\Classes\Directory\Background\shell\fastdelete"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\FastDelete.exe,0"; Tasks: rightclick; Flags: uninsdeletekey
Root: HKLM; Subkey: "SOFTWARE\Classes\Directory\Background\shell\fastdelete\command"; ValueType: string; ValueName: "(Default)"; ValueData: """{app}\FastDelete.exe"" add ""%1"""; Tasks: rightclick; Flags: uninsdeletekey

[UninstallDelete]
Type: filesandordirs; Name: "{app}\log"
Type: filesandordirs; Name: "{localappdata}\FastDelete"
Type: filesandordirs; Name: "{app}"
