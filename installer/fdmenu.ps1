# FastDelete right-click menu management script (called by Inno installer)
# Usage: fdmenu.ps1 <install|uninstall>
$action = $args[0]
$exe = "C:\Program Files\FastDelete\FastDelete.exe"

function Write-MenuKey($base) {
    $key = "HKLM:\SOFTWARE\Classes\$base\shell\fastdelete"
    New-Item -Path $key -Force | Out-Null
    Set-ItemProperty $key -Name "(Default)" -Value "fastdelete - Fast Delete"
    Set-ItemProperty $key -Name "Icon" -Value "$exe,0"
    New-ItemProperty $key -Name "NoWorkingDirectory" -Value 0 -PropertyType DWord -Force | Out-Null
    New-Item -Path "$key\command" -Force | Out-Null
    $q = [char]34
    $cmdVal = $q + $exe + $q + ' add ' + $q + '%1' + $q
    Set-ItemProperty "$key\command" -Name "(Default)" -Value $cmdVal
}

function Remove-MenuKey($base) {
    Remove-Item "HKLM:\SOFTWARE\Classes\$base\shell\fastdelete" -Recurse -Force -ErrorAction SilentlyContinue
}

switch ($action) {
    "install" {
        Write-Host "Registering right-click menu (legacy: Win10 direct, Win11 show-more-options)..."
        Write-MenuKey "Directory"
        Write-MenuKey "Directory\Background"
        Write-Host "Done. Right-click a folder to see fastdelete (Win11: under Show more options)."
    }
    "uninstall" {
        Write-Host "Unregistering right-click menu..."
        Remove-MenuKey "Directory"
        Remove-MenuKey "Directory\Background"
        Remove-Item "HKCU:\Software\Classes\Directory\shell\fastdelete" -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "Done."
    }
    default {
        Write-Error "Usage: fdmenu.ps1 <install|uninstall>"
        exit 1
    }
}
exit 0
