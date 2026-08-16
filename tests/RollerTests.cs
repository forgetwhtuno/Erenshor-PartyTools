using System;
using ErenshorPartyTools;

internal static class RollerTests
{
    internal static void Run()
    {
        Assert(Roller.Roll(1) == 1, "one-sided roll is exactly one");
        Roller.Shutdown();
        Roller.Initialize();
        int afterInitialize = Roller.Roll(100);
        Assert(afterInitialize >= 1 && afterInitialize <= 100, "roll works after explicit reload lifecycle");
        Roller.Shutdown();
        int recreated = Roller.Roll(100);
        Assert(recreated >= 1 && recreated <= 100, "roll lazily recovers if invoked after shutdown");

        int[] sides = new int[] { 2, 100, 1000000 };
        for (int s = 0; s < sides.Length; s++)
        {
            for (int i = 0; i < 5000; i++)
            {
                int value = Roller.Roll(sides[s]);
                Assert(value >= 1 && value <= sides[s], "roll escaped requested bounds");
            }
        }
        Console.WriteLine("RollerTests: PASS");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
