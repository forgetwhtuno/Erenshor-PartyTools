using System;
using System.Security.Cryptography;

namespace ErenshorPartyTools
{
    internal static class Roller
    {
        // Cosmetic rolls still deserve exact bounds without modulo bias. Use the framework RNG
        // with rejection sampling; these values never control game loot, XP, currency or authority.
        private static readonly object RandomLock = new object();
        private static readonly byte[] Buffer = new byte[4];
        private static RandomNumberGenerator _random;

        static Roller()
        {
            Initialize();
        }

        internal static void Initialize()
        {
            lock (RandomLock)
            {
                if (_random != null) return;
                _random = RandomNumberGenerator.Create();
            }
        }

        internal static void Shutdown()
        {
            lock (RandomLock)
            {
                if (_random != null)
                {
                    try { _random.Dispose(); } catch { }
                    _random = null;
                }
            }
        }

        internal static int Roll(int sides)
        {
            if (sides <= 1) return 1;

            ulong range = (ulong)(uint)sides;
            ulong sampleSpace = ((ulong)uint.MaxValue) + 1UL;
            ulong limit = sampleSpace - (sampleSpace % range);
            uint sample;
            do
            {
                lock (RandomLock)
                {
                    if (_random == null) _random = RandomNumberGenerator.Create();
                    _random.GetBytes(Buffer);
                    sample = BitConverter.ToUInt32(Buffer, 0);
                }
            }
            while ((ulong)sample >= limit);

            return (int)(((ulong)sample % range) + 1UL);
        }
    }
}
