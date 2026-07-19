# Build patched RMP and install into STS2 mods folder.
$ErrorActionPreference = "Stop"
$here = $PSScriptRoot
$props = Join-Path $here "Sts2PathDiscovery.props"

# Resolve STS2 data dir (same defaults as Sts2PathDiscovery.props)
$sts2Root = $env:Sts2Root
if (-not $sts2Root) {
    foreach ($c in @(
        "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2",
        "C:\Program Files\Steam\steamapps\common\Slay the Spire 2"
    )) {
        if (Test-Path $c) { $sts2Root = $c; break }
    }
}
if (-not $sts2Root) { throw "STS2 install not found. Set Sts2Root." }

$sts2Dll = Join-Path $sts2Root "data_sts2_windows_x86_64\sts2.dll"
$modsPath = Join-Path $sts2Root "mods\RemoveMultiplayerPlayerLimit"
$original = Join-Path $here "vendor\RemoveMultiplayerPlayerLimit.original.dll"
$pck = Join-Path $here "vendor\RemoveMultiplayerPlayerLimit.pck"
$json = Join-Path $here "RemoveMultiplayerPlayerLimit.json"
$dist = Join-Path $here "dist"
$outDll = Join-Path $dist "RemoveMultiplayerPlayerLimit.dll"

if (-not (Test-Path $sts2Dll)) { throw "sts2.dll missing: $sts2Dll" }
if (-not (Test-Path $original)) { throw "vendor original missing: $original" }

New-Item -ItemType Directory -Force -Path $dist | Out-Null

Write-Host "Patching RMP against $sts2Dll"
dotnet run --project (Join-Path $here "tools\PatchRmp\PatchRmp.csproj") -c Release -- `
    $original $sts2Dll $outDll
if ($LASTEXITCODE -ne 0) { throw "PatchRmp failed ($LASTEXITCODE)" }

Write-Host "Installing to $modsPath"
New-Item -ItemType Directory -Force -Path $modsPath | Out-Null
Copy-Item $outDll $modsPath -Force
Copy-Item $pck $modsPath -Force
Copy-Item $json $modsPath -Force

Write-Host "Done. Disable the Steam Workshop RMP entry so only this local build loads."
Get-ChildItem $modsPath | Format-Table Name, Length, LastWriteTime
