using System;
using System.Collections.Generic;

namespace ErenshorPartyTools
{
    internal enum FriendAvailabilityState
    {
        Available,
        Busy,
        Offline
    }

    // Pure deterministic availability model. It deliberately has no Unity, game-save,
    // or scene-object dependency so a friend's result survives restart and zoning.
    internal static class FriendAvailability
    {
        internal const int DefaultSessionHours = 3;

        internal static List<string> ParseConfiguredFriends(string configuredNames)
        {
            List<string> result = new List<string>();
            if (string.IsNullOrWhiteSpace(configuredNames)) return result;

            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string[] names = configuredNames.Split(',');
            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i] == null ? string.Empty : names[i].Trim();
                if (string.IsNullOrWhiteSpace(name) || name.Length > 80 || !seen.Add(name)) continue;
                result.Add(name);
            }
            return result;
        }

        internal static bool TryGetSimulatedState(
            string simIdentity,
            string persistentSeed,
            long sessionBlock,
            bool enabled,
            out FriendAvailabilityState state)
        {
            state = FriendAvailabilityState.Offline;
            string identity = NormalizeIdentity(simIdentity);
            string seed = NormalizeIdentity(persistentSeed);
            if (identity == null || seed == null) return false;
            if (!enabled)
            {
                state = FriendAvailabilityState.Available;
                return true;
            }

            // 0-34 OFFLINE, 35-59 BUSY, 60-99 AVAILABLE.
            uint roll = StableHash(seed + "|" + identity + "|" + sessionBlock.ToString()) % 100u;
            state = roll < 35u
                ? FriendAvailabilityState.Offline
                : roll < 60u ? FriendAvailabilityState.Busy : FriendAvailabilityState.Available;
            return true;
        }

        internal static FriendAvailabilityState ApplyVerifiedBusy(
            FriendAvailabilityState simulatedState,
            bool nativeBusy)
        {
            return nativeBusy ? FriendAvailabilityState.Busy : simulatedState;
        }

        internal static long GetSessionBlock(DateTime utcNow, int sessionHours)
        {
            int hours = sessionHours < 1 ? 1 : (sessionHours > 24 ? 24 : sessionHours);
            return utcNow.ToUniversalTime().Ticks / (TimeSpan.TicksPerHour * hours);
        }

        private static string NormalizeIdentity(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            string normalized = value.Trim();
            return normalized.Length == 0 || normalized.Length > 160 ? null : normalized.ToUpperInvariant();
        }

        private static uint StableHash(string value)
        {
            // FNV-1a: stable across .NET process restarts unlike string.GetHashCode().
            uint hash = 2166136261u;
            for (int i = 0; i < value.Length; i++)
            {
                hash ^= value[i];
                hash *= 16777619u;
            }
            return hash;
        }
    }
}
