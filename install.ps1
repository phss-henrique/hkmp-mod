# Copies the built mod into the Hollow Knight Mods folder.
# Uninstall = delete that folder.

$ErrorActionPreference = "Stop"

$Dll  = Join-Path $PSScriptRoot "build\HkmpDynamicAggro.dll"
. (Join-Path $PSScriptRoot "FindManaged.ps1")
$Dest = Join-Path (Get-ManagedFolder) "Mods\HkmpDynamicAggro"

if (-not (Test-Path $Dll)) { throw "Not built yet. Run build.ps1 first." }
if (-not (Test-Path $Dest)) { New-Item -ItemType Directory $Dest | Out-Null }

Copy-Item $Dll $Dest -Force
Write-Output "Installed to $Dest"
