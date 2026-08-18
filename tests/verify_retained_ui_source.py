from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src"

def read(name):
    return (SRC / name).read_text(encoding="utf-8")

def require(cond, msg):
    if not cond:
        raise AssertionError(msg)

panel = read("PartyToolsPanel.cs")
plugin = read("ErenshorPartyToolsPlugin.cs")
drag = read("PartyToolsDragGuard.cs")
aura = read("PartyToolsSuiteAuraProvider.cs")
control = read("PartyToolsControlApi.cs")
settings = read("PartyToolsSettings.cs")
project = (ROOT / "ErenshorPartyTools.csproj").read_text(encoding="utf-8")

for token in ("OnGUI", "GUILayout", "GUI.Window", "GUI.DragWindow", "GameData.EditUIMode", "PlayerControl.LeftClick", "csMouseOrbit"):
    require(token not in panel + plugin, "forbidden production UI token: " + token)
require("Input.GetKeyDown" not in plugin, "normal-access global hotkey polling remains")
require("OpenMenuKey" not in plugin, "legacy F7 setting is still wired into runtime access")
require("OpenMenuKey" in settings and "UI.Legacy" in settings, "legacy config compatibility was not retained")
for token in ("CanvasScaler", "GraphicRaycaster", "TextMeshProUGUI", "ScrollRect", "PartyToolsDragGuard"):
    require(token in panel, "retained UI component missing: " + token)
require("GameData.DraggingUIElement = true" in drag, "drag ownership never asserts the native flag")
require("GameData.DraggingUIElement = baseline" in drag,
        "drag ownership must restore the captured pre-gesture value, not blind-clear another UI owner")
require("drag = grip.gameObject.AddComponent<PartyToolsDragGuard>()" in panel, "launcher drag guard must attach to launcher grip")
require("AddImage(_headerGrip, new Color(0f, 0f, 0f, 0f))" in panel, "panel drag surface must be raycastable")
for action in ("ReadyCheck", "Roll(100)", "PartyRoll(100)", "ShowPartyWho"):
    require("PartyToolsControlApi." + action in panel, "panel action not routed through ControlApi: " + action)
require("showLauncher" in aura and "openPanel" in aura and "resetLauncher" in aura, "Aura panel/launcher contract incomplete")
require("!SuiteUiPolicy.IsGameplayReady()" in control, "ControlApi readiness gate missing")
require("Unity.TextMeshPro" in project, "project references do not match retained UI stack")
require("UnityEngine.IMGUIModule" not in project, "IMGUI reference reintroduced; Party Tools is retained-uGUI only")
require("UnityEngine.InputLegacyModule" in project,
        "project must reference InputLegacyModule: Escape handling and the drag guard use UnityEngine.Input")
require("BepInEx.dll" not in project and 'Reference Include="BepInEx' not in project, "BepInEx project reference remains")
print("verify_retained_ui_source: PASS")
