; FastDelete Inno Setup 安装脚本
#define MyAppName "FastDelete"
#define MyAppVersion "1.0.17"
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
; IExplorerCommand registration script (CLSID + InprocServer32) and its
; uninstall counterpart, shipped inside the app dir so [Run]/[UninstallRun]
; can invoke them on the user's machine (the build-time installer\ dir is not
; shipped).
Source: "fdreg.ps1"; DestDir: "{app}"; Flags: ignoreversion
Source: "fdunreg.ps1"; DestDir: "{app}"; Flags: ignoreversion
[Icons]
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\FastDelete.exe"; Description: "现在运行 FastDelete"; Flags: postinstall nowait
; IExplorerCommand COM registration (CLSID + menu-key InprocServer32), only when
; the rightclick task is selected. postinstall defaults to runasoriginaluser (the
; pre-UAC token) which CANNOT write HKLM -- so explicitly add runascurrentuser
; to inherit Inno's elevated admin token. Hidden window: fdreg.ps1 sets
; -WindowStyle Hidden itself.
Filename: "powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File ""{app}\fdreg.ps1"""; Tasks: rightclick; Flags: postinstall runascurrentuser; StatusMsg: "正在注册右键菜单..."
[UninstallRun]
Filename: "taskkill"; Parameters: "/F /IM FastDelete.exe"; Flags: runhidden; RunOnceId: "killfd"
; Deregister CLSID + menu-key InprocServer32 on uninstall (mirrors fdreg.ps1).
; The CLSID GUID is written via PowerShell at install time by fdreg.ps1. On
; uninstall, the menu key's InprocServer32 value and the CLSID root key need
; explicit cleanup. We call fdunreg.ps1 (shipped in {app} via [Files]). Add
; runascurrentuser so the spawned powershell inherits the elevated admin token
; (UninstallRun defaults to the original user, which cannot write HKLM).
Filename: "powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File ""{app}\fdunreg.ps1"""; RunOnceId: "unregcom"; Flags: runhidden runascurrentuser
[Registry]
; --- legacy fallback shell verb (Win10 direct / Win11 Show more options) ---
Root: HKLM; Subkey: "SOFTWARE\Classes\Directory\shell\fastdelete"; ValueType: string; ValueName: "(Default)"; ValueData: "Fast Delete - 极速删除"; Tasks: rightclick; Flags: uninsdeletekey
Root: HKLM; Subkey: "SOFTWARE\Classes\Directory\shell\fastdelete"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\FastDelete.exe,0"; Tasks: rightclick; Flags: uninsdeletekey
Root: HKLM; Subkey: "SOFTWARE\Classes\Directory\shell\fastdelete"; ValueType: dword; ValueName: "NoWorkingDirectory"; ValueData: "0"; Tasks: rightclick; Flags: uninsdeletevalue
Root: HKLM; Subkey: "SOFTWARE\Classes\Directory\shell\fastdelete\command"; ValueType: string; ValueName: "(Default)"; ValueData: """{app}\FastDelete.exe"" add ""%1"""; Tasks: rightclick; Flags: uninsdeletekey
Root: HKLM; Subkey: "SOFTWARE\Classes\Directory\Background\shell\fastdelete"; ValueType: string; ValueName: "(Default)"; ValueData: "Fast Delete - 极速删除"; Tasks: rightclick; Flags: uninsdeletekey
Root: HKLM; Subkey: "SOFTWARE\Classes\Directory\Background\shell\fastdelete"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\FastDelete.exe,0"; Tasks: rightclick; Flags: uninsdeletekey
Root: HKLM; Subkey: "SOFTWARE\Classes\Directory\Background\shell\fastdelete"; ValueType: dword; ValueName: "NoWorkingDirectory"; ValueData: "0"; Tasks: rightclick; Flags: uninsdeletevalue
Root: HKLM; Subkey: "SOFTWARE\Classes\Directory\Background\shell\fastdelete\command"; ValueType: string; ValueName: "(Default)"; ValueData: """{app}\FastDelete.exe"" add ""%1"""; Tasks: rightclick; Flags: uninsdeletekey

; --- IExplorerCommand (Win11 first-screen, via in-proc COM DLL) ---
; CLSID + menu-key InprocServer32 are written by fdreg.ps1 (PowerShell has no
; brace/constant limitation, unlike Inno [Registry]). Kept out of [Registry]
; because Inno parses a literal CLSID GUID in Subkey/ValueData as an unknown
; constant and aborts the build.
; See the [Run] section below: fdreg.ps1 runs postinstall under the rightclick task.

[UninstallDelete]
Type: filesandordirs; Name: "{app}\log"
Type: filesandordirs; Name: "{localappdata}\FastDelete"
Type: filesandordirs; Name: "{app}"
