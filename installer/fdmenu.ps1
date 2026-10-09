# FastDelete right-click menu management script (LEGACY / manual variant)
# Usage: fdmenu.ps1 <install|uninstall>
#
# STATUS: NOT referenced by fastdelete.iss [Run]/[UninstallRun] -- kept in the
# repo only for out-of-band manual menu registration. The current build
# registers the menu via fdreg.ps1 (IExplorerCommand CLSID + legacy fallback)
# shipped inside {app} and run elevated by the Inno installer.
#
# NOTE: keep this file pure ASCII - Windows PowerShell 5.1 reads .ps1 as ANSI,
#       Chinese UTF-8 comments break the parser. The Chinese menu display name
#       is emitted via char codes.
#
# Verb command format (Win11 25H2 ShellExecuteW requirement):
#   "C:\Program Files\FastDelete\FastDelete.exe" add "%1"
# The first token is a QUOTED absolute exe path (no spaces inside the quoted
# token are mis-parsed by shell32 because %1 owns its own quotes), followed
# by a bare verb and a quoted %1. This matches the form Explorer accepts on
# both Win10 (direct right-click) and Win11 (Show more options).
$action = $args[0]
$exe = "C:\Program Files\FastDelete\FastDelete.exe"

function Write-MenuKey($base) {
    $key = "HKLM:\SOFTWARE\Classes\$base\shell\fastdelete"
    New-Item -Path $key -Force | Out-Null
    # Menu display name: "Fast Delete - <ji-su-shan-chu>"
    # 极=0x6781 速=0x901F 删=0x5220 除=0x9664
    $displayName = "Fast Delete - " + [string]::new([char[]]@(0x6781,0x901F,0x5220,0x9664))
    Set-ItemProperty $key -Name "(Default)" -Value $displayName
    Set-ItemProperty $key -Name "Icon" -Value "$exe,0"
    New-ItemProperty $key -Name "NoWorkingDirectory" -Value 0 -PropertyType DWord -Force | Out-Null
    New-Item -Path "$key\command" -Force | Out-Null
    $q = [char]34
    # Form:  "C:\Program Files\FastDelete\FastDelete.exe" add "%1"
    $cmdVal = $q + $exe + $q + ' add ' + $q + '%1' + $q
    Set-ItemProperty "$key\command" -Name "(Default)" -Value $cmdVal
}

function Remove-MenuKey($base) {
    Remove-Item "HKLM:\SOFTWARE\Classes\$base\shell\fastdelete" -Recurse -Force -ErrorAction SilentlyContinue
}

if ($action -eq "install") {
    Write-Host "Registering right-click menu (Win10 direct / Win11 Show more options)..."
    foreach ($base in @("Directory", "Directory\Background")) {
        Remove-Item "HKCU:\Software\Classes\$base\shell\fastdelete" -Recurse -Force -ErrorAction SilentlyContinue
    }
    Write-MenuKey "Directory"
    Write-MenuKey "Directory\Background"
    Write-Host "Done. Right-click a folder to see Fast Delete."
} elseif ($action -eq "uninstall") {
    Write-Host "Unregistering right-click menu..."
    Remove-MenuKey "Directory"
    Remove-MenuKey "Directory\Background"
    Remove-Item "HKCU:\Software\Classes\Directory\shell\fastdelete" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item "HKCU:\Software\Classes\Directory\Background\shell\fastdelete" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "Done."
} else {
    Write-Error "Usage: fdmenu.ps1 <install|uninstall>"
    exit 1
}
exit 0
