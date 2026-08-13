using System;
using System.Globalization;
using Lunaris;
using Lunaris.IPC;

namespace ErenshorPartyTools
{
    // Thin, optional transport adapter over the public PartyToolsControlApi. No gameplay logic
    // here - every action call revalidates through the owning mod's real state. Never references
    // ErenshorSuiteHub.dll.
    internal sealed class PartyToolsSuiteAuraProvider
    {
        private const string Prefix = "forgetwhtuno.erenshor.suite.partytools.v1.";
        private const string ActionIds = "openPanel,closePanel,resetPanel,resetLauncher,readyCheck,roll,rollParty,friendAvailability";

        private IAuraProvider<string> _describe;
        private IAuraProvider<string> _basicSettings;
        private IAuraProvider<string> _advancedSettings;
        private IAuraProvider<string, string, string> _settingSet;
        private IAuraProvider<string, string, string> _action;
        internal bool Registered { get; private set; }

        internal PartyToolsSuiteAuraProvider(LunarisPlugin owner)
        {
            if (owner == null) throw new ArgumentNullException("owner");
            try
            {
                _describe = owner.IPCAuraProvider<string>(Prefix + "describe");
                _describe.RegisterFunc(Describe);

                _basicSettings = owner.IPCAuraProvider<string>(Prefix + "settings.basic");
                _basicSettings.RegisterFunc(BasicSettings);
                _advancedSettings = owner.IPCAuraProvider<string>(Prefix + "settings.advanced");
                _advancedSettings.RegisterFunc(AdvancedSettings);
                _settingSet = owner.IPCAuraProvider<string, string, string>(Prefix + "setting.set");
                _settingSet.RegisterFunc(SetSetting);
                _action = owner.IPCAuraProvider<string, string, string>(Prefix + "action");
                _action.RegisterFunc(InvokeAction);
                Registered = true;
            }
            catch
            {
                Unregister();
                throw;
            }
        }

        // Call from the plugin's OnDestroy(). MANDATORY - explicitly unregister every handler.
        internal void Unregister()
        {
            try { if (_describe != null) _describe.UnregisterFunc(); } catch { } _describe = null;
            try { if (_basicSettings != null) _basicSettings.UnregisterFunc(); } catch { } _basicSettings = null;
            try { if (_advancedSettings != null) _advancedSettings.UnregisterFunc(); } catch { } _advancedSettings = null;
            try { if (_settingSet != null) _settingSet.UnregisterFunc(); } catch { } _settingSet = null;
            try { if (_action != null) _action.UnregisterFunc(); } catch { } _action = null;
            Registered = false;
        }

        private string Describe()
        {
            return "protocol=1"
                + "&module=" + PartyToolsControlApi.ModuleId
                + "&display=" + Uri.EscapeDataString("Party Tools")
                + "&version=" + Uri.EscapeDataString(ErenshorPartyToolsPlugin.PluginVersion)
                + "&summary=" + Uri.EscapeDataString("Deterministic party utilities: ready checks, cosmetic rolls, and friend availability.")
                + "&status=" + Uri.EscapeDataString(PartyToolsControlApi.GetStatus())
                + "&actions=" + ActionIds;
        }

        private string BasicSettings()
        {
            PartyToolsControlState s = PartyToolsControlApi.GetBasicState();
            return BoolLine("showLauncher", "Show Party Tools launcher", "basic", s.ShowLauncher) + "\n" +
                   BoolLine("rollChatter", "Party roll chatter", "basic", s.RollChatterEnabled);
        }

        private string AdvancedSettings()
        {
            PartyToolsControlState s = PartyToolsControlApi.GetBasicState();
            return BoolLine("friendFallback", "Friend availability fallback", "advanced", s.FriendFallbackEnabled);
        }

        private string SetSetting(string settingId, string value)
        {
            bool v = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
            if (settingId == "showLauncher") return PartyToolsControlApi.SetShowLauncher(v) ? "ok" : "rejected";
            if (settingId == "rollChatter") return PartyToolsControlApi.SetRollChatterEnabled(v) ? "ok" : "rejected";
            if (settingId == "friendFallback") return PartyToolsControlApi.SetFriendFallbackEnabled(v) ? "ok" : "rejected";
            return "unknown setting";
        }

        private static string BoolLine(string id, string label, string tier, bool value)
        {
            return "id=" + Uri.EscapeDataString(id) + "&label=" + Uri.EscapeDataString(label) + "&tier=" + tier + "&type=bool&value=" + (value ? "true" : "false") + "&mutable=true";
        }

        private string InvokeAction(string actionId, string argument)
        {
            // Only advertised, explicit, safe player-facing actions. Revalidate everything -
            // Hub is not authorization. Consume UI-sensitive actions on the mod's Update path,
            // not synchronously from here - route through the same pending-flag pattern
            // PartyToolsControlApi.OpenPanel()/etc. already use.
            switch (actionId)
            {
                case "openPanel": return PartyToolsControlApi.OpenPanel() ? "ok" : "rejected";
                case "closePanel": return PartyToolsControlApi.ClosePanel() ? "ok" : "rejected";
                case "resetPanel": return PartyToolsControlApi.ResetPanelPosition() ? "ok" : "rejected";
                case "resetLauncher": return PartyToolsControlApi.ResetLauncherPosition() ? "ok" : "rejected";
                case "readyCheck": return PartyToolsControlApi.ReadyCheck() ? "ok" : "rejected";
                case "roll":
                {
                    int sides;
                    if (!TryParseSides(argument, out sides)) return "invalid argument";
                    return PartyToolsControlApi.Roll(sides) ? "ok" : "rejected";
                }
                case "rollParty":
                {
                    int sides;
                    if (!TryParseSides(argument, out sides)) return "invalid argument";
                    return PartyToolsControlApi.PartyRoll(sides) ? "ok" : "rejected";
                }
                case "friendAvailability": return PartyToolsControlApi.ShowFriendAvailability() ? "ok" : "rejected";
                default: return "unknown action";
            }
        }

        private static bool TryParseSides(string argument, out int sides)
        {
            return PartyToolsCommandPolicy.TryParseSides(argument, ErenshorPartyToolsPlugin.MaximumRollSides, out sides);
        }
    }
}
