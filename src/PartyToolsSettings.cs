using System;
using Lunaris.Config;

namespace ErenshorPartyTools
{
    // Loader-neutral ConfigEntry-style shim. Keeping the Value surface makes the Lunaris
    // migration mechanical and lets the existing call sites keep their proven access pattern.
    internal sealed class PartyToolsConfigEntry<T>
    {
        private readonly Func<T> _get;
        private readonly Action<T> _set;

        internal PartyToolsConfigEntry(Func<T> get, Action<T> set)
        {
            _get = get;
            _set = set;
        }

        internal T Value
        {
            get { return _get(); }
            set { _set(value); }
        }
    }

    internal sealed class PartyToolsSettings
    {
        public PartyToolsSettings() { }

        [Config("PanelOffsetX", "UI",
            "Persisted horizontal offset from Party Tools' default upper-right position. Updated when a Party Tools panel finishes moving.")]
        public float PanelOffsetX = 0f;

        [Config("PanelOffsetY", "UI",
            "Persisted vertical offset from Party Tools' default position below the normal upper-right minimap area. Updated when a Party Tools panel finishes moving.")]
        public float PanelOffsetY = 0f;

        [Config("OpenMenuKey", "UI",
            "Keyboard shortcut to toggle the Party Tools command menu. Use /tools if this conflicts with another mod.")]
        public UnityEngine.KeyCode OpenMenuKey = UnityEngine.KeyCode.F7;

        [Config("Enabled", "Roll Chatter",
            "Show local cosmetic party-roll chat: the player announces, local Sims acknowledge, every result is displayed, and one untied winner reacts. This does not control loot or gameplay.")]
        public bool RollChatterEnabled = true;

        [Config("Enabled", "FriendAvailability",
            "Compatibility fallback only: when false, manually configured fallback friends are shown as AVAILABLE unless native game state verifies they are grouped. The native current-character friend roster is always preferred.")]
        public bool FriendAvailabilityEnabled = true;

        [Config("SessionHours", "FriendAvailability",
            "Real-world availability block duration in hours (1-24). Default: 3.")]
        public int FriendAvailabilitySessionHours = FriendAvailability.DefaultSessionHours;

        [Config("Seed", "FriendAvailability",
            "Persistent Party Tools seed. Generated once when blank; do not change during a session unless intentionally changing simulated availability.")]
        public string FriendAvailabilitySeed = string.Empty;

        [Config("Friends", "FriendAvailability",
            "Compatibility fallback only: comma-separated Sim names used if the native current-character friend roster is temporarily unavailable.")]
        public string FriendAvailabilityFriends = string.Empty;
    }
}
