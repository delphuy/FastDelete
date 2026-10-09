# FastDelete uninstall-time deregistration (called by Inno uninstaller)
# Pure ASCII - PS5.1 reads .ps1 as ANSI; Chinese comments would break parser.
$clsid = "{6F5E2C9D-8A1B-4C3D-9E7F-2A1B3C4D5E6F}"
# Remove CLSID registration entirely
Remove-Item "HKLM:\SOFTWARE\Classes\CLSID\$clsid" -Recurse -Force -ErrorAction SilentlyContinue
# Remove InprocServer32 value from shell menu keys (the whole key is removed by
# Inno [Registry] uninsdeletekey when rightclick task is not selected)
foreach ($base in @("Directory", "Directory\Background")) {
    Remove-ItemProperty "HKLM:\SOFTWARE\Classes\$base\shell\fastdelete" -Name "InprocServer32" -ErrorAction SilentlyContinue
}
exit 0
