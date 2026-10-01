# FastDelete - build & publish & Inno installer (auto version bump)
# Usage:
#   .\build.ps1                 auto bump last part (1.0.1 -> 1.0.2) + publish + ISCC
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

### sync csproj (保留 CRLF：ReadAllText/WriteAllText) ###
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$c = [System.IO.File]::ReadAllText($csproj)
$c = $c -replace "<AssemblyVersion>.*?</AssemblyVersion>", ('<AssemblyVersion>' + $full + '</AssemblyVersion>')
$c = $c -replace "<FileVersion>.*?</FileVersion>", ('<FileVersion>' + $full + '</FileVersion>')
$c = $c -replace "<InformationalVersion>.*?</InformationalVersion>", ('<InformationalVersion>' + $v + '</InformationalVersion>')
[System.IO.File]::WriteAllText($csproj, $c, $utf8NoBom)
Write-Host ("[sync] csproj -> " + $full + " / " + $v)

### sync iss (#define MyAppVersion 单行，保留 CRLF) ###
$ic = [System.IO.File]::ReadAllText($iss)
$ic = $ic -replace "(?m)^#define MyAppVersion .*?$", ("#define MyAppVersion "" + $v + """)
[System.IO.File]::WriteAllText($iss, $ic, $utf8NoBom)
Write-Host ("[sync] fastdelete.iss MyAppVersion -> " + $v)

Write-Host "[publish] dotnet publish ..."
& $dotnet publish $csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -v m -nologo
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed (exit " + $LASTEXITCODE + ")" }

Write-Host "[Inno] compiling installer ..."
& $iscc $iss | Out-Null
if ($LASTEXITCODE -ne 0) { throw "ISCC failed (exit " + $LASTEXITCODE + ")" }

$setup = Join-Path $root "installer\Output\FastDelete_Setup.exe"
Write-Host ("[done] " + $setup + "  (v" + $v + ")")

if ($Install) {
    Write-Host "[install] silent install ..."
    taskkill /F /IM FastDelete.exe 2>$null
    & $setup /SILENT /SUPPRESSMSGBOXES /NORESTART /TASKS=rightclick
    if ($LASTEXITCODE -ne 0) { throw "install failed (exit " + $LASTEXITCODE + ")" }
    taskkill /F /IM FastDelete.exe 2>$null
    Write-Host "[install] done"
}