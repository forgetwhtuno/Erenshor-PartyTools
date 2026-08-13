param([string]$GameDir = "")

$ErrorActionPreference = "Stop"
if (-not $GameDir) {
    $GameDir = Read-Host "Paste the Erenshor install folder (contains Erenshor.exe)"
}

if (-not (Test-Path (Join-Path $GameDir "Erenshor.exe"))) {
    throw "The selected folder does not contain Erenshor.exe: $GameDir"
}

$dll = Join-Path $GameDir "plugins\ErenshorPartyTools.dll"
if (Test-Path $dll) {
    Remove-Item -LiteralPath $dll -Force
    Write-Host "Removed Erenshor Party Tools." -ForegroundColor Green
}
else {
    Write-Host "Erenshor Party Tools was not installed natively in this game folder."
}
