# FastDelete uninstall-time deregistration script (called by Inno uninstaller)
$clsid = "{6F5E2C9D-8A1B-4C3D-9E7F-2A1B3C4D5E6F}"
Remove-Item "HKLM:\SOFTWARE\Classes\CLSID\$clsid" -Recurse -Force -ErrorAction SilentlyContinue
foreach ($base in @("Directory", "Directory\Background")) {
    Remove-Item "HKLM:\SOFTWARE\Classes\$base\shell\fastdelete" -Recurse -Force -ErrorAction SilentlyContinue
}
Remove-AppxPackage -Package "FastDelete.Hui" -ErrorAction SilentlyContinue
Remove-Item "HKCU:\Software\FastDelete" -Recurse -Force -ErrorAction SilentlyContinue
exit 0
