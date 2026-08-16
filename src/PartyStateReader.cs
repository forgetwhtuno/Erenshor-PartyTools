using System;
using System.Collections.Generic;
using UnityEngine;

namespace ErenshorPartyTools
{
    internal static class PartyStateReader
    {
        internal static List<ReadyRow> BuildReadyRows()
        {
            List<ReadyRow> rows = new List<ReadyRow>();
            rows.Add(new ReadyRow("You", ReadPlayerReadyState()));

            SimPlayerTracking[] members = ReadGroupMembers();
            if (members == null || members.Length == 0) return rows;

            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < members.Length; i++)
            {
                SimPlayerTracking tracking = members[i];
                string partyName = ReadTrackingName(tracking);
                if (partyName.Length == 0 || !seen.Add(partyName)) continue;

                SimPlayer sim = SafeAvatar(tracking);
                if (sim == null)
                {
                    rows.Add(new ReadyRow(partyName, ReadyState.Unavailable));
                    continue;
                }

                if (CoopCompatibility.IsRemoteCoopHuman(sim))
                {
                    // We can authoritatively identify COOP ownership from its explicit component,
                    // but Party Tools cannot authoritatively answer for that remote human.
                    rows.Add(new ReadyRow(partyName, ReadyState.RemotePlayer));
                    continue;
                }
                if (CoopCompatibility.IsRemoteCoopSim(sim))
                {
                    rows.Add(new ReadyRow(partyName, ReadyState.Unavailable));
                    continue;
                }

                if (!IsCurrentPartySim(sim))
                {
                    rows.Add(new ReadyRow(partyName, ReadyState.Unavailable));
                    continue;
                }

                rows.Add(new ReadyRow(SafeDisplayName(sim, partyName), ReadSimReadyState(sim)));
            }
            return rows;
        }

        internal static List<PanelRow> BuildPartyWhoRows()
        {
            List<PanelRow> rows = new List<PanelRow>();
            rows.Add(new PanelRow(ReadPlayerName(),
                PartySnapshotPolicy.Describe(PartyWhoKind.LocalPlayer, ReadPlayerLevel(), string.Empty), false));

            SimPlayerTracking[] members = ReadGroupMembers();
            if (members == null || members.Length == 0) return rows;

            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < members.Length; i++)
            {
                SimPlayerTracking tracking = members[i];
                string partyName = ReadTrackingName(tracking);
                if (partyName.Length == 0 || !seen.Add(partyName)) continue;

                SimPlayer sim = SafeAvatar(tracking);
                bool hasAvatar = sim != null && IsAvailableSim(sim);
                bool remoteHuman = hasAvatar && CoopCompatibility.IsRemoteCoopHuman(sim);
                bool remoteSim = hasAvatar && CoopCompatibility.IsRemoteCoopSim(sim);
                bool currentParty = hasAvatar && !remoteHuman && !remoteSim && IsCurrentPartySim(sim);
                PartyWhoKind kind = PartySnapshotPolicy.Classify(false, hasAvatar, remoteHuman, remoteSim, currentParty);
                string display = hasAvatar ? SafeDisplayName(sim, partyName) : partyName;
                int level = kind == PartyWhoKind.LocalSim ? ReadTrackingLevel(tracking) : 0;
                string className = kind == PartyWhoKind.LocalSim ? ReadTrackingClass(tracking) : string.Empty;
                rows.Add(new PanelRow(display, PartySnapshotPolicy.Describe(kind, level, className),
                    kind != PartyWhoKind.LocalSim));
            }
            return rows;
        }

        // This mirrors Group Builder's native Friends filter.  The roster belongs to the
        // current character, not to the active party, so it is the authoritative source for
        // finding Sims who may be available to group with.
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
                    string name = ReadTrackingName(tracking);
                    if (name.Length == 0 || !seen.Add(name)) continue;
                    if (!NativeFriendRosterPolicy.IsCurrentCharacterFriend(
                        tracking.FriendedBy, currentSlot, tracking.IsGMCharacter)) continue;

                    FriendAvailabilityState state = NativeFriendRosterPolicy.Availability(tracking.online, tracking.Grouped);
                    string value = state == FriendAvailabilityState.Busy ? "BUSY - GROUPED" : FriendAvailabilityText(state);
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

        internal static bool IsRaidActive()
        {
            try { return GameData.RaidActive; }
            catch { return false; }
        }

        internal static List<PartyRollParticipant> GetLocalRollParticipants()
        {
            List<PartyRollParticipant> result = new List<PartyRollParticipant>();
            result.Add(new PartyRollParticipant(ReadPlayerName(), true));

            SimPlayerTracking[] members = ReadGroupMembers();
            if (members == null || members.Length == 0) return result;

            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < members.Length; i++)
            {
                SimPlayerTracking tracking = members[i];
                string partyName = ReadTrackingName(tracking);
                if (partyName.Length == 0 || !seen.Add(partyName)) continue;
                SimPlayer sim = SafeAvatar(tracking);
                if (!IsLocallyRollEligible(sim)) continue;
                result.Add(new PartyRollParticipant(SafeDisplayName(sim, partyName), false));
            }
            return result;
        }

        internal static string ReadPlayerName()
        {
            try
            {
                Character player = GameData.PlayerControl == null ? null : GameData.PlayerControl.Myself;
                if (player != null && player.MyStats != null && !string.IsNullOrWhiteSpace(player.MyStats.MyName))
                    return CleanDisplayName(player.MyStats.MyName, "You");
            }
            catch { }
            return "You";
        }

        internal static int ReadPlayerLevel()
        {
            try
            {
                Character player = GameData.PlayerControl == null ? null : GameData.PlayerControl.Myself;
                if (player != null && player.MyStats != null && player.MyStats.Level > 0) return player.MyStats.Level;
            }
            catch { }
            return 0;
        }

        private static ReadyState ReadPlayerReadyState()
        {
            Character player = null;
            try { player = GameData.PlayerControl == null ? null : GameData.PlayerControl.Myself; }
            catch { }
            bool available = IsAvailableCharacter(player);
            bool aliveKnown = false;
            bool alive = false;
            if (available)
            {
                try { alive = player.Alive; aliveKnown = true; }
                catch { }
            }
            bool combatKnown = false;
            bool inCombat = false;
            try { inCombat = GameData.InCombat; combatKnown = true; }
            catch { }
            return ReadyStatePolicy.Classify(available, false, aliveKnown, alive, combatKnown, inCombat);
        }

        private static ReadyState ReadSimReadyState(SimPlayer sim)
        {
            Character character = null;
            try { character = sim == null || sim.MyStats == null ? null : sim.MyStats.Myself; }
            catch { }
            bool available = IsAvailableSim(sim) && IsAvailableCharacter(character);
            bool aliveKnown = false;
            bool alive = false;
            if (available)
            {
                try { alive = character.Alive; aliveKnown = true; }
                catch { }
            }

            bool combatKnown = false;
            bool inCombat = false;
            if (available)
            {
                try { inCombat = sim.IsSimGroupInCombat(); combatKnown = true; }
                catch { }
                try
                {
                    NPC npc = character.MyNPC;
                    if (npc != null)
                    {
                        combatKnown = true;
                        if (npc.CurrentAggroTarget != null) inCombat = true;
                    }
                }
                catch { }
            }
            return ReadyStatePolicy.Classify(available, false, aliveKnown, alive, combatKnown, inCombat);
        }

        private static bool IsLocallyRollEligible(SimPlayer sim)
        {
            if (!IsAvailableSim(sim)) return false;
            if (CoopCompatibility.IsRemoteCoopHuman(sim) || CoopCompatibility.IsRemoteCoopSim(sim)) return false;
            return IsCurrentPartySim(sim);
        }

        private static SimPlayerTracking[] ReadGroupMembers()
        {
            try { return GameData.GroupMembers; }
            catch { return null; }
        }

        private static SimPlayer SafeAvatar(SimPlayerTracking tracking)
        {
            try { return tracking == null ? null : tracking.MyAvatar; }
            catch { return null; }
        }

        private static string ReadTrackingName(SimPlayerTracking tracking)
        {
            try { return tracking == null ? string.Empty : CleanDisplayName(tracking.SimName, string.Empty); }
            catch { return string.Empty; }
        }

        private static int ReadTrackingLevel(SimPlayerTracking tracking)
        {
            try { return tracking != null && tracking.Level > 0 ? tracking.Level : 0; }
            catch { return 0; }
        }

        private static string ReadTrackingClass(SimPlayerTracking tracking)
        {
            try { return tracking == null ? string.Empty : PartySnapshotPolicy.CleanClassName(tracking.ClassName); }
            catch { return string.Empty; }
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
            try { return character != null && character.gameObject != null && character.gameObject.activeInHierarchy; }
            catch { return false; }
        }

        private static string SafeDisplayName(SimPlayer sim, string fallback)
        {
            string name = ReadSimName(sim);
            return string.IsNullOrWhiteSpace(name) ? CleanDisplayName(fallback, "Party member") : name;
        }

        private static string ReadSimName(SimPlayer sim)
        {
            if (sim == null) return string.Empty;
            try
            {
                Character character = sim.MyStats == null ? null : sim.MyStats.Myself;
                NPC npc = character == null ? null : character.MyNPC;
                if (npc != null && !string.IsNullOrWhiteSpace(npc.NPCName)) return CleanDisplayName(npc.NPCName, string.Empty);
            }
            catch { }
            try
            {
                if (sim.MyStats != null && !string.IsNullOrWhiteSpace(sim.MyStats.MyName))
                    return CleanDisplayName(sim.MyStats.MyName, string.Empty);
            }
            catch { }
            return string.Empty;
        }

        private static string CleanDisplayName(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            string clean = value.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ').Replace('\0', ' ').Trim();
            if (clean.Length == 0) return fallback;
            return clean.Length <= 48 ? clean : clean.Substring(0, 48);
        }
    }
}
