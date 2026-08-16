using System;
using System.Collections.Generic;
using ErenshorPartyTools;

internal static class PartyRollSocialTests
{
    internal static void Run()
    {
        SummaryIsSingleBoundedResultLine();
        HighestUntiedRollWins();
        TiedHighestRollHasNoSingleWinner();
        TieSummaryDoesNotInventWinner();
        Console.WriteLine("PartyRollSocialTests: PASS");
    }

    private static void SummaryIsSingleBoundedResultLine()
    {
        List<PartyRollResult> results = new List<PartyRollResult>
        {
            new PartyRollResult(new PartyRollParticipant("TestPlayer", true), 20),
            new PartyRollResult(new PartyRollParticipant("Phanty", false), 83)
        };
        string line = PartyRollSocial.Summary(100, results);
        Assert(line.Contains("Party roll 1-100") && line.Contains("TestPlayer 20") && line.Contains("Phanty 83"),
            "summary should include the range and each actual roll");
        Assert(line.Contains("Winner: Phanty."), "summary should identify the untied winner");
        Assert(line.IndexOf('\n') < 0 && line.IndexOf('\r') < 0, "one party roll should emit one chat line");
    }

    private static void HighestUntiedRollWins()
    {
        List<PartyRollResult> results = new List<PartyRollResult>
        {
            new PartyRollResult(new PartyRollParticipant("TestPlayer", true), 20),
            new PartyRollResult(new PartyRollParticipant("Phanty", false), 83)
        };
        Assert(ReferenceEquals(PartyRollSocial.SingleWinner(results), results[1]), "highest result should win");
    }

    private static void TiedHighestRollHasNoSingleWinner()
    {
        List<PartyRollResult> results = new List<PartyRollResult>
        {
            new PartyRollResult(new PartyRollParticipant("TestPlayer", true), 77),
            new PartyRollResult(new PartyRollParticipant("Phanty", false), 77),
            new PartyRollResult(new PartyRollParticipant("Dancer", false), 12)
        };
        Assert(PartyRollSocial.SingleWinner(results) == null, "highest tie should not invent one winner");
    }

    private static void TieSummaryDoesNotInventWinner()
    {
        List<PartyRollResult> results = new List<PartyRollResult>
        {
            new PartyRollResult(new PartyRollParticipant("TestPlayer", true), 77),
            new PartyRollResult(new PartyRollParticipant("Phanty", false), 77)
        };
        string line = PartyRollSocial.Summary(100, results);
        Assert(line.Contains("Tie at 77."), "tie summary should report the tied high result");
        Assert(!line.Contains("Winner:"), "tie summary must not invent a winner");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
