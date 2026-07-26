using Godot;

namespace PingRage;

/// <summary>
/// Tracks how hard a player is mashing Ping.
/// Rage builds slowly; 1s without a ping zeros rage immediately.
/// </summary>
internal static class PingRageTracker
{
    /// <summary>Minimum gap between accepted pings (vanilla is 1000ms).</summary>
    public const ulong DebounceMsec = 160;

    /// <summary>After this idle gap, rage drops to zero immediately.</summary>
    public const float RageResetIdleSec = 1.0f;

    private static readonly Dictionary<ulong, PlayerRage> ByPlayer = new();

    public static float RegisterPing(ulong playerNetId)
    {
        float now = Time.GetTicksMsec() / 1000f;
        if (!ByPlayer.TryGetValue(playerNetId, out var state))
        {
            state = new PlayerRage();
            ByPlayer[playerNetId] = state;
        }

        float dt = state.LastPingSec <= 0f ? 999f : now - state.LastPingSec;

        // Immediate cool-down after ~1s without mashing.
        if (dt >= RageResetIdleSec)
            state.Rage = 0f;

        // Slow build: small base tick + modest speed bonus when mashing hard.
        // At ~160ms gaps, ~12–15 pings to full rage (was ~3–4 before).
        float speed = dt >= RageResetIdleSec
            ? 0f
            : 1f - Mathf.Clamp(dt / RageResetIdleSec, 0f, 1f);
        float gain = 0.035f + speed * 0.055f + speed * speed * 0.04f;
        state.Rage = Mathf.Clamp(state.Rage + gain, 0f, 1f);
        state.LastPingSec = now;
        state.Streak = dt >= RageResetIdleSec ? 1 : state.Streak + 1;

        return state.Rage;
    }

    public static float Peek(ulong playerNetId)
    {
        if (!ByPlayer.TryGetValue(playerNetId, out var s))
            return 0f;
        float now = Time.GetTicksMsec() / 1000f;
        if (s.LastPingSec > 0f && now - s.LastPingSec >= RageResetIdleSec)
            return 0f;
        return s.Rage;
    }

    private sealed class PlayerRage
    {
        public float Rage;
        public float LastPingSec;
        public int Streak;
    }
}
