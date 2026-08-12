param([string]$BepInExRoot = "")

$ErrorActionPreference = "Stop"
if (-not $BepInExRoot) {
    $BepInExRoot = Read-Host "Paste the Erenshor/r2modman/Thunderstore profile folder that contains BepInEx"
}

$core = Join-Path $BepInExRoot "BepInEx\core\BepInEx.dll"
if (-not (Test-Path $core)) {
    throw "The selected folder is not a BepInEx root: $BepInExRoot"
}

$pluginDir = Join-Path $BepInExRoot "BepInEx\plugins\ErenshorPartyTools"
if (Test-Path $pluginDir) {
    Remove-Item -LiteralPath $pluginDir -Recurse -Force
    Write-Host "Removed Erenshor Party Tools." -ForegroundColor Green
}
else {
    Write-Host "Erenshor Party Tools was not installed in this profile."
}
