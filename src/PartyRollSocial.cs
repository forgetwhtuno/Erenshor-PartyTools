using System;
using System.Collections.Generic;
using System.Globalization;

namespace ErenshorPartyTools
{
    // Cosmetic, local chat only. These lines never enter the party-command input path,
    // decide loot, or cause a Sim to take an action.
    internal static class PartyRollSocial
    {
        internal static string Opening(string playerName, int sides)
        {
            return SafeName(playerName, "You") + " tells the party: Alright everyone, roll 1-" +
                sides.ToString(CultureInfo.InvariantCulture) + "!";
        }

        internal static string Agreement(PartyRollTone tone)
        {
            switch (tone)
            {
                case PartyRollTone.Friendly: return "Sure, let's do it!";
                case PartyRollTone.Competitive: return "I'm in. Let's see who gets it.";
                case PartyRollTone.Blunt: return "Fine. Rolling.";
                case PartyRollTone.Rival: return "Try to beat this.";
                default: return "Sure, rolling.";
            }
        }

        internal static string Result(PartyRollResult result, int sides)
        {
            string name = result == null || result.Participant == null ? "A party member" : SafeName(result.Participant.Name, "A party member");
            int value = result == null ? 0 : result.Value;
            return name + " rolls " + value.ToString(CultureInfo.InvariantCulture) + " (1-" +
                sides.ToString(CultureInfo.InvariantCulture) + ").";
        }

        internal static PartyRollResult SingleWinner(IList<PartyRollResult> results)
        {
            if (results == null || results.Count == 0) return null;
            PartyRollResult winner = null;
            bool tied = false;
            for (int i = 0; i < results.Count; i++)
            {
                PartyRollResult candidate = results[i];
                if (candidate == null || candidate.Participant == null) continue;
                if (winner == null || candidate.Value > winner.Value)
                {
                    winner = candidate;
                    tied = false;
                }
                else if (candidate.Value == winner.Value)
                {
                    tied = true;
                }
            }
            return tied ? null : winner;
        }

        internal static string Winner(PartyRollTone tone)
        {
            switch (tone)
            {
                case PartyRollTone.Friendly: return "Yay, I won!";
                case PartyRollTone.Competitive: return "Knew I'd take that one.";
                case PartyRollTone.Blunt: return "I won. Nice.";
                case PartyRollTone.Rival: return "Of course I won.";
                default: return "Nice, I won that one.";
            }
        }

        private static string SafeName(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }
}
