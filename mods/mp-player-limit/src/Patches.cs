using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Daily;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using MegaCrit.Sts2.Core.Nodes.Screens.DailyRun;
using MegaCrit.Sts2.Core.Runs;

namespace MpPlayerLimit;

/// <summary>
/// Steam / ENet host accept capacity. Vanilla always passes 4 from the host submenus.
/// </summary>
[HarmonyPatch(typeof(NetHostGameService), nameof(NetHostGameService.StartSteamHost))]
public static class StartSteamHostPatch
{
    public static void Prefix(ref int maxClients)
    {
        int before = maxClients;
        maxClients = MpLimitConfig.RewriteCapacity(maxClients);
        if (before != maxClients)
            MainFile.Logger.Info($"StartSteamHost capacity {before} → {maxClients}");
    }
}

[HarmonyPatch(typeof(NetHostGameService), nameof(NetHostGameService.StartENetHost))]
public static class StartENetHostPatch
{
    public static void Prefix(ref int maxClients)
    {
        int before = maxClients;
        maxClients = MpLimitConfig.RewriteCapacity(maxClients);
        if (before != maxClients)
            MainFile.Logger.Info($"StartENetHost capacity {before} → {maxClients}");
    }
}

/// <summary>
/// Lobby logic (join full check, slot assignment) uses MaxPlayers from the ctor.
/// </summary>
[HarmonyPatch(typeof(StartRunLobby), MethodType.Constructor,
    new[]
    {
        typeof(GameMode),
        typeof(INetGameService),
        typeof(IStartRunLobbyListener),
        typeof(int),
    })]
public static class StartRunLobbyCtorPatch
{
    public static void Prefix(ref int maxPlayers)
    {
        int before = maxPlayers;
        maxPlayers = MpLimitConfig.RewriteCapacity(maxPlayers);
        if (before != maxPlayers)
            MainFile.Logger.Info($"StartRunLobby MaxPlayers {before} → {maxPlayers}");
    }
}

[HarmonyPatch(typeof(StartRunLobby), MethodType.Constructor,
    new[]
    {
        typeof(GameMode),
        typeof(INetGameService),
        typeof(IStartRunLobbyListener),
        typeof(TimeServerResult),
        typeof(int),
    })]
public static class StartRunLobbyDailyCtorPatch
{
    public static void Prefix(ref int maxPlayers)
    {
        int before = maxPlayers;
        maxPlayers = MpLimitConfig.RewriteCapacity(maxPlayers);
        if (before != maxPlayers)
            MainFile.Logger.Info($"StartRunLobby (daily) MaxPlayers {before} → {maxPlayers}");
    }
}

/// <summary>
/// Character / custom host init pass maxPlayers into the lobby ctor (also patched above).
/// Belt-and-suspenders so logs show the screen-level rewrite too.
/// </summary>
[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.InitializeMultiplayerAsHost))]
public static class CharSelectHostInitPatch
{
    public static void Prefix(ref int maxPlayers)
    {
        maxPlayers = MpLimitConfig.RewriteCapacity(maxPlayers);
    }
}

[HarmonyPatch(typeof(NCustomRunScreen), nameof(NCustomRunScreen.InitializeMultiplayerAsHost))]
public static class CustomRunHostInitPatch
{
    public static void Prefix(ref int maxPlayers)
    {
        maxPlayers = MpLimitConfig.RewriteCapacity(maxPlayers);
    }
}

/// <summary>
/// Daily host hardcodes <c>new StartRunLobby(..., 4)</c> — ctor patch covers it.
/// Keep a postfix that forces MaxPlayers if private set ever bypasses the ctor arg.
/// </summary>
[HarmonyPatch(typeof(NDailyRunScreen), nameof(NDailyRunScreen.InitializeMultiplayerAsHost))]
public static class DailyRunHostInitPatch
{
    private static readonly FieldInfo? LobbyField =
        AccessTools.Field(typeof(NDailyRunScreen), "_lobby")
        ?? AccessTools.Field(typeof(NDailyRunScreen), "Lobby");

    private static readonly PropertyInfo? MaxPlayersProp =
        AccessTools.Property(typeof(StartRunLobby), nameof(StartRunLobby.MaxPlayers));

    public static void Postfix(NDailyRunScreen __instance)
    {
        try
        {
            StartRunLobby? lobby = null;
            if (LobbyField != null)
                lobby = LobbyField.GetValue(__instance) as StartRunLobby;

            // Some builds expose a public Lobby property.
            lobby ??= AccessTools.Property(typeof(NDailyRunScreen), "Lobby")
                ?.GetValue(__instance) as StartRunLobby;

            if (lobby == null)
                return;

            if (lobby.MaxPlayers == MpLimitConfig.VanillaMax
                && MaxPlayersProp?.SetMethod != null)
            {
                MaxPlayersProp.SetValue(lobby, MpLimitConfig.ClampedMax);
                MainFile.Logger.Info(
                    $"Daily lobby MaxPlayers forced → {MpLimitConfig.ClampedMax}");
            }
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Daily MaxPlayers force failed: {e.Message}");
        }
    }
}
