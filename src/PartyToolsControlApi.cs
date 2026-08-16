using System;
using System.Collections.Generic;

namespace ErenshorPartyTools
{
    public sealed class PartyToolsControlState
    {
        public bool GameplayReady;
        public bool RaidActive;
        public bool PanelOpen;
        public int LocalParticipantCount;
        public int PartyMemberCount;
        public string PlayerName;
        public bool ShowLauncher;
        public bool RollChatterEnabled;
        public bool FriendFallbackEnabled;
    }

    public static class PartyToolsControlApi
    {
        public const int ApiVersion = 1;
        public const string ModuleId = "partytools";
        public static bool HasDedicatedPanel { get { return true; } }
        public static bool IsPanelOpen { get { return PartyToolsPanel.IsOpen; } }
        public static string GetStatus()
        {
            PartyToolsControlState state = GetBasicState();
            return state.GameplayReady ? (state.RaidActive ? "Raid active; normal-party tools limited." : state.PartyMemberCount + " current party member(s).") : "Waiting for character.";
        }
        public static PartyToolsControlState GetBasicState()
        {
            PartyToolsControlState state = new PartyToolsControlState();
            state.GameplayReady = SuiteUiPolicy.IsGameplayReady();
            state.PanelOpen = PartyToolsPanel.IsOpen;
            ErenshorPartyToolsPlugin plugin = ErenshorPartyToolsPlugin.Instance;
            if (plugin != null) { state.ShowLauncher = plugin.ShowLauncherPreference; state.RollChatterEnabled = plugin.RollChatterPreference; state.FriendFallbackEnabled = plugin.FriendFallbackPreference; }
            try { state.RaidActive = PartyStateReader.IsRaidActive(); } catch { }
            // PlayerName is retained for local API shape compatibility but intentionally omitted from Suite-facing state.
            try { state.LocalParticipantCount = PartyStateReader.GetLocalRollParticipants().Count; } catch { }
            try { state.PartyMemberCount = PartyStateReader.BuildPartyWhoRows().Count; } catch { }
            return state;
        }
        public static bool OpenPanel()
        {
            ErenshorPartyToolsPlugin plugin = ErenshorPartyToolsPlugin.Instance;
            if (plugin == null || !SuiteUiPolicy.IsGameplayReady()) return false;
            plugin.RequestOpenToolsPanel(); return true;
        }
        public static bool TogglePanel()
        {
            ErenshorPartyToolsPlugin plugin = ErenshorPartyToolsPlugin.Instance; if (plugin == null || !SuiteUiPolicy.IsGameplayReady()) return false;
            if (PartyToolsPanel.IsOpen) plugin.RequestCloseToolsPanel(); else plugin.RequestOpenToolsPanel(); return true;
        }
        public static bool ClosePanel()
        {
            if (ErenshorPartyToolsPlugin.Instance == null) return false;
            PartyToolsPanel.Close();
            return !PartyToolsPanel.IsOpen;
        }
        public static bool ResetPanelPosition() { PartyToolsPanel.ResetPosition(); return true; }
        public static bool ResetLauncherPosition() { var p=ErenshorPartyToolsPlugin.Instance; if(p==null)return false; p.ResetLauncherPosition(); return true; }
        public static bool SetShowLauncher(bool value) { var p=ErenshorPartyToolsPlugin.Instance; if(p==null)return false; p.SetShowLauncherPreference(value); return true; }
        public static bool SetRollChatterEnabled(bool value) { var p=ErenshorPartyToolsPlugin.Instance; if(p==null)return false; p.SetRollChatterPreference(value); return true; }
        public static bool SetFriendFallbackEnabled(bool value) { var p=ErenshorPartyToolsPlugin.Instance; if(p==null)return false; p.SetFriendFallbackPreference(value); return true; }
        public static bool ReadyCheck() { var p = ErenshorPartyToolsPlugin.Instance; if (p == null || !SuiteUiPolicy.IsGameplayReady()) return false; p.ControlReadyCheck(); return true; }
        public static bool Roll(int sides) { var p = ErenshorPartyToolsPlugin.Instance; if (p == null || !SuiteUiPolicy.IsGameplayReady() || sides < 1 || sides > 1000000) return false; p.ControlRoll(sides); return true; }
        public static bool PartyRoll(int sides) { var p = ErenshorPartyToolsPlugin.Instance; if (p == null || !SuiteUiPolicy.IsGameplayReady() || sides < 1 || sides > 1000000) return false; p.ControlPartyRoll(sides); return true; }
        public static bool ShowPartyWho() { var p = ErenshorPartyToolsPlugin.Instance; if (p == null || !SuiteUiPolicy.IsGameplayReady()) return false; p.ControlPartyWho(); return true; }
        public static bool ShowFriendAvailability() { return ShowPartyWho(); }
    }
}
