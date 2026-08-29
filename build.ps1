# Builds HkmpDynamicAggro.dll against the game's own assemblies.
# Uses the .NET Framework compiler shipped with Windows, so no SDK install is needed.

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "FindManaged.ps1")
$Managed = Get-ManagedFolder
$Csc     = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$OutDir  = Join-Path $PSScriptRoot "build"
$Out     = Join-Path $OutDir "HkmpDynamicAggro.dll"

if (-not (Test-Path $OutDir))  { New-Item -ItemType Directory $OutDir | Out-Null }

$refs = @(
    "mscorlib.dll"
    "System.dll"
    "System.Core.dll"
    "netstandard.dll"
    "UnityEngine.dll"
    "UnityEngine.CoreModule.dll"
    "UnityEngine.Physics2DModule.dll"
    "PlayMaker.dll"
    "Assembly-CSharp.dll"
    "MMHOOK_Assembly-CSharp.dll"
    "MonoMod.RuntimeDetour.dll"
    "MonoMod.Utils.dll"
    "Mods\HKMP\HKMP.dll"
) | ForEach-Object { "/r:`"$Managed\$_`"" }

$sources = Get-ChildItem (Join-Path $PSScriptRoot "src") -Filter *.cs | ForEach-Object { "`"$($_.FullName)`"" }

$argList = @("/target:library", "/nostdlib+", "/noconfig", "/optimize+", "/out:`"$Out`"") + $refs + $sources

& $Csc $argList
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

Write-Output "Built $Out"
Write-Output "  against $Managed"
