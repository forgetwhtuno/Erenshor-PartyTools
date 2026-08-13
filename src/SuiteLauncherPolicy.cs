namespace ErenshorPartyTools
{
    internal static class SuiteLauncherPolicy
    {
        internal static bool ShouldShow(bool ready, bool hubAvailable, bool bridgeRegistered, bool preference)
        {
            return ready && (preference || !hubAvailable || !bridgeRegistered);
        }

        internal static string RunSelfTests()
        {
            if (ShouldShow(false, false, false, true)) return "FAIL launcher before ready";
            if (!ShouldShow(true, false, false, false)) return "FAIL launcher fallback no hub";
            if (!ShouldShow(true, true, false, false)) return "FAIL launcher fallback no bridge";
            if (ShouldShow(true, true, true, false)) return "FAIL launcher obey off with hub";
            if (!ShouldShow(true, true, true, true)) return "FAIL launcher obey on with hub";
            return "PASS partytools launcher policy";
        }
    }
}
