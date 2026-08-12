using System;

namespace ErenshorPartyTools
{
    internal static class Roller
    {
        // Game-social utility only. A single process-local PRNG is appropriate here;
        // these rolls have no cryptographic, economic, or loot authority.
        private static readonly Random Random = new Random();

        internal static int Roll(int sides)
        {
            if (sides <= 1) return 1;
            return Random.Next(1, sides + 1);
        }
    }
}
