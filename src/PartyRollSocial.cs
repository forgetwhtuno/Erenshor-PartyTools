using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ErenshorPartyTools
{
    // Optional cosmetic chat output. One party-roll action emits at most one summary line;
    // Party Tools never impersonates Sim dialogue or routes generated text through party commands.
    internal static class PartyRollSocial
    {
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

        internal static string Summary(int sides, IList<PartyRollResult> results)
        {
            if (results == null || results.Count == 0) return string.Empty;
            StringBuilder builder = new StringBuilder();
            builder.Append("Party roll 1-").Append(sides.ToString(CultureInfo.InvariantCulture)).Append(": ");
            int written = 0;
            for (int i = 0; i < results.Count; i++)
            {
                PartyRollResult result = results[i];
                if (result == null || result.Participant == null) continue;
                if (written > 0) builder.Append(", ");
                builder.Append(SafeName(result.Participant.Name)).Append(' ')
                       .Append(result.Value.ToString(CultureInfo.InvariantCulture));
                written++;
            }
            if (written == 0) return string.Empty;

            PartyRollResult winner = SingleWinner(results);
            if (winner == null)
            {
                int best = int.MinValue;
                for (int i = 0; i < results.Count; i++)
                    if (results[i] != null && results[i].Participant != null && results[i].Value > best) best = results[i].Value;
                builder.Append(". Tie at ").Append(best.ToString(CultureInfo.InvariantCulture)).Append('.');
            }
            else
            {
                builder.Append(". Winner: ").Append(SafeName(winner.Participant.Name)).Append('.');
            }
            return builder.ToString();
        }

        private static string SafeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Party member";
            string clean = value.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ').Replace('\0', ' ').Trim();
            return clean.Length <= 48 ? clean : clean.Substring(0, 48);
        }
    }
}
