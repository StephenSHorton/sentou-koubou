using Godot;

namespace PingRage;

/// <summary>
/// Tracks how hard a player is mashing Ping.
/// Fast successive pings raise rage; idle time decays it.
/// </summary>
internal static class PingRageTracker
{
    /// <summary>Minimum gap between accepted pings (vanilla is 1000ms).</summary>
    public const ulong DebounceMsec = 160;

    /// <summary>Window for “this is still a mash combo”.</summary>
    private const float ComboWindowSec = 2.2f;

    private static readonly Dictionary<ulong, PlayerRage> ByPlayer = new();

    public static float RegisterPing(ulong playerNetId)
    {
        float now = Time.GetTicksMsec() / 1000f;
        if (!ByPlayer.TryGetValue(playerNetId, out var state))
        {
            state = new PlayerRage();
            ByPlayer[playerNetId] = state;
        }

        float dt = now - state.LastPingSec;
        if (state.LastPingSec <= 0f)
            dt = ComboWindowSec; // first ping = calm

        // Decay if they paused.
        if (dt > ComboWindowSec)
            state.Rage *= 0.15f;
        else if (dt > 1.0f)
            state.Rage *= 0.55f;

        // Faster mash → more rage. dt near Debounce is max gain.
        float speed = 1f - Mathf.Clamp(dt / ComboWindowSec, 0f, 1f);
        state.Rage = Mathf.Clamp(state.Rage + 0.12f + speed * 0.38f, 0f, 1f);
        state.LastPingSec = now;
        state.Streak++;

        return state.Rage;
    }

    public static float Peek(ulong playerNetId) =>
        ByPlayer.TryGetValue(playerNetId, out var s) ? s.Rage : 0f;

    private sealed class PlayerRage
    {
        public float Rage;
        public float LastPingSec;
        public int Streak;
    }
}
