using System;
using ErenshorPartyTools;

internal static class UiAndCommandPolicyTests
{
    internal static void Run()
    {
        int sides;
        Assert(PartyToolsCommandPolicy.TryParseSides("", 1000000, out sides) && sides == 100, "bare roll defaults to 100");
        Assert(PartyToolsCommandPolicy.TryParseSides("1", 1000000, out sides) && sides == 1, "minimum roll accepted");
        Assert(PartyToolsCommandPolicy.TryParseSides("1000000", 1000000, out sides) && sides == 1000000, "large maximum accepted");
        Assert(!PartyToolsCommandPolicy.TryParseSides("0", 1000000, out sides), "zero rejected");
        Assert(!PartyToolsCommandPolicy.TryParseSides("-1", 1000000, out sides), "negative rejected");
        Assert(!PartyToolsCommandPolicy.TryParseSides("1000001", 1000000, out sides), "above maximum rejected");
        Assert(!PartyToolsCommandPolicy.TryParseSides("999999999999999999999", 1000000, out sides), "overflow rejected");
        Assert(!PartyToolsCommandPolicy.TryParseSides("10 20", 1000000, out sides), "multi-token input rejected");
        Assert(PartyToolsCommandPolicy.TryParseSides("20", 1000000, out sides) && sides == 20 && PartyToolsCommandPolicy.TryParseSides("20", 1000000, out sides) && sides == 20, "repeated parse is stateless");

        Assert(!PartyToolsPanelPolicy.ShouldClose(false, false, "A", "B"), "closed panel stays closed");
        Assert(PartyToolsPanelPolicy.ShouldClose(true, false, "A", "A"), "not-ready closes open panel");
        Assert(PartyToolsPanelPolicy.ShouldClose(true, true, "A", "B"), "zone change closes panel");
        Assert(!PartyToolsPanelPolicy.ShouldClose(true, true, "A", "A"), "same zone keeps panel open");
        Assert(SuiteLauncherPolicy.RunSelfTests().StartsWith("PASS"), "launcher fallback policy");
        Assert(PartyToolsUiGeometry.RunSelfTests().StartsWith("PASS"), "retained geometry policy");
        Assert(ReadyCheckPresentation.Text(ReadyState.Ready) == "READY", "ready presentation");
        Assert(ReadyCheckPresentation.Text(ReadyState.Dead) == "DEAD", "dead presentation");
        Assert(ReadyCheckPresentation.Text(ReadyState.InCombat) == "IN COMBAT", "combat presentation");
        Assert(ReadyCheckPresentation.Text(ReadyState.Unavailable) == "UNAVAILABLE", "unknown/remote state does not invent ready");
        Console.WriteLine("UiAndCommandPolicyTests: PASS");
    }

    private static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
