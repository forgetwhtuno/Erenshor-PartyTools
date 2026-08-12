using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ErenshorPartyTools
{
    internal static class PartyStateReader
    {
        internal static List<ReadyRow> BuildReadyRows()
        {
            List<ReadyRow> rows = new List<ReadyRow>();
            rows.Add(new ReadyRow("You", ReadPlayerReadyState()));

            SimPlayerTracking[] members = null;
            try { members = GameData.GroupMembers; }
            catch { }
            if (members == null || members.Length == 0) return rows;

            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < members.Length; i++)
            {
                SimPlayerTracking tracking = members[i];
                if (tracking == null || string.IsNullOrWhiteSpace(tracking.SimName)) continue;
                string partyName = tracking.SimName.Trim();
                if (!seen.Add(partyName)) continue;

                SimPlayer sim = tracking.MyAvatar;
                if (sim == null)
                {
                    // This is also the safe representation for a COOP human that has a
                    // party tracking entry but no authoritative local SimPlayer state.
                    rows.Add(new ReadyRow(partyName, ReadyState.Unavailable));
                    continue;
                }

                if (CoopCompatibility.IsRemoteCoopHuman(sim) || CoopCompatibility.IsRemoteCoopSim(sim))
                {
                    rows.Add(new ReadyRow(partyName, ReadyState.Unavailable));
                    continue;
                }

                // The tracking entry is authoritative for presence (GameData.GroupMembers).
                // If the local Sim-side flags can't confirm liveness/grouping (e.g. during
                // zoning or a group-slot reshuffle), still show the row, just as UNAVAILABLE
                // rather than silently dropping a real party member.
                if (!IsCurrentPartySim(sim))
                {
                    rows.Add(new ReadyRow(partyName, ReadyState.Unavailable));
                    continue;
                }

                rows.Add(new ReadyRow(SafeDisplayName(sim, partyName), ReadSimReadyState(sim)));
            }

            return rows;
        }

        internal static bool IsRaidActive()
        {
            try { return GameData.RaidActive; }
            catch { return false; }
        }

        internal static List<PartyRollParticipant> GetLocalRollParticipants()
        {
            List<PartyRollParticipant> result = new List<PartyRollParticipant>();
            result.Add(new PartyRollParticipant(ReadPlayerName(), true));

            SimPlayerTracking[] members = null;
            try { members = GameData.GroupMembers; }
            catch { }
            if (members == null || members.Length == 0) return result;

            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < members.Length; i++)
            {
                SimPlayerTracking tracking = members[i];
                if (tracking == null || string.IsNullOrWhiteSpace(tracking.SimName)) continue;
                string partyName = tracking.SimName.Trim();
                if (!seen.Add(partyName)) continue;

                SimPlayer sim = tracking.MyAvatar;
                if (!IsLocallyRollEligible(sim)) continue;
                result.Add(new PartyRollParticipant(SafeDisplayName(sim, partyName), false));
            }

            return result;
        }

        internal static List<PanelRow> BuildFriendAvailabilityRows(
            List<string> configuredFriends,
            string persistentSeed,
            long sessionBlock,
            bool enabled)
        {
            List<PanelRow> rows = new List<PanelRow>();
            if (configuredFriends == null) return rows;

            for (int i = 0; i < configuredFriends.Count; i++)
            {
                string name = configuredFriends[i];
                bool excludedRemoteOrUnknown;
                bool nativeBusy = TryReadNativeBusy(name, out excludedRemoteOrUnknown);
                if (excludedRemoteOrUnknown) continue;

                FriendAvailabilityState simulated;
                if (!FriendAvailability.TryGetSimulatedState(name, persistentSeed, sessionBlock, enabled, out simulated))
                    continue;

                FriendAvailabilityState state = FriendAvailability.ApplyVerifiedBusy(simulated, nativeBusy);
                string value = nativeBusy ? "BUSY - GROUPED" : FriendAvailabilityText(state);
                rows.Add(new PanelRow(name, value, state != FriendAvailabilityState.Available));
            }
            return rows;
        }

        internal static List<PanelRow> BuildNativeFriendAvailabilityRows(out bool rosterAvailable)
        {
            rosterAvailable = false;
            List<PanelRow> rows = new List<PanelRow>();
            try
            {
                if (GameData.CurrentCharacterSlot == null || GameData.SimMngr == null || GameData.SimMngr.Sims == null)
                    return rows;

                int currentSlot = GameData.CurrentCharacterSlot.index;
                rosterAvailable = currentSlot >= 0;
                if (!rosterAvailable) return rows;

                HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                List<SimPlayerTracking> sims = GameData.SimMngr.Sims;
                for (int i = 0; i < sims.Count; i++)
                {
                    SimPlayerTracking tracking = sims[i];
                    if (tracking == null || string.IsNullOrWhiteSpace(tracking.SimName)) continue;
                    if (!NativeFriendRosterPolicy.IsCurrentCharacterFriend(
                        tracking.FriendedBy, currentSlot, tracking.IsGMCharacter)) continue;

                    string name = tracking.SimName.Trim();
                    if (!seen.Add(name)) continue;
                    FriendAvailabilityState state = NativeFriendRosterPolicy.Availability(tracking.online, tracking.Grouped);
                    string value = state == FriendAvailabilityState.Busy
                        ? "BUSY - GROUPED"
                        : FriendAvailabilityText(state);
                    rows.Add(new PanelRow(name, value, state != FriendAvailabilityState.Available));
                }
            }
            catch
            {
                rosterAvailable = false;
                rows.Clear();
            }
            return rows;
        }

        internal static string ReadPlayerName()
        {
            try
            {
                Character player = GameData.PlayerControl == null ? null : GameData.PlayerControl.Myself;
                if (player != null && player.MyStats != null && !string.IsNullOrWhiteSpace(player.MyStats.MyName))
                    return player.MyStats.MyName.Trim();
            }
            catch { }
            return "You";
        }

        internal static PartyRollTone ReadPartyRollTone(string simName)
        {
            SimPlayer sim = FindLocalPartySim(simName);
            if (sim == null) return PartyRollTone.Neutral;

            if (ReadBoolMember(sim, "Rival", false)) return PartyRollTone.Rival;
            object personality = ReadMember(sim, "PersonalityType");
            if (personality == null) personality = ReadMember(sim, "Personality");
            int code;
            try { code = personality == null ? -1 : Convert.ToInt32(personality); }
            catch { code = -1; }
            switch (code)
            {
                case 0:
                case 1: return PartyRollTone.Friendly;
                case 2: return PartyRollTone.Competitive;
                case 3: return PartyRollTone.Blunt;
                default: return PartyRollTone.Neutral;
            }
        }

        internal static string ApplyVanillaTypingStyle(string simName, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            SimPlayer sim = FindLocalPartySim(simName);
            if (sim == null) return text;
            try
            {
                if (GameData.SimMngr != null)
                {
                    string styled = GameData.SimMngr.PersonalizeString(text, sim);
                    if (!string.IsNullOrWhiteSpace(styled)) return styled;
                }
            }
            catch { }
            return text;
        }

        private static ReadyState ReadPlayerReadyState()
        {
            Character player = null;
            try { player = GameData.PlayerControl == null ? null : GameData.PlayerControl.Myself; }
            catch { }
            if (player == null) return ReadyState.Unavailable;
            if (!IsAvailableCharacter(player)) return ReadyState.Unavailable;
            try { if (!player.Alive) return ReadyState.Dead; }
            catch { return ReadyState.Unavailable; }

            try { if (GameData.InCombat) return ReadyState.InCombat; }
            catch { }
            return ReadyState.Ready;
        }

        private static ReadyState ReadSimReadyState(SimPlayer sim)
        {
            if (sim == null || sim.MyStats == null || sim.MyStats.Myself == null) return ReadyState.Unavailable;
            Character character = sim.MyStats.Myself;
            if (!IsAvailableSim(sim) || !IsAvailableCharacter(character)) return ReadyState.Unavailable;
            try { if (!character.Alive) return ReadyState.Dead; }
            catch { return ReadyState.Unavailable; }

            try { if (sim.IsSimGroupInCombat()) return ReadyState.InCombat; }
            catch { }

            try
            {
                NPC npc = character.MyNPC;
                if (npc != null && npc.CurrentAggroTarget != null) return ReadyState.InCombat;
            }
            catch { }

            return ReadyState.Ready;
        }

        private static bool IsLocallyRollEligible(SimPlayer sim)
        {
            if (!IsAvailableSim(sim)) return false;
            if (CoopCompatibility.IsRemoteCoopHuman(sim) || CoopCompatibility.IsRemoteCoopSim(sim)) return false;
            return IsCurrentPartySim(sim);
        }

        private static SimPlayer FindLocalPartySim(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            SimPlayerTracking[] members = null;
            try { members = GameData.GroupMembers; }
            catch { }
            if (members == null) return null;

            for (int i = 0; i < members.Length; i++)
            {
                SimPlayerTracking tracking = members[i];
                if (tracking == null) continue;
                SimPlayer sim = tracking.MyAvatar;
                if (!IsLocallyRollEligible(sim)) continue;
                string display = SafeDisplayName(sim, tracking.SimName);
                if (string.Equals(display, name, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(tracking.SimName, name, StringComparison.OrdinalIgnoreCase)) return sim;
            }
            return null;
        }

        private static object ReadMember(object target, string name)
        {
            if (target == null || string.IsNullOrWhiteSpace(name)) return null;
            Type type = target.GetType();
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            try
            {
                FieldInfo field = type.GetField(name, flags);
                if (field != null) return field.GetValue(target);
                PropertyInfo property = type.GetProperty(name, flags);
                if (property != null && property.GetIndexParameters().Length == 0)
                    return property.GetValue(target, null);
            }
            catch { }
            return null;
        }

        private static bool ReadBoolMember(object target, string name, bool fallback)
        {
            object value = ReadMember(target, name);
            if (value == null) return fallback;
            try { return Convert.ToBoolean(value); }
            catch { return fallback; }
        }

        private static bool TryReadNativeBusy(string name, out bool excludedRemoteOrUnknown)
        {
            excludedRemoteOrUnknown = false;
            if (string.IsNullOrWhiteSpace(name)) return false;

            SimPlayerTracking[] members = null;
            try { members = GameData.GroupMembers; }
            catch { }
            if (members == null) return false;

            for (int i = 0; i < members.Length; i++)
            {
                SimPlayerTracking tracking = members[i];
                if (tracking == null || !string.Equals(tracking.SimName, name, StringComparison.OrdinalIgnoreCase)) continue;
                SimPlayer sim = tracking.MyAvatar;
                if (sim == null || CoopCompatibility.IsRemoteCoopHuman(sim) || CoopCompatibility.IsRemoteCoopSim(sim))
                {
                    excludedRemoteOrUnknown = true;
                    return false;
                }

                // A verified current subgroup is real native activity. Do not replace it
                // with a simulated AVAILABLE state or invent an activity beyond GROUPED.
                return IsCurrentPartySim(sim);
            }
            return false;
        }

        private static string FriendAvailabilityText(FriendAvailabilityState state)
        {
            switch (state)
            {
                case FriendAvailabilityState.Busy: return "BUSY";
                case FriendAvailabilityState.Offline: return "OFFLINE";
                default: return "AVAILABLE";
            }
        }

        private static bool IsCurrentPartySim(SimPlayer sim)
        {
            try
            {
                return sim != null && sim.InGroup && GameData.SimPlayerGrouping != null &&
                       GameData.SimPlayerGrouping.IsSimInPlayerGroup(sim);
            }
            catch { return false; }
        }

        private static bool IsAvailableSim(SimPlayer sim)
        {
            try
            {
                return sim != null && sim.gameObject != null && sim.gameObject.activeInHierarchy &&
                       sim.MyStats != null && sim.MyStats.Myself != null;
            }
            catch { return false; }
        }

        private static bool IsAvailableCharacter(Character character)
        {
            try
            {
                return character != null && character.gameObject != null && character.gameObject.activeInHierarchy;
            }
            catch { return false; }
        }

        private static string SafeDisplayName(SimPlayer sim, string fallback)
        {
            string name = ReadSimName(sim);
            return string.IsNullOrWhiteSpace(name) ? fallback : name;
        }

        private static string ReadSimName(SimPlayer sim)
        {
            if (sim == null) return string.Empty;
            try
            {
                Character character = sim.MyStats == null ? null : sim.MyStats.Myself;
                NPC npc = character == null ? null : character.MyNPC;
                if (npc != null && !string.IsNullOrWhiteSpace(npc.NPCName)) return npc.NPCName.Trim();
            }
            catch { }
            try
            {
                if (sim.MyStats != null && !string.IsNullOrWhiteSpace(sim.MyStats.MyName))
                    return sim.MyStats.MyName.Trim();
            }
            catch { }
            try
            {
                return sim.gameObject == null ? string.Empty : sim.gameObject.name;
            }
            catch { return string.Empty; }
        }
    }
}
