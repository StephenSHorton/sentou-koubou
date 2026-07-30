using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Runs;

namespace MpDropOut;

/// <summary>
/// Who still needs to act in multiplayer waits.
/// Uses <see cref="RunLobby.ConnectedPlayerIds"/> (vanilla already removes leavers there).
/// </summary>
public static class DropOutUtil
{
    public static RunLobby? Lobby => RunManager.Instance?.RunLobby;

    public static RunState? State => RunManager.Instance?.State;

    /// <summary>
    /// True if this player still holds a connection and should block shared waits.
    /// Singleplayer / missing lobby → everyone participates (no-op).
    /// </summary>
    public static bool IsParticipating(ulong playerId)
    {
        RunLobby? lobby = Lobby;
        if (lobby == null)
            return true;
        return lobby.ConnectedPlayerIds.Contains(playerId);
    }

    public static bool IsParticipating(Player player) =>
        player != null && IsParticipating(player.NetId);

    /// <summary>
    /// Players that must still satisfy "all ready / all voted" style gates.
    /// Dead combatants are still listed but combat already auto-ends them; we keep them
    /// as non-blockers when disconnected.
    /// </summary>
    public static IEnumerable<Player> ParticipatingPlayers(RunState? state = null)
    {
        state ??= State;
        if (state == null)
            yield break;

        foreach (Player player in state.Players)
        {
            if (IsParticipating(player))
                yield return player;
        }
    }

    public static int ParticipatingCount(RunState? state = null) =>
        ParticipatingPlayers(state).Count();

}
