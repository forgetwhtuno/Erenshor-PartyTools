using System;
using System.Collections.Generic;
using ErenshorPartyTools;

internal static class PartyRollSocialTests
{
    internal static void Run()
    {
        OpeningIncludesPlayerAndRange();
        PersonalityTonesDiffer();
        HighestUntiedRollWins();
        TiedHighestRollHasNoSingleWinner();
        Console.WriteLine("PartyRollSocialTests: PASS");
    }

    private static void OpeningIncludesPlayerAndRange()
    {
        string line = PartyRollSocial.Opening("TestPlayer", 1000);
        Assert(line.Contains("TestPlayer") && line.Contains("1-1000"), "opening should identify player and range");
    }

    private static void PersonalityTonesDiffer()
    {
        Assert(PartyRollSocial.Agreement(PartyRollTone.Friendly) != PartyRollSocial.Agreement(PartyRollTone.Blunt),
            "friendly and blunt agreement should differ");
        Assert(PartyRollSocial.Winner(PartyRollTone.Competitive) != PartyRollSocial.Winner(PartyRollTone.Rival),
            "competitive and rival winner reactions should differ");
    }

    private static void HighestUntiedRollWins()
    {
        List<PartyRollResult> results = new List<PartyRollResult>
        {
            new PartyRollResult(new PartyRollParticipant("TestPlayer", true), 20),
            new PartyRollResult(new PartyRollParticipant("Phanty", false), 83)
        };
        Assert(ReferenceEquals(PartyRollSocial.SingleWinner(results), results[1]), "highest result should win");
        Assert(PartyRollSocial.Result(results[1], 100).Contains("Phanty rolls 83"), "result should include the real roll");
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

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
