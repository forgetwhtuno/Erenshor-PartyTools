using System;
using Lunaris;
using Lunaris.IPC;

namespace ErenshorPartyTools
{
    internal sealed class PartyToolsSuiteAuraProvider
    {
        private const string Prefix = "forgetwhtuno.erenshor.suite.partytools.v1.";
        private const string ActionIds = "openPanel,closePanel,resetPanel,resetLauncher,readyCheck,roll,rollParty,partyWho";
        private IAuraProvider<string> _describe;
        private IAuraProvider<string> _basicSettings;
        private IAuraProvider<string> _advancedSettings;
        private IAuraProvider<string> _uiState;
        private IAuraProvider<string, string, string> _settingSet;
        private IAuraProvider<string, string, string> _action;
        internal bool Registered { get; private set; }

        internal PartyToolsSuiteAuraProvider(LunarisPlugin owner)
        {
            if (owner == null) throw new ArgumentNullException("owner");
            try
            {
                _describe = owner.IPCAuraProvider<string>(Prefix + "describe"); _describe.RegisterFunc(Describe);
                _basicSettings = owner.IPCAuraProvider<string>(Prefix + "settings.basic"); _basicSettings.RegisterFunc(BasicSettings);
                _advancedSettings = owner.IPCAuraProvider<string>(Prefix + "settings.advanced"); _advancedSettings.RegisterFunc(AdvancedSettings);
                _uiState = owner.IPCAuraProvider<string>(Prefix + "ui.state"); _uiState.RegisterFunc(UiState);
                _settingSet = owner.IPCAuraProvider<string, string, string>(Prefix + "setting.set"); _settingSet.RegisterFunc(SetSetting);
                _action = owner.IPCAuraProvider<string, string, string>(Prefix + "action"); _action.RegisterFunc(InvokeAction);
                Registered = true;
            }
            catch { Unregister(); throw; }
        }

        internal void Unregister()
        {
            Registered = false;
            Safe(_describe); _describe = null; Safe(_basicSettings); _basicSettings = null; Safe(_advancedSettings); _advancedSettings = null;
            Safe(_uiState); _uiState = null; Safe(_settingSet); _settingSet = null; Safe(_action); _action = null;
        }

        private static void Safe(IAuraProvider provider) { if (provider == null) return; try { provider.UnregisterFunc(); } catch { } }

        private string Describe()
        {
            return "protocol=1&module=" + PartyToolsControlApi.ModuleId
                + "&display=" + Uri.EscapeDataString("Party Tools")
                + "&version=" + Uri.EscapeDataString(ErenshorPartyToolsPlugin.PluginVersion)
                + "&summary=" + Uri.EscapeDataString("Small deterministic party utilities: ready checks, rolls, and friend availability.")
                + "&status=" + Uri.EscapeDataString(PartyToolsControlApi.GetStatus())
                + "&actions=" + ActionIds;
        }

        private string UiState()
        {
            return SuiteUiStatePolicy.Build(PartyToolsControlApi.ModuleId, PartyToolsPanel.IsOpen,
                PartyToolsPanel.CanvasSortOrder, PartyToolsPanel.LastActivatedAt);
        }

        private string BasicSettings()
        {
            PartyToolsControlState state = PartyToolsControlApi.GetBasicState();
            return BoolLine("showLauncher", "Show Party Tools Launcher", "basic", state.ShowLauncher) + "\n" +
                   BoolLine("rollChatter", "Party roll chat summary", "basic", state.RollChatterEnabled);
        }

        private string AdvancedSettings() { return string.Empty; }

        private string SetSetting(string settingId, string value)
        {
            bool parsed;
            if (!TryParseBool(value, out parsed)) return "invalid value";
            if (settingId == "showLauncher") return PartyToolsControlApi.SetShowLauncher(parsed) ? "ok" : "rejected";
            if (settingId == "rollChatter") return PartyToolsControlApi.SetRollChatterEnabled(parsed) ? "ok" : "rejected";
            return "unknown setting";
        }

        private static bool TryParseBool(string value, out bool parsed)
        {
            if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)) { parsed = true; return true; }
            if (string.Equals(value, "false", StringComparison.OrdinalIgnoreCase)) { parsed = false; return true; }
            parsed = false; return false;
        }

        private static string BoolLine(string id, string label, string tier, bool value)
        {
            return "id=" + Uri.EscapeDataString(id) + "&label=" + Uri.EscapeDataString(label) + "&tier=" + tier + "&type=bool&value=" + (value ? "true" : "false") + "&mutable=true";
        }

        private string InvokeAction(string actionId, string argument)
        {
            switch (actionId)
            {
                case "openPanel": return PartyToolsControlApi.OpenPanel() ? "ok" : "rejected";
                case "closePanel": return PartyToolsControlApi.ClosePanel() ? "ok" : "rejected";
                case "resetPanel": return PartyToolsControlApi.ResetPanelPosition() ? "ok" : "rejected";
                case "resetLauncher": return PartyToolsControlApi.ResetLauncherPosition() ? "ok" : "rejected";
                case "readyCheck": return PartyToolsControlApi.ReadyCheck() ? "ok" : "rejected";
                case "partyWho": return PartyToolsControlApi.ShowPartyWho() ? "ok" : "rejected";
                case "roll": { int sides; if (!PartyToolsCommandPolicy.TryParseSides(argument, ErenshorPartyToolsPlugin.MaximumRollSides, out sides)) return "invalid argument"; return PartyToolsControlApi.Roll(sides) ? "ok" : "rejected"; }
                case "rollParty": { int sides; if (!PartyToolsCommandPolicy.TryParseSides(argument, ErenshorPartyToolsPlugin.MaximumRollSides, out sides)) return "invalid argument"; return PartyToolsControlApi.PartyRoll(sides) ? "ok" : "rejected"; }
                default: return "unknown action";
            }
        }
    }
}
