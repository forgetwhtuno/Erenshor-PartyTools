$ErrorActionPreference = "Stop"
$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

function Find-Csc {
    foreach ($path in @(
        "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
        "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
    )) {
        if (Test-Path $path) { return $path }
    }
    throw "csc.exe not found. Install the .NET Framework Developer Pack or Visual Studio Build Tools."
}

$csc = Find-Csc
$out = Join-Path $env:TEMP "ErenshorPartyTools.PanelPositioningTests.exe"

& $csc /nologo /target:exe ("/out:{0}" -f $out) `
    (Join-Path $ScriptRoot "src\PanelPositioning.cs") `
    (Join-Path $ScriptRoot "src\FriendAvailability.cs") `
    (Join-Path $ScriptRoot "src\NativeFriendRosterPolicy.cs") `
    (Join-Path $ScriptRoot "src\PartyModels.cs") `
    (Join-Path $ScriptRoot "src\PartyRollSocial.cs") `
    (Join-Path $ScriptRoot "src\PartyToolsCommandPolicy.cs") `
    (Join-Path $ScriptRoot "src\PartyToolsUiGeometry.cs") `
    (Join-Path $ScriptRoot "src\SuiteLauncherPolicy.cs") `
    (Join-Path $ScriptRoot "tests\FriendAvailabilityTests.cs") `
    (Join-Path $ScriptRoot "tests\PartyRollSocialTests.cs") `
    (Join-Path $ScriptRoot "tests\UiAndCommandPolicyTests.cs") `
    (Join-Path $ScriptRoot "tests\PanelPositioningTests.cs")
if ($LASTEXITCODE -ne 0) {
    throw "Panel positioning test compilation failed."
}

try {
    & $out
    if ($LASTEXITCODE -ne 0) {
        throw "Panel positioning tests failed with exit code $LASTEXITCODE."
    }
}
finally {
    Remove-Item $out -Force -ErrorAction SilentlyContinue
}
