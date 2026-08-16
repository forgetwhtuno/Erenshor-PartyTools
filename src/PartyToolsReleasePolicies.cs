using System;
using System.Globalization;

namespace ErenshorPartyTools
{
    internal enum PartyWhoKind
    {
        LocalPlayer,
        LocalSim,
        RemotePlayer,
        RemoteSim,
        Unavailable
    }

    internal static class ReadyStatePolicy
    {
        // A ready row is only affirmative when every native fact needed for that answer is known.
        // Remote humans are intentionally identified as REMOTE rather than impersonated with a
        // ready/dead/combat answer that Party Tools does not authoritatively own.
        internal static ReadyState Classify(bool available, bool remotePlayer, bool aliveKnown, bool alive,
            bool combatKnown, bool inCombat)
        {
            if (remotePlayer) return ReadyState.RemotePlayer;
            if (!available || !aliveKnown) return ReadyState.Unavailable;
            if (!alive) return ReadyState.Dead;
            if (!combatKnown) return ReadyState.Unavailable;
            if (inCombat) return ReadyState.InCombat;
            return ReadyState.Ready;
        }
    }

    internal static class PartySnapshotPolicy
    {
        internal static PartyWhoKind Classify(bool isPlayer, bool hasLocalAvatar, bool remoteHuman, bool remoteSim, bool verifiedCurrentParty)
        {
            if (isPlayer) return PartyWhoKind.LocalPlayer;
            if (!hasLocalAvatar) return PartyWhoKind.Unavailable;
            if (remoteHuman) return PartyWhoKind.RemotePlayer;
            if (remoteSim) return PartyWhoKind.RemoteSim;
            if (!verifiedCurrentParty) return PartyWhoKind.Unavailable;
            return PartyWhoKind.LocalSim;
        }

        internal static string Text(PartyWhoKind kind)
        {
            switch (kind)
            {
                case PartyWhoKind.LocalPlayer: return "PLAYER";
                case PartyWhoKind.LocalSim: return "LOCAL SIM";
                case PartyWhoKind.RemotePlayer: return "REMOTE PLAYER";
                case PartyWhoKind.RemoteSim: return "REMOTE SIM";
                default: return "UNAVAILABLE";
            }
        }

        internal static string Describe(PartyWhoKind kind, int level, string className)
        {
            string identity = string.Empty;
            if (level > 0) identity = "L" + level.ToString(CultureInfo.InvariantCulture);
            string cleanClass = CleanClassName(className);
            if (cleanClass.Length > 0)
                identity = identity.Length == 0 ? cleanClass : identity + " " + cleanClass;
            string ownership = Text(kind);
            return identity.Length == 0 ? ownership : identity + "  •  " + ownership;
        }

        internal static string CleanClassName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            string clean = value.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ').Replace('\0', ' ').Trim();
            return clean.Length <= 32 ? clean : clean.Substring(0, 32);
        }
    }

    internal static class ReadyCheckSessionPolicy
    {
        internal const float DefaultLifetimeSeconds = 10f;

        internal static bool IsExpired(float startedAt, float now, float lifetimeSeconds)
        {
            if (float.IsNaN(startedAt) || float.IsInfinity(startedAt) || startedAt < 0f) return false;
            if (float.IsNaN(now) || float.IsInfinity(now)) return false;
            if (float.IsNaN(lifetimeSeconds) || float.IsInfinity(lifetimeSeconds) || lifetimeSeconds <= 0f) return false;
            return now - startedAt >= lifetimeSeconds;
        }
    }

    internal struct SuiteHubPresenceState
    {
        internal readonly bool Present;
        internal readonly bool Usable;
        internal readonly bool QuickCloseVerified;
        internal SuiteHubPresenceState(bool present, bool usable, bool quickCloseVerified)
        {
            Present = present;
            Usable = usable;
            QuickCloseVerified = quickCloseVerified;
        }
    }

    internal static class SuiteHubPresencePolicy
    {
        // Runtime endpoint presence is stronger evidence that Hub exists than a transient malformed
        // or throwing payload. Never reinterpret a live Hub function as "standalone" and start a
        // competing Escape poll merely because its describe call failed this second.
        internal static SuiteHubPresenceState FromEndpoint(bool endpointPresent, string payload)
        {
            if (!endpointPresent) return Bad();
            SuiteHubPresenceState parsed = Parse(payload);
            return parsed.Present ? parsed : new SuiteHubPresenceState(true, false, false);
        }

        internal static SuiteHubPresenceState Parse(string payload)
        {
            if (string.IsNullOrEmpty(payload) || payload.Length > 2048) return Bad();
            string protocol = null, module = null, status = null, uiAvailable = null, quickCloseContract = null, quickClose = null;
            string[] fields = payload.Split('&');
            for (int i = 0; i < fields.Length; i++)
            {
                int eq = fields[i].IndexOf('=');
                if (eq <= 0) return Bad();
                string key = fields[i].Substring(0, eq), value = fields[i].Substring(eq + 1);
                if (key == "protocol") { if (protocol != null) return Bad(); protocol = value; }
                else if (key == "module") { if (module != null) return Bad(); module = value; }
                else if (key == "status") { if (status != null) return Bad(); status = value; }
                else if (key == "uiAvailable") { if (uiAvailable != null) return Bad(); uiAvailable = value; }
                else if (key == "quickCloseContract") { if (quickCloseContract != null) return Bad(); quickCloseContract = value; }
                else if (key == "quickClose") { if (quickClose != null) return Bad(); quickClose = value; }
            }
            bool present = protocol == "1" && module == "suitehub" && status != null;
            bool ready = present && status == "Ready";
            bool usable = ready && string.Equals(uiAvailable, "true", StringComparison.OrdinalIgnoreCase);
            bool quickCloseVerified = ready && quickCloseContract == "1" && quickClose == "1";
            return new SuiteHubPresenceState(present, usable, quickCloseVerified);
        }
        private static SuiteHubPresenceState Bad() { return new SuiteHubPresenceState(false, false, false); }
    }

    internal static class SuiteUiStatePolicy
    {
        internal static string Build(string moduleId, bool open, int sortOrder, double activated)
        {
            if (string.IsNullOrEmpty(moduleId)) return string.Empty;
            if (sortOrder < -10000) sortOrder = -10000;
            if (sortOrder > 10000) sortOrder = 10000;
            if (double.IsNaN(activated) || double.IsInfinity(activated) || activated < 0d) activated = 0d;
            return "protocol=1&module=" + moduleId
                + "&open=" + (open ? "true" : "false")
                + "&closeable=true&sortOrder=" + sortOrder.ToString(CultureInfo.InvariantCulture)
                + "&activated=" + activated.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }

    internal enum SuiteEscapeAuthority
    {
        StandaloneFallback,
        ExplicitCloseControls,
        HubVerified
    }

    internal static class SuiteQuickCloseCompatibility
    {
        internal static SuiteEscapeAuthority Resolve(bool hubPresent, bool hubQuickCloseVerified, bool providerRegistered)
        {
            if (!hubPresent) return SuiteEscapeAuthority.StandaloneFallback;
            if (hubQuickCloseVerified && providerRegistered) return SuiteEscapeAuthority.HubVerified;
            return SuiteEscapeAuthority.ExplicitCloseControls;
        }

        internal static bool ShouldHandleEscapeLocally(bool open, bool hubPresent,
            bool hubQuickCloseVerified, bool providerRegistered)
        {
            return open && Resolve(hubPresent, hubQuickCloseVerified, providerRegistered) == SuiteEscapeAuthority.StandaloneFallback;
        }
    }
}
