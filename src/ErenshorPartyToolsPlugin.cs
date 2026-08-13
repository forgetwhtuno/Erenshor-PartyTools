using System;
using System.Collections.Generic;
using System.Globalization;
using Lunaris;
using Lunaris.Config;
using HarmonyLib;

namespace ErenshorPartyTools
{
    [LunarisPlugin(PluginGuid, PluginVersion, "forgetwhtuno",
        "Deterministic party utilities: ready checks, cosmetic rolls, and friend availability.")]
    [LunarisPermission(LunarisPermission.Reflection | LunarisPermission.Harmony)]
    public sealed class ErenshorPartyToolsPlugin : LunarisPlugin
    {
        internal const string PluginGuid = "forgetwhtuno.erenshor.partytools";
        internal const string PluginName = "Erenshor Party Tools";
        internal const string PluginVersion = "0.1.2";
        internal const int MaximumRollSides = 1000000;

        internal static ErenshorPartyToolsPlugin Instance;

        private Harmony _harmony;
        private bool _pendingExternalOpen;
        private bool _pendingExternalClose;
        private int _pendingControlAction;
        private int _pendingControlSides;
        private PartyToolsSettings _settings;
        private PartyToolsConfigEntry<float> _panelNormalizedX;
        private PartyToolsConfigEntry<float> _panelNormalizedY;
        private PartyToolsConfigEntry<bool> _showLauncher;
        private PartyToolsConfigEntry<float> _launcherNormalizedX;
        private PartyToolsConfigEntry<float> _launcherNormalizedY;
        private PartyToolsConfigEntry<bool> _friendAvailabilityEnabled;
        private PartyToolsConfigEntry<int> _friendAvailabilitySessionHours;
        private PartyToolsConfigEntry<string> _friendAvailabilitySeed;
        private PartyToolsConfigEntry<string> _friendAvailabilityFriends;
        private PartyToolsConfigEntry<bool> _rollChatterEnabled;
        private PartyToolsSuiteAuraProvider _auraProvider;

        private void Awake()
        {
            Instance = this;

            _settings = new PartyToolsSettings();
            Config.Register(ref _settings);
            InitializeConfigEntries();

            PartyToolsPanel.ConfigurePosition(_panelNormalizedX.Value, _panelNormalizedY.Value, _launcherNormalizedX.Value, _launcherNormalizedY.Value, PersistPanelPosition, PersistLauncherPosition);
            EnsureFriendAvailabilitySeed();

            _harmony = new Harmony(PluginGuid);
            try
            {
                _harmony.PatchAll();
            }
            catch (Exception ex)
            {
                Logging.LogError("Erenshor Party Tools failed to patch: " + ex);
                return;
            }

            try { _auraProvider = new PartyToolsSuiteAuraProvider(this); }
            catch (Exception ex) { Logging.LogError("Erenshor Party Tools Suite Aura provider failed to register: " + ex); }

            Logging.LogInfo("Erenshor Party Tools loaded. Use the retained launcher/Hub panel; compatibility commands: /tools, /ready, /roll [max], /rollparty [max], /ptwho.");
        }

        private void InitializeConfigEntries()
        {
            _panelNormalizedX = new PartyToolsConfigEntry<float>(delegate { return _settings.PanelNormalizedX; }, delegate(float v) { _settings.PanelNormalizedX = v; });
            _panelNormalizedY = new PartyToolsConfigEntry<float>(delegate { return _settings.PanelNormalizedY; }, delegate(float v) { _settings.PanelNormalizedY = v; });
            _showLauncher = new PartyToolsConfigEntry<bool>(delegate { return _settings.ShowLauncher; }, delegate(bool v) { _settings.ShowLauncher = v; });
            _launcherNormalizedX = new PartyToolsConfigEntry<float>(delegate { return _settings.LauncherNormalizedX; }, delegate(float v) { _settings.LauncherNormalizedX = v; });
            _launcherNormalizedY = new PartyToolsConfigEntry<float>(delegate { return _settings.LauncherNormalizedY; }, delegate(float v) { _settings.LauncherNormalizedY = v; });
            _rollChatterEnabled = new PartyToolsConfigEntry<bool>(delegate { return _settings.RollChatterEnabled; }, delegate(bool v) { _settings.RollChatterEnabled = v; });
            _friendAvailabilityEnabled = new PartyToolsConfigEntry<bool>(delegate { return _settings.FriendAvailabilityEnabled; }, delegate(bool v) { _settings.FriendAvailabilityEnabled = v; });
            _friendAvailabilitySessionHours = new PartyToolsConfigEntry<int>(delegate { return _settings.FriendAvailabilitySessionHours; }, delegate(int v) { _settings.FriendAvailabilitySessionHours = v; });
            _friendAvailabilitySeed = new PartyToolsConfigEntry<string>(delegate { return _settings.FriendAvailabilitySeed; }, delegate(string v) { _settings.FriendAvailabilitySeed = v; });
            _friendAvailabilityFriends = new PartyToolsConfigEntry<string>(delegate { return _settings.FriendAvailabilityFriends; }, delegate(string v) { _settings.FriendAvailabilityFriends = v; });
        }

        private void Update()
        {
            try
            {
                bool ready = SuiteUiPolicy.IsGameplayReady();
                if (_pendingExternalClose) { _pendingExternalClose = false; PartyToolsPanel.Close(); }
                if (_pendingExternalOpen) { _pendingExternalOpen = false; if (ready) PartyToolsPanel.ShowCommandMenu(HandleMenuAction); }
                if (_pendingControlAction != 0 && ready)
                {
                    int action = _pendingControlAction; int sides = _pendingControlSides;
                    _pendingControlAction = 0; _pendingControlSides = 0;
                    if (action == 1) ShowReadyCheck();
                    else if (action == 2) ShowLocalRoll(sides, true);
                    else if (action == 3) ShowPartyRoll(sides);
                    else if (action == 4) ShowFriendAvailability();
                }
                if (!ready) { _pendingControlAction = 0; _pendingControlSides = 0; PartyToolsPanel.Close(); }
                bool bridge = _auraProvider != null && _auraProvider.Registered;
                bool launcher = SuiteLauncherPolicy.ShouldShow(ready, SuiteUiPolicy.IsHubAvailable(), bridge, ShowLauncherPreference);
                PartyToolsPanel.Tick(launcher);
            }
            catch (Exception ex)
            {
                Logging.LogError("Party Tools update/UI failed: " + ex);
                PartyToolsPanel.Close(); PartyToolsPanel.ReleaseDrag();
            }
        }

        private void OnDestroy()
        {
            // Lunaris can unload/reload plugins while Erenshor is running. Release everything
            // this plugin owns before clearing the singleton.
            try { if (_auraProvider != null) _auraProvider.Unregister(); } catch { }
            _auraProvider = null;
            try { PartyToolsPanel.Dispose(); } catch { }
            try { CoopCompatibility.Shutdown(); } catch { }
            try { if (_harmony != null) _harmony.UnpatchSelf(); } catch { }
            _harmony = null;
            _pendingExternalOpen = _pendingExternalClose = false; _pendingControlAction = _pendingControlSides = 0;
            SuiteUiPolicy.Reset();
            Instance = null;
        }

        internal void RequestOpenToolsPanel() { _pendingExternalOpen = true; }
        internal void RequestCloseToolsPanel() { _pendingExternalClose = true; }
        internal void ControlReadyCheck() { _pendingControlAction = 1; _pendingControlSides = 0; }
        internal void ControlRoll(int sides) { _pendingControlAction = 2; _pendingControlSides = sides; }
        internal void ControlPartyRoll(int sides) { _pendingControlAction = 3; _pendingControlSides = sides; }
        internal void ControlFriendAvailability() { _pendingControlAction = 4; _pendingControlSides = 0; }

        private void PersistPanelPosition(float x, float y)
        {
            if (_panelNormalizedX == null || _panelNormalizedY == null) return;
            _panelNormalizedX.Value = x; _panelNormalizedY.Value = y; Config.Save();
        }

        private void PersistLauncherPosition(float x, float y)
        {
            if (_launcherNormalizedX == null || _launcherNormalizedY == null) return;
            _launcherNormalizedX.Value = x; _launcherNormalizedY.Value = y; Config.Save();
        }

        internal bool ShowLauncherPreference { get { return _showLauncher == null || _showLauncher.Value; } }
        internal bool RollChatterPreference { get { return _rollChatterEnabled != null && _rollChatterEnabled.Value; } }
        internal bool FriendFallbackPreference { get { return _friendAvailabilityEnabled != null && _friendAvailabilityEnabled.Value; } }
        internal void SetShowLauncherPreference(bool value) { if (_showLauncher != null) { _showLauncher.Value = value; Config.Save(); } }
        internal void SetRollChatterPreference(bool value) { if (_rollChatterEnabled != null) { _rollChatterEnabled.Value = value; Config.Save(); } }
        internal void SetFriendFallbackPreference(bool value) { if (_friendAvailabilityEnabled != null) { _friendAvailabilityEnabled.Value = value; Config.Save(); } }
        internal void ResetLauncherPosition() { if (_launcherNormalizedX != null) _launcherNormalizedX.Value = PartyToolsUiGeometry.Unset; if (_launcherNormalizedY != null) _launcherNormalizedY.Value = PartyToolsUiGeometry.Unset; PartyToolsPanel.ResetLauncherPosition(); Config.Save(); }

        internal void Chat(string message, string color)
        {
            try { UpdateSocialLog.LogAdd(message, color); }
            catch
            {
                try { UpdateSocialLog.LogAdd(message); }
                catch { }
            }
        }

        internal void LogPatchError(Exception ex)
        {
            try { Logging.LogError("Party Tools command patch failed: " + ex); }
            catch { }
        }

        internal bool TryHandle(TypeText typeText, string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return false;

            string command = raw.Trim();
            string argument;

            if (TryMatchCommand(command, "/tools", out argument))
            {
                ClearInput(typeText);
                if (!string.IsNullOrWhiteSpace(argument))
                {
                    Chat("[Party Tools] Usage: /tools", "yellow");
                    return true;
                }
                PartyToolsPanel.ShowCommandMenu(HandleMenuAction);
                return true;
            }

            // Check /rollparty before /roll so the longer command can never be
            // misinterpreted as the shorter one.
            if (TryMatchCommand(command, "/rollparty", out argument))
            {
                ClearInput(typeText);
                int sides;
                if (!TryParseSides(argument, out sides))
                {
                    Chat("[Party Tools] Usage: /rollparty [1-" + MaximumRollSides.ToString(CultureInfo.InvariantCulture) + "]", "yellow");
                    return true;
                }

                ShowPartyRoll(sides);
                return true;
            }

            if (TryMatchCommand(command, "/ptwho", out argument))
            {
                ClearInput(typeText);
                if (!string.IsNullOrWhiteSpace(argument))
                {
                    Chat("[Party Tools] Usage: /ptwho", "yellow");
                    return true;
                }

                ShowFriendAvailability();
                return true;
            }

            if (TryMatchCommand(command, "/ready", out argument))
            {
                ClearInput(typeText);
                if (!string.IsNullOrWhiteSpace(argument))
                {
                    Chat("[Party Tools] Usage: /ready", "yellow");
                    return true;
                }
                ShowReadyCheck();
                return true;
            }

            if (TryMatchCommand(command, "/roll", out argument))
            {
                ClearInput(typeText);
                int sides;
                if (!TryParseSides(argument, out sides))
                {
                    Chat("[Party Tools] Usage: /roll [1-" + MaximumRollSides.ToString(CultureInfo.InvariantCulture) + "]", "yellow");
                    return true;
                }

                ShowLocalRoll(sides, false);
                return true;
            }

            return false;
        }

        private void HandleMenuAction(PartyToolsAction action)
        {
            switch (action)
            {
                case PartyToolsAction.ReadyCheck: ShowReadyCheck(); break;
                case PartyToolsAction.Roll: ShowLocalRoll(100, true); break;
                case PartyToolsAction.PartyRoll: ShowPartyRoll(100); break;
                case PartyToolsAction.FriendAvailability: ShowFriendAvailability(); break;
            }
        }

        private void ShowReadyCheck()
        {
            if (PartyStateReader.IsRaidActive())
            {
                Chat("[Party Tools] /ready is unavailable during a raid.", "yellow");
                return;
            }
            try { PartyToolsPanel.ShowReadyCheck(); }
            catch (Exception ex) { Logging.LogError("Party Tools ready-check UI failed: " + ex); }
        }

        private void ShowLocalRoll(int sides, bool showPanel)
        {
            int value = Roller.Roll(sides);
            string name = PartyStateReader.ReadPlayerName();
            Chat(name + " rolls " + value.ToString(CultureInfo.InvariantCulture) + " (1-" + sides.ToString(CultureInfo.InvariantCulture) + ").", "lightblue");
            if (showPanel) PartyToolsPanel.ShowLocalRoll(name, sides, value);
        }

        private void ShowPartyRoll(int sides)
        {
            if (PartyStateReader.IsRaidActive())
            {
                Chat("[Party Tools] /rollparty is unavailable during a raid.", "yellow");
                return;
            }
            try
            {
                List<PartyRollParticipant> participants = PartyStateReader.GetLocalRollParticipants();
                List<PanelRow> rows = new List<PanelRow>();
                List<PartyRollResult> results = new List<PartyRollResult>();
                for (int i = 0; i < participants.Count; i++)
                {
                    PartyRollParticipant participant = participants[i];
                    int value = Roller.Roll(sides);
                    results.Add(new PartyRollResult(participant, value));
                    rows.Add(new PanelRow(participant.Name, value.ToString(CultureInfo.InvariantCulture), false));
                }
                PartyToolsPanel.ShowPartyRoll(sides, rows);
                if (_rollChatterEnabled != null && _rollChatterEnabled.Value)
                    ShowPartyRollChatter(sides, results);
            }
            catch (Exception ex) { Logging.LogError("Party Tools /rollparty handling failed: " + ex); }
        }

        private void ShowPartyRollChatter(int sides, List<PartyRollResult> results)
        {
            if (results == null || results.Count == 0) return;
            if (results.Count == 1)
            {
                Chat(PartyRollSocial.Result(results[0], sides), "lightblue");
                return;
            }
            Chat(PartyRollSocial.Opening(PartyStateReader.ReadPlayerName(), sides), "lightblue");
            for (int i = 0; i < results.Count; i++)
            {
                PartyRollResult result = results[i];
                if (result == null || result.Participant == null || result.Participant.IsPlayer) continue;
                string name = result.Participant.Name;
                PartyRollTone tone = PartyStateReader.ReadPartyRollTone(name);
                string agreement = PartyStateReader.ApplyVanillaTypingStyle(name, PartyRollSocial.Agreement(tone));
                Chat(name + " tells the party: " + agreement, "lightblue");
            }
            for (int i = 0; i < results.Count; i++)
                Chat(PartyRollSocial.Result(results[i], sides), "lightblue");

            PartyRollResult winner = PartyRollSocial.SingleWinner(results);
            if (winner != null && winner.Participant != null)
            {
                string name = winner.Participant.Name;
                PartyRollTone tone = winner.Participant.IsPlayer ? PartyRollTone.Neutral : PartyStateReader.ReadPartyRollTone(name);
                string reaction = PartyRollSocial.Winner(tone);
                if (!winner.Participant.IsPlayer) reaction = PartyStateReader.ApplyVanillaTypingStyle(name, reaction);
                Chat(name + " tells the party: " + reaction, "lightblue");
            }
        }

        private void ShowFriendAvailability()
        {
            try
            {
                bool nativeRosterAvailable;
                List<PanelRow> rows = PartyStateReader.BuildNativeFriendAvailabilityRows(out nativeRosterAvailable);
                if (nativeRosterAvailable)
                {
                    Logging.LogInfo("/ptwho read " + rows.Count.ToString(CultureInfo.InvariantCulture) +
                        " friend(s) from Erenshor's native roster for the current character.");
                    if (rows.Count == 0)
                    {
                        Chat("[Party Tools] Erenshor's Friends filter is empty for this character.", "yellow");
                        return;
                    }
                    PartyToolsPanel.ShowFriendAvailability(rows);
                    return;
                }

                List<string> friends = FriendAvailability.ParseConfiguredFriends(_friendAvailabilityFriends.Value);
                if (friends.Count == 0)
                {
                    Chat("[Party Tools] Erenshor's native friend roster is not ready, and no compatibility fallback names are configured.", "yellow");
                    return;
                }
                long sessionBlock = FriendAvailability.GetSessionBlock(DateTime.UtcNow, _friendAvailabilitySessionHours.Value);
                rows = PartyStateReader.BuildFriendAvailabilityRows(
                    friends, _friendAvailabilitySeed.Value, sessionBlock, _friendAvailabilityEnabled.Value);
                if (rows.Count == 0)
                {
                    Chat("[Party Tools] No configured local Sim friends are observable right now.", "yellow");
                    return;
                }
                PartyToolsPanel.ShowFriendAvailability(rows);
            }
            catch (Exception ex) { Logging.LogError("Party Tools /ptwho handling failed: " + ex); }
        }

        private static bool TryMatchCommand(string raw, string command, out string argument)
        {
            argument = null;
            if (raw == null || command == null) return false;
            if (!raw.StartsWith(command, StringComparison.OrdinalIgnoreCase)) return false;
            if (raw.Length > command.Length && !char.IsWhiteSpace(raw[command.Length])) return false;
            argument = raw.Length == command.Length ? string.Empty : raw.Substring(command.Length).Trim();
            return true;
        }

        private static bool TryParseSides(string argument, out int sides)
        {
            return PartyToolsCommandPolicy.TryParseSides(argument, MaximumRollSides, out sides);
        }

        private void EnsureFriendAvailabilitySeed()
        {
            if (_friendAvailabilitySeed == null || !string.IsNullOrWhiteSpace(_friendAvailabilitySeed.Value)) return;
            _friendAvailabilitySeed.Value = Guid.NewGuid().ToString("N");
            Config.Save();
        }

        private static void ClearInput(TypeText typeText)
        {
            try
            {
                if (typeText != null && typeText.typed != null) typeText.typed.text = string.Empty;
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(TypeText), "CheckCommands")]
    internal static class PartyToolsChatPatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(TypeText __instance)
        {
            try
            {
                return ErenshorPartyToolsPlugin.Instance == null ||
                       __instance == null ||
                       __instance.typed == null ||
                       !ErenshorPartyToolsPlugin.Instance.TryHandle(__instance, __instance.typed.text);
            }
            catch (Exception ex)
            {
                if (ErenshorPartyToolsPlugin.Instance != null)
                    ErenshorPartyToolsPlugin.Instance.LogPatchError(ex);
                return true;
            }
        }
    }

}
