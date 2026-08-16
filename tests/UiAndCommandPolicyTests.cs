using System;
using ErenshorPartyTools;

internal static class UiAndCommandPolicyTests
{
    internal static void Run()
    {
        int sides;
        Assert(PartyToolsCommandPolicy.TryParseSides("", 1000000, out sides) && sides == 100, "bare roll defaults to 100");
        Assert(PartyToolsCommandPolicy.TryParseSides("1", 1000000, out sides) && sides == 1, "minimum roll accepted");
        Assert(PartyToolsCommandPolicy.TryParseSides("100", 1000000, out sides) && sides == 100, "normal explicit roll accepted");
        Assert(PartyToolsCommandPolicy.TryParseSides("1000000", 1000000, out sides) && sides == 1000000, "large maximum accepted");
        Assert(!PartyToolsCommandPolicy.TryParseSides("0", 1000000, out sides), "zero rejected");
        Assert(!PartyToolsCommandPolicy.TryParseSides("-1", 1000000, out sides), "negative rejected");
        Assert(!PartyToolsCommandPolicy.TryParseSides("1000001", 1000000, out sides), "above maximum rejected");
        Assert(!PartyToolsCommandPolicy.TryParseSides("999999999999999999999", 1000000, out sides), "overflow rejected");
        Assert(!PartyToolsCommandPolicy.TryParseSides("10 20", 1000000, out sides), "multi-token input rejected");
        Assert(!PartyToolsCommandPolicy.TryParseSides("+1", 1000000, out sides), "signed positive input rejected consistently");
        Assert(!PartyToolsCommandPolicy.TryParseSides("1\t2", 1000000, out sides), "tab-separated extra argument rejected");
        Assert(PartyToolsCommandPolicy.TryParseSides("20", 1000000, out sides) && sides == 20 && PartyToolsCommandPolicy.TryParseSides("20", 1000000, out sides) && sides == 20, "repeated parse is stateless");

        Assert(!PartyToolsPanelPolicy.ShouldClose(false, false, "A", "B"), "closed panel stays closed");
        Assert(PartyToolsPanelPolicy.ShouldClose(true, false, "A", "A"), "not-ready closes open panel");
        Assert(PartyToolsPanelPolicy.ShouldClose(true, true, "A", "B"), "zone change closes panel");
        Assert(!PartyToolsPanelPolicy.ShouldClose(true, true, "A", "A"), "same zone keeps panel open");
        Assert(SuiteLauncherPolicy.RunSelfTests().StartsWith("PASS"), "launcher fallback policy");
        Assert(PartyToolsUiGeometry.RunSelfTests().StartsWith("PASS"), "retained geometry policy");
        Assert(PartyToolsPanelLayoutPolicy.NoResultOverlap(455f), "normal panel result list does not overlap its header");
        Assert(PartyToolsPanelLayoutPolicy.NoResultOverlap(340f), "small-screen panel result list does not overlap its header");
        Assert(!PartyToolsPanelLayoutPolicy.Resolve(340f).ShowFooter, "small-screen panel drops optional footer before crushing results");
        Assert(ReadyCheckPresentation.Text(ReadyState.Ready) == "READY", "ready presentation");
        Assert(ReadyCheckPresentation.Text(ReadyState.Dead) == "DEAD", "dead presentation");
        Assert(ReadyCheckPresentation.Text(ReadyState.InCombat) == "IN COMBAT", "combat presentation");
        Assert(ReadyCheckPresentation.Text(ReadyState.RemotePlayer) == "REMOTE", "remote human is identified without impersonating readiness");
        Assert(ReadyCheckPresentation.Text(ReadyState.Unavailable) == "UNAVAILABLE", "unknown state does not invent ready");

        Assert(ReadyStatePolicy.Classify(true, false, true, true, true, false) == ReadyState.Ready, "known alive out-of-combat actor is ready");
        Assert(ReadyStatePolicy.Classify(true, false, true, false, true, false) == ReadyState.Dead, "known dead actor is dead");
        Assert(ReadyStatePolicy.Classify(true, false, true, true, true, true) == ReadyState.InCombat, "known combat actor is in combat");
        Assert(ReadyStatePolicy.Classify(true, true, true, true, true, false) == ReadyState.RemotePlayer, "remote player ownership takes precedence over local readiness");
        Assert(ReadyStatePolicy.Classify(false, false, false, false, false, false) == ReadyState.Unavailable, "missing actor is unavailable");
        Assert(ReadyStatePolicy.Classify(true, false, true, true, false, false) == ReadyState.Unavailable, "unknown combat state cannot fabricate ready");


        Assert(ReadyCheckSessionPolicy.IsExpired(10f, 20f, ReadyCheckSessionPolicy.DefaultLifetimeSeconds), "ready session expires at bounded timeout");
        Assert(!ReadyCheckSessionPolicy.IsExpired(10f, 19.99f, ReadyCheckSessionPolicy.DefaultLifetimeSeconds), "ready session remains active before timeout");
        Assert(!ReadyCheckSessionPolicy.IsExpired(-1f, 500f, ReadyCheckSessionPolicy.DefaultLifetimeSeconds), "unset ready session never spuriously expires");

        Assert(PartySnapshotPolicy.Classify(true, true, false, false, true) == PartyWhoKind.LocalPlayer, "party snapshot identifies local player");
        Assert(PartySnapshotPolicy.Classify(false, true, false, false, true) == PartyWhoKind.LocalSim, "party snapshot identifies verified local Sim");
        Assert(PartySnapshotPolicy.Classify(false, true, true, false, true) == PartyWhoKind.RemotePlayer, "party snapshot identifies remote player when locally observable");
        Assert(PartySnapshotPolicy.Classify(false, true, false, true, true) == PartyWhoKind.RemoteSim, "party snapshot identifies remote Sim when locally observable");
        Assert(PartySnapshotPolicy.Classify(false, false, false, false, true) == PartyWhoKind.Unavailable, "missing local avatar fails snapshot closed");
        Assert(PartySnapshotPolicy.Classify(false, true, false, false, false) == PartyWhoKind.Unavailable, "unverified party membership fails snapshot closed");

        Assert(PartySnapshotPolicy.Describe(PartyWhoKind.LocalSim, 12, "Arcanist") == "L12 Arcanist  •  LOCAL SIM", "ptwho includes authoritative Sim level/class");
        Assert(PartySnapshotPolicy.Describe(PartyWhoKind.RemotePlayer, 0, string.Empty) == "REMOTE PLAYER", "ptwho remote player does not borrow Sim tracking identity");
        Assert(PartySnapshotPolicy.CleanClassName("Arcanist\nInjected") == "Arcanist Injected", "ptwho class text is single-line");

        SuiteHubPresenceState ordinaryHub = SuiteHubPresencePolicy.Parse("protocol=1&module=suitehub&status=Ready&uiAvailable=true&quickCloseContract=1&quickClose=0");
        SuiteHubPresenceState quickHub = SuiteHubPresencePolicy.Parse("protocol=1&module=suitehub&status=Ready&uiAvailable=true&quickCloseContract=1&quickClose=1");
        SuiteHubPresenceState presentNotReady = SuiteHubPresencePolicy.Parse("protocol=1&module=suitehub&status=Starting&uiAvailable=false&quickCloseContract=1&quickClose=0");
        Assert(ordinaryHub.Present && ordinaryHub.Usable && !ordinaryHub.QuickCloseVerified, "usable Hub does not imply quick-close");
        Assert(quickHub.Present && quickHub.Usable && quickHub.QuickCloseVerified, "quick-close requires verified Hub capability");
        Assert(presentNotReady.Present && !presentNotReady.Usable && !presentNotReady.QuickCloseVerified, "well-formed Hub presence is distinct from readiness");
        Assert(!SuiteHubPresencePolicy.Parse("protocol=1&module=suitehub&module=suitehub&status=Ready&uiAvailable=true").Present, "duplicate Hub field fails closed");
        SuiteHubPresenceState brokenLiveEndpoint = SuiteHubPresencePolicy.FromEndpoint(true, "broken");
        Assert(brokenLiveEndpoint.Present && !brokenLiveEndpoint.Usable && !brokenLiveEndpoint.QuickCloseVerified,
            "live but malformed Hub endpoint fails closed without becoming standalone");
        Assert(!SuiteHubPresencePolicy.FromEndpoint(false, "").Present, "missing Hub endpoint is standalone-unavailable");

        Assert(SuiteQuickCloseCompatibility.Resolve(false, false, false) == SuiteEscapeAuthority.StandaloneFallback,
            "genuinely unavailable Hub permits documented standalone Escape fallback");
        Assert(SuiteQuickCloseCompatibility.ShouldHandleEscapeLocally(true, false, false, false),
            "open standalone panel may use local Escape only when Hub is genuinely unavailable");
        Assert(!SuiteQuickCloseCompatibility.ShouldHandleEscapeLocally(false, false, false, false),
            "closed standalone panel never polls Escape");
        Assert(SuiteQuickCloseCompatibility.Resolve(true, false, true) == SuiteEscapeAuthority.ExplicitCloseControls,
            "healthy Hub without verified native consume uses explicit close controls");
        Assert(!SuiteQuickCloseCompatibility.ShouldHandleEscapeLocally(true, true, false, true),
            "Hub present without native consume disables Party Tools Escape polling");
        Assert(SuiteQuickCloseCompatibility.Resolve(true, true, false) == SuiteEscapeAuthority.ExplicitCloseControls,
            "Hub presence plus provider failure still avoids competing Escape ownership");
        Assert(!SuiteQuickCloseCompatibility.ShouldHandleEscapeLocally(true, true, true, false),
            "provider failure does not resurrect local Escape while Hub is present");
        Assert(SuiteQuickCloseCompatibility.Resolve(true, true, true) == SuiteEscapeAuthority.HubVerified,
            "verified Hub plus registered provider is the centralized Escape authority");
        Assert(!SuiteQuickCloseCompatibility.ShouldHandleEscapeLocally(true, true, true, true),
            "verified Hub ownership suppresses Party Tools local polling");
        Assert(!SuiteQuickCloseCompatibility.ShouldHandleEscapeLocally(true, presentNotReady.Present,
            presentNotReady.QuickCloseVerified, true), "present but not-ready Hub still prevents competing suite-level Escape polling");

        string uiState = SuiteUiStatePolicy.Build("partytools", true, 521, 4.25);
        Assert(Field(uiState, "module") == "partytools" && Field(uiState, "open") == "true" && Field(uiState, "closeable") == "true", "partytools ui.state wire");
        Assert(Field(uiState, "sortOrder") == "521" && Field(uiState, "activated") == "4.25", "partytools ui.state ordering/activation");
        Console.WriteLine("UiAndCommandPolicyTests: PASS");
    }

    private static string Field(string line, string key)
    {
        string[] pairs = (line ?? string.Empty).Split('&');
        for (int i = 0; i < pairs.Length; i++)
        {
            int eq = pairs[i].IndexOf('=');
            if (eq <= 0) continue;
            if (pairs[i].Substring(0, eq) == key) return pairs[i].Substring(eq + 1);
        }
        return string.Empty;
    }

    private static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
