# FastDelete install-time registration (called by Inno installer)
# NOTE: keep pure ASCII. Windows PowerShell 5.1 reads .ps1 as ANSI codepage;
#       Chinese UTF-8 literals/comments can corrupt the parser. Chinese menu
#       name is emitted via char codes so the display name stays correct.
$app   = "C:\Program Files\FastDelete"
$clsid = "{6F5E2C9D-8A1B-4C3D-9E7F-2A1B3C4D5E6F}"
$exe   = "C:\Program Files\FastDelete\FastDelete.exe"
$dll   = "C:\Program Files\FastDelete\FastDelete.Shell.dll"
$q     = [char]34

# Menu display name: "Fast Delete - 极速删除"
# Chinese chars emitted via char codes: 0x6781 0x901F 0x5220 0x9664
$menuName = "Fast Delete - " + [string]::new([char[]]@(0x6781,0x901F,0x5220,0x9664))

# COM in-proc server. The path is a literal single-backslash string (no
# Join-Path, no TrimEnd) so the stored value reads exactly:
#   C:\Program Files\FastDelete\FastDelete.Shell.dll
# (Earlier builds stored a trailing double backslash via Join-Path on a
#  trailing-separator dir; reg query output confirmed the bad form.)
New-Item -Path "HKLM:\SOFTWARE\Classes\CLSID\$clsid" -Force | Out-Null
New-Item -Path "HKLM:\SOFTWARE\Classes\CLSID\$clsid\InprocServer32" -Force | Out-Null
Set-ItemProperty "HKLM:\SOFTWARE\Classes\CLSID\$clsid" -Name "(Default)" -Value "FastDelete Shell Extension"
Set-ItemProperty "HKLM:\SOFTWARE\Classes\CLSID\$clsid\InprocServer32" -Name "(Default)" -Value $dll
Set-ItemProperty "HKLM:\SOFTWARE\Classes\CLSID\$clsid\InprocServer32" -Name "ThreadingModel" -Value "Both"

# Legacy right-click menu keys (Win10 direct / Win11 Show more options)
foreach ($base in @("Directory", "Directory\Background")) {
    $menuKey = "HKLM:\SOFTWARE\Classes\$base\shell\fastdelete"
    New-Item -Path $menuKey -Force | Out-Null
    Set-ItemProperty $menuKey -Name "(Default)" -Value $menuName
    Set-ItemProperty $menuKey -Name "Icon" -Value "$exe,0"
    New-ItemProperty $menuKey -Name "NoWorkingDirectory" -Value 0 -PropertyType DWord -Force | Out-Null
    New-Item -Path "$menuKey\command" -Force | Out-Null
    $cmdVal = $q + $exe + $q + ' add ' + $q + '%1' + $q
    Set-ItemProperty "$menuKey\command" -Name "(Default)" -Value $cmdVal
    # Register IExplorerCommand only when the DLL actually exists; otherwise
    # strip any stale InprocServer32 reference so Explorer falls back to the
    # legacy command line instead of failing to load a missing COM server.
    if (Test-Path $dll) {
        Set-ItemProperty $menuKey -Name "InprocServer32" -Value $clsid
    } else {
        Remove-ItemProperty $menuKey -Name "InprocServer32" -ErrorAction SilentlyContinue
        Write-Warning ("FastDelete.Shell.dll missing; IExplorerCommand registration skipped: " + $dll)
    }
}

# Clean up any stale / malformed CLSID entries left by earlier builds
# (e.g. the double-backslash InprocServer32 path that broke COM loading).
# Self-heal in-place: rewrite the value to the clean literal path, but leave
# the key structure intact so a subsequent uninstall can still remove it.
$inproc   = "HKLM:\SOFTWARE\Classes\CLSID\$clsid\InprocServer32"
if (Test-Path $inproc) {
    $val = (Get-ItemProperty $inproc -Name '(Default)').PSObject.Properties['(Default)'].Value
    if ($val -and $val -ne $dll) {
        Write-Host ("Stale InprocServer32 path detected: " + $val + " -- rewriting to clean value")
        Set-ItemProperty $inproc -Name "(Default)" -Value $dll
    }
}

exit 0
