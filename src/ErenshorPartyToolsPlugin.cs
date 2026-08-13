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
        private PartyToolsSettings _settings;
        private PartyToolsConfigEntry<float> _panelOffsetX;
        private PartyToolsConfigEntry<float> _panelOffsetY;
        private PartyToolsConfigEntry<bool> _friendAvailabilityEnabled;
        private PartyToolsConfigEntry<int> _friendAvailabilitySessionHours;
        private PartyToolsConfigEntry<string> _friendAvailabilitySeed;
        private PartyToolsConfigEntry<string> _friendAvailabilityFriends;
        private PartyToolsConfigEntry<UnityEngine.KeyCode> _openMenuKey;
        private PartyToolsConfigEntry<bool> _rollChatterEnabled;

        private void Awake()
        {
            Instance = this;

            _settings = new PartyToolsSettings();
            Config.Register(ref _settings);
            InitializeConfigEntries();

            PartyToolsPanel.ConfigurePosition(_panelOffsetX.Value, _panelOffsetY.Value, PersistPanelPosition);
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
            Logging.LogInfo("Erenshor Party Tools loaded. Use F7 or /tools for the command menu; commands: /ready, /roll [max], /rollparty [max], /ptwho.");
        }

        private void InitializeConfigEntries()
        {
            _panelOffsetX = new PartyToolsConfigEntry<float>(delegate { return _settings.PanelOffsetX; }, delegate(float v) { _settings.PanelOffsetX = v; });
            _panelOffsetY = new PartyToolsConfigEntry<float>(delegate { return _settings.PanelOffsetY; }, delegate(float v) { _settings.PanelOffsetY = v; });
            _openMenuKey = new PartyToolsConfigEntry<UnityEngine.KeyCode>(delegate { return _settings.OpenMenuKey; }, delegate(UnityEngine.KeyCode v) { _settings.OpenMenuKey = v; });
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
                if (_openMenuKey != null && UnityEngine.Input.GetKeyDown(_openMenuKey.Value))
                {
                    if (PartyToolsPanel.IsCommandMenuOpen) PartyToolsPanel.Close();
                    else PartyToolsPanel.ShowCommandMenu(HandleMenuAction);
                }
                PartyToolsPanel.Tick();
            }
            catch (Exception ex)
            {
                Logging.LogError("Party Tools update failed: " + ex);
                PartyToolsPanel.Close();
            }
        }

        private void OnGUI()
        {
            try
            {
                PartyToolsPanel.Draw();
            }
            catch (Exception ex)
            {
                Logging.LogError("Party Tools UI failed: " + ex);
                PartyToolsPanel.Close();
            }
        }

        private void OnDestroy()
        {
            // Lunaris can unload/reload plugins while Erenshor is running. Release everything
            // this plugin owns before clearing the singleton.
            try { PartyToolsPanel.Dispose(); } catch { }
            try { CoopCompatibility.Shutdown(); } catch { }
            try { if (_harmony != null) _harmony.UnpatchSelf(); } catch { }
            _harmony = null;
            Instance = null;
        }

        private void PersistPanelPosition(float offsetX, float offsetY)
        {
            if (_panelOffsetX == null || _panelOffsetY == null) return;
            if (PanelPositioning.NearlyEqual(_panelOffsetX.Value, offsetX) &&
                PanelPositioning.NearlyEqual(_panelOffsetY.Value, offsetY)) return;

            _panelOffsetX.Value = offsetX;
            _panelOffsetY.Value = offsetY;
            Config.Save();
        }

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

                ShowLocalRoll(sides);
                return true;
            }

            return false;
        }

        private void HandleMenuAction(PartyToolsAction action)
        {
            switch (action)
            {
                case PartyToolsAction.ReadyCheck: ShowReadyCheck(); break;
                case PartyToolsAction.Roll: ShowLocalRoll(100); break;
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

        private void ShowLocalRoll(int sides)
        {
            int value = Roller.Roll(sides);
            string name = PartyStateReader.ReadPlayerName();
            Chat(name + " rolls " + value.ToString(CultureInfo.InvariantCulture) + " (1-" + sides.ToString(CultureInfo.InvariantCulture) + ").", "lightblue");
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
            sides = 100;
            if (string.IsNullOrWhiteSpace(argument)) return true;
            string value = argument.Trim();
            if (value.IndexOfAny(new char[] { ' ', '\t', '\r', '\n' }) >= 0) return false;
            if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out sides)) return false;
            return sides >= 1 && sides <= MaximumRollSides;
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

    [HarmonyPatch(typeof(PlayerControl), "LeftClick")]
    internal static class PartyToolsPanelLeftClickPatch
    {
        [HarmonyPrefix]
        private static bool Prefix()
        {
            try { return !PartyToolsPanel.PointerIsOverPanel(new UnityEngine.Vector2(UnityEngine.Input.mousePosition.x, UnityEngine.Screen.height - UnityEngine.Input.mousePosition.y)); }
            catch { return true; }
        }
    }

    [HarmonyPatch(typeof(csMouseOrbit), "LateUpdate")]
    internal static class PartyToolsCameraLookPatch
    {
        private static csMouseOrbit _muted;
        private static float _x;
        private static float _y;

        private static void Prefix(csMouseOrbit __instance)
        {
            Restore();
            try
            {
                UnityEngine.Vector2 point = new UnityEngine.Vector2(UnityEngine.Input.mousePosition.x, UnityEngine.Screen.height - UnityEngine.Input.mousePosition.y);
                if (__instance == null || !PartyToolsPanel.PointerIsOverPanel(point)) return;
                _x = __instance.xSpeed; _y = __instance.ySpeed;
                __instance.xSpeed = 0f; __instance.ySpeed = 0f; _muted = __instance;
            }
            catch { }
        }

        private static void Postfix() { Restore(); }

        private static void Restore()
        {
            csMouseOrbit orbit = _muted; _muted = null;
            if (orbit == null) return;
            try { orbit.xSpeed = _x; orbit.ySpeed = _y; } catch { }
        }
    }
}
