namespace ErenshorPartyTools
{
    internal enum PartyToolsAction
    {
        ReadyCheck,
        Roll,
        PartyRoll,
        FriendAvailability
    }

    internal sealed class PartyRollParticipant
    {
        internal readonly string Name;
        internal readonly bool IsPlayer;

        internal PartyRollParticipant(string name, bool isPlayer)
        {
            Name = name;
            IsPlayer = isPlayer;
        }
    }

    internal sealed class PartyRollResult
    {
        internal readonly PartyRollParticipant Participant;
        internal readonly int Value;

        internal PartyRollResult(PartyRollParticipant participant, int value)
        {
            Participant = participant;
            Value = value;
        }
    }

    internal enum PartyRollTone
    {
        Neutral,
        Friendly,
        Competitive,
        Blunt,
        Rival
    }

    internal enum ReadyState
    {
        Ready,
        Dead,
        InCombat,
        Unavailable
    }

    internal sealed class ReadyRow
    {
        internal readonly string Name;
        internal readonly ReadyState State;

        internal ReadyRow(string name, ReadyState state)
        {
            Name = name;
            State = state;
        }
    }

    internal sealed class PanelRow
    {
        internal readonly string Name;
        internal readonly string Value;
        internal readonly bool Blocked;

        internal PanelRow(string name, string value, bool blocked)
        {
            Name = name;
            Value = value;
            Blocked = blocked;
        }
    }
}
