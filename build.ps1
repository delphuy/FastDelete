# FastDelete - build & publish & Inno installer (auto version bump)
# Usage:
#   .\build.ps1                 auto bump last part + publish + portable + ISCC
#   .\build.ps1 -Install        same + silent install
#   .\build.ps1 -Version 1.2.0  manually set version (no auto bump)
#   .\build.ps1 -SkipBump       keep current version (no bump)
param(
    [string]$Version = "",
    [switch]$Install,
    [switch]$SkipBump
)
$ErrorActionPreference = "Stop"

$root   = $PSScriptRoot
$src     = Join-Path $root "src\FastDelete"
$csproj   = Join-Path $src  "FastDelete.csproj"
$iss      = Join-Path $root "installer\fastdelete.iss"
$vfile    = Join-Path $root "version.txt"
$iscc     = "C:\Users\youdh\AppData\Local\Programs\Inno Setup 6\ISCC.exe"
$dotnet   = "C:\Program Files\dotnet\dotnet.exe"

[System.Environment]::SetEnvironmentVariable("USERPROFILE","C:\Users\youdh")
[System.Environment]::SetEnvironmentVariable("HOME","C:\Users\youdh")
[System.Environment]::SetEnvironmentVariable("HOMEDRIVE","C:")
[System.Environment]::SetEnvironmentVariable("HOMEPATH","\Users\youdh")
[System.Environment]::SetEnvironmentVariable("APPDATA","C:\Users\youdh\AppData\Roaming")
[System.Environment]::SetEnvironmentVariable("LOCALAPPDATA","C:\Users\youdh\AppData\Local")
[System.Environment]::SetEnvironmentVariable("NUGET_PACKAGES","C:\Users\youdh\.nuget\packages")
[System.Environment]::SetEnvironmentVariable("TEMP","C:\Users\youdh\AppData\Local\Temp")
[System.Environment]::SetEnvironmentVariable("SystemRoot","C:\WINDOWS")
[System.Environment]::SetEnvironmentVariable("ComSpec","C:\WINDOWS\system32\cmd.exe")
[System.Environment]::SetEnvironmentVariable("OS","Windows_NT")

if ($Version -ne "") { $v = $Version.Trim() }
elseif ($SkipBump) { $v = (Get-Content $vfile | Select-Object -First 1).Trim() }
else {
    $cur   = (Get-Content $vfile | Select-Object -First 1).Trim()
    $parts = $cur.Split(".")
    $parts[$parts.Length-1] = ([int]$parts[$parts.Length-1] + 1).ToString()
    $v = $parts -join "."
    Set-Content -Path $vfile -Value $v -NoNewline
    Write-Host ("[bump] version: " + $cur + " -> " + $v)
}
$full = $v + ".0"
Write-Host ("[version] build " + $v + "  (assembly " + $full + ")")

### 发布产物命名：体现 安装版/便携版 + Windows + x64 + 版本 ###
$instName = "FastDelete-" + $v + "-Windows-x64-Installer.exe"
$portName = "FastDelete-" + $v + "-Windows-x64-Portable.exe"

### sync csproj (CRLF) ###
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$c = [System.IO.File]::ReadAllText($csproj)
$c = $c -replace "<AssemblyVersion>.*?</AssemblyVersion>", ('<AssemblyVersion>' + $full + '</AssemblyVersion>')
$c = $c -replace "<FileVersion>.*?</FileVersion>", ('<FileVersion>' + $full + '</FileVersion>')
$c = $c -replace "<InformationalVersion>.*?</InformationalVersion>", ('<InformationalVersion>' + $v + '</InformationalVersion>')
[System.IO.File]::WriteAllText($csproj, $c, $utf8NoBom)
Write-Host ("[sync] csproj -> " + $full + " / " + $v)

### sync iss (MyAppVersion，CRLF) ###
$ic = [System.IO.File]::ReadAllText($iss)
$ic = $ic -replace "(?m)^#define MyAppVersion .*?$", ("#define MyAppVersion "" + $v + """)
[System.IO.File]::WriteAllText($iss, $ic, $utf8NoBom)
Write-Host ("[sync] fastdelete.iss MyAppVersion -> " + $v)

### 1) publish 框架依赖（Inno 要打包的产物，必须先于 ISCC）###
Write-Host "[publish] dotnet publish (framework-dependent) ..."
& $dotnet publish $csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -v m -nologo
if ($LASTEXITCODE -ne 0) { throw "publish failed (exit " + $LASTEXITCODE + ")" }

### 2) 安装版 ISCC 编译 + 重命名 ###
Write-Host "[installer] ISCC ..."
& $iscc $iss | Out-Null
if ($LASTEXITCODE -ne 0) { throw "ISCC failed (exit " + $LASTEXITCODE + ")" }
$setup = Join-Path $root ("installer\Output\" + $instName)
if (Test-Path (Join-Path $root "installer\Output\FastDelete_Setup.exe")) {
    Move-Item -Force (Join-Path $root "installer\Output\FastDelete_Setup.exe") $setup
}
Write-Host ("[installer] " + $setup)

### 3) 便携版（框架依赖单文件，输出到 dist\portable，重命名）###
$portOut = Join-Path $root "dist\portable"
Write-Host "[portable] dotnet publish (portable) ..."
& $dotnet publish $csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:DebugType=none -o $portOut -v m -nologo
if ($LASTEXITCODE -ne 0) { throw "portable publish failed (exit " + $LASTEXITCODE + ")" }
# 清旧 exe / pdb，只留新命名的
if (Test-Path $portOut) {
    Get-ChildItem $portOut -File | Where-Object { $_.Name -like "FastDelete*.exe" -and $_.Name -ne $portName } | Remove-Item -Force
    Get-ChildItem $portOut -File -Filter *.pdb | Remove-Item -Force -ErrorAction SilentlyContinue
}
Move-Item -Force (Join-Path $portOut "FastDelete.exe") (Join-Path $portOut $portName)
Write-Host ("[portable] " + (Join-Path $portOut $portName))

Write-Host ""
Write-Host ("[done] v" + $v + ":", [System.Environment]::NewLine + "   安装版  " + $setup + [System.Environment]::NewLine + "   便携版  " + (Join-Path $portOut $portName))

if ($Install) {
    Write-Host "[install] silent install ..."
    taskkill /F /IM FastDelete.exe 2>$null
    & $setup /SILENT /SUPPRESSMSGBOXES /NORESTART /TASKS=rightclick
    if ($LASTEXITCODE -ne 0) { throw "install failed (exit " + $LASTEXITCODE + ")" }
    taskkill /F /IM FastDelete.exe 2>$null
    Write-Host "[install] done"
}