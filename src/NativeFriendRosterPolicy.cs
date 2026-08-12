namespace ErenshorPartyTools
{
    // Mirrors GroupBuilder.FilterBy's native Friends predicate and the online
    // value used by SimPlayerMngr.SpawnSimsInZone. Kept pure for regression tests.
    internal static class NativeFriendRosterPolicy
    {
        internal static bool IsCurrentCharacterFriend(int friendedBy, int currentCharacterSlot, bool isGmCharacter)
        {
            return !isGmCharacter && currentCharacterSlot >= 0 && friendedBy == currentCharacterSlot;
        }

        internal static FriendAvailabilityState Availability(int nativeOnline, bool grouped)
        {
            if (nativeOnline != 1) return FriendAvailabilityState.Offline;
            return grouped ? FriendAvailabilityState.Busy : FriendAvailabilityState.Available;
        }
    }
}
