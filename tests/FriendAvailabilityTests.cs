using System;
using System.Collections.Generic;
using ErenshorPartyTools;

internal static class FriendAvailabilityTests
{
    internal static void Run()
    {
        SameIdentityAndBlockIsStable();
        ANewBlockCanChange();
        VerifiedBusyOverridesSimulation();
        UnknownNativeStateDoesNotInventBusy();
        StableNameSurvivesObjectRecreation();
        NonFriendsAndInvalidNamesAreExcluded();
        DisabledAvailabilityDoesNotFilter();
        NativeRosterUsesCurrentCharacterBinding();
        NativeAvailabilityUsesOnlineAndGrouped();
        Console.WriteLine("FriendAvailabilityTests: PASS");
    }

    private static void SameIdentityAndBlockIsStable()
    {
        FriendAvailabilityState first;
        FriendAvailabilityState second;
        Assert(FriendAvailability.TryGetSimulatedState("Phanty", "test-seed", 42L, true, out first), "valid identity accepted");
        Assert(FriendAvailability.TryGetSimulatedState("Phanty", "test-seed", 42L, true, out second), "repeat identity accepted");
        Assert(first == second, "same Sim and block must remain stable");
    }

    private static void ANewBlockCanChange()
    {
        FriendAvailabilityState first;
        Assert(FriendAvailability.TryGetSimulatedState("Fiora", "test-seed", 10L, true, out first), "first state accepted");
        bool changed = false;
        for (long block = 11L; block < 200L; block++)
        {
            FriendAvailabilityState next;
            FriendAvailability.TryGetSimulatedState("Fiora", "test-seed", block, true, out next);
            if (next != first) { changed = true; break; }
        }
        Assert(changed, "a later block should be able to produce another state");
    }

    private static void VerifiedBusyOverridesSimulation()
    {
        Assert(FriendAvailability.ApplyVerifiedBusy(FriendAvailabilityState.Available, true) == FriendAvailabilityState.Busy,
            "verified native busy must override simulated available");
    }

    private static void UnknownNativeStateDoesNotInventBusy()
    {
        Assert(FriendAvailability.ApplyVerifiedBusy(FriendAvailabilityState.Available, false) == FriendAvailabilityState.Available,
            "unknown native activity must not invent busy");
    }

    private static void StableNameSurvivesObjectRecreation()
    {
        FriendAvailabilityState before;
        FriendAvailabilityState after;
        FriendAvailability.TryGetSimulatedState("Dancer", "test-seed", 99L, true, out before);
        FriendAvailability.TryGetSimulatedState("dancer", "test-seed", 99L, true, out after);
        Assert(before == after, "stable name identity must not depend on a scene object");
    }

    private static void NonFriendsAndInvalidNamesAreExcluded()
    {
        List<string> friends = FriendAvailability.ParseConfiguredFriends(" Phanty, phanty, , Fiora ");
        Assert(friends.Count == 2, "only configured, unique friend names are included");
        FriendAvailabilityState ignored;
        Assert(!FriendAvailability.TryGetSimulatedState(null, "test-seed", 1L, true, out ignored), "missing identity fails conservatively");
        Assert(!FriendAvailability.TryGetSimulatedState("Phanty", string.Empty, 1L, true, out ignored), "missing seed fails conservatively");
    }

    private static void DisabledAvailabilityDoesNotFilter()
    {
        FriendAvailabilityState state;
        Assert(FriendAvailability.TryGetSimulatedState("Essek", "test-seed", 1L, false, out state), "disabled availability accepts valid identity");
        Assert(state == FriendAvailabilityState.Available, "disabled availability does not simulate offline or busy");
    }

    private static void NativeRosterUsesCurrentCharacterBinding()
    {
        Assert(NativeFriendRosterPolicy.IsCurrentCharacterFriend(2, 2, false),
            "matching FriendedBy and current slot should be a native friend");
        Assert(!NativeFriendRosterPolicy.IsCurrentCharacterFriend(1, 2, false),
            "another character's friend should be excluded");
        Assert(!NativeFriendRosterPolicy.IsCurrentCharacterFriend(2, 2, true),
            "GM characters should match the game's friend-filter exclusion");
    }

    private static void NativeAvailabilityUsesOnlineAndGrouped()
    {
        Assert(NativeFriendRosterPolicy.Availability(1, false) == FriendAvailabilityState.Available,
            "native online ungrouped friend should be available");
        Assert(NativeFriendRosterPolicy.Availability(1, true) == FriendAvailabilityState.Busy,
            "native online grouped friend should be busy");
        Assert(NativeFriendRosterPolicy.Availability(0, false) == FriendAvailabilityState.Offline,
            "native non-online friend should be offline");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
