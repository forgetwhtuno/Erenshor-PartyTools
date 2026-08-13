using System;
using System.Globalization;

namespace ErenshorPartyTools
{
    internal static class PartyToolsCommandPolicy
    {
        internal static bool TryParseSides(string argument, int maximum, out int sides)
        {
            sides = 100;
            if (string.IsNullOrWhiteSpace(argument)) return true;
            string value = argument.Trim();
            if (value.IndexOfAny(new char[] { ' ', '\t', '\r', '\n' }) >= 0) return false;
            int parsed;
            if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out parsed)) return false;
            if (parsed < 1 || parsed > maximum) return false;
            sides = parsed; return true;
        }
    }

    internal static class PartyToolsPanelPolicy
    {
        internal static bool ShouldClose(bool open, bool gameplayReady, string originScene, string currentScene)
        {
            if (!open) return false;
            if (!gameplayReady) return true;
            return !string.Equals(originScene ?? string.Empty, currentScene ?? string.Empty, StringComparison.Ordinal);
        }
    }

    internal static class ReadyCheckPresentation
    {
        internal static string Text(ReadyState state)
        {
            switch (state)
            {
                case ReadyState.Dead: return "DEAD";
                case ReadyState.InCombat: return "IN COMBAT";
                case ReadyState.Unavailable: return "UNAVAILABLE";
                default: return "READY";
            }
        }
    }
}
