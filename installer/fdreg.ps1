# FastDelete install-time registration (called by Inno installer)
$app   = "C:\Program Files\FastDelete"
$clsid = "{6F5E2C9D-8A1B-4C3D-9E7F-2A1B3C4D5E6F}"
$dll   = Join-Path $app "FastDelete.Shell.dll"
$exe   = Join-Path $app "FastDelete.exe"
$q     = [char]34

# COM in-proc server
New-Item -Path "HKLM:\SOFTWARE\Classes\CLSID\$clsid" -Force | Out-Null
New-Item -Path "HKLM:\SOFTWARE\Classes\CLSID\$clsid\InprocServer32" -Force | Out-Null
Set-ItemProperty "HKLM:\SOFTWARE\Classes\CLSID\$clsid" -Name "(Default)" -Value "FastDelete Shell Extension"
Set-ItemProperty "HKLM:\SOFTWARE\Classes\CLSID\$clsid\InprocServer32" -Name "(Default)" -Value $dll
Set-ItemProperty "HKLM:\SOFTWARE\Classes\CLSID\$clsid\InprocServer32" -Name "ThreadingModel" -Value "Both"

# Win11 first-screen: IExplorerCommand menu keys
foreach ($base in @("Directory", "Directory\Background")) {
    $menuKey = "HKLM:\SOFTWARE\Classes\$base\shell\fastdelete"
    New-Item -Path $menuKey -Force | Out-Null
    Set-ItemProperty $menuKey -Name "(Default)" -Value "fastdelete - 闪电删除"
    Set-ItemProperty $menuKey -Name "Icon" -Value "$exe,0"
    New-ItemProperty $menuKey -Name "NoWorkingDirectory" -Value 0 -PropertyType DWord -Force | Out-Null
    New-Item -Path "$menuKey\command" -Force | Out-Null
    Set-ItemProperty "$menuKey\command" -Name "(Default)" -Value ($q + $exe + $q + " add " + $q + "%1" + $q)
    Set-ItemProperty $menuKey -Name "InprocServer32" -Value $clsid
}

# Sparse package identity
$manifest = Join-Path $app "AppxManifest.xml"
if (Test-Path $manifest) {
    try {
        Add-AppxPackage -Register $manifest -ErrorAction Stop
        Write-Host "稀疏标识注册成功"
    } catch {
        Write-Warning ("稀疏标识注册失败: " + $_.Exception.Message)
    }
} else {
    Write-Warning ("manifest 缺失: " + $manifest)
}
exit 0
