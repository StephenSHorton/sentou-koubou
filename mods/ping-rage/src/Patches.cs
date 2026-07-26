using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game.Flavor;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;

namespace PingRage;

/// <summary>
/// Faster local debounce + custom dialogue. Line + rage are chosen by the pinger
/// and broadcast so every client shows the same bubble.
/// </summary>
[HarmonyPatch(typeof(FlavorSynchronizer), nameof(FlavorSynchronizer.SendEndTurnPing))]
public static class SendEndTurnPingPatch
{
    public static bool Prefix(FlavorSynchronizer __instance)
    {
        try
        {
            ulong now = Time.GetTicksMsec();
            if (now < __instance._nextAllowedPingTime)
                return false;

            var player = __instance._playerCollection.GetPlayer(__instance._localPlayerId);
            if (player == null)
                return false;

            // Authoritative pick on the sender — remotes must not re-roll.
            int lineIndex = PingLines.NextIndex();
            float rage = PingRageTracker.RegisterPing(player.NetId);

            // Vanilla empty ping (events / other listeners) + our sync payload.
            __instance._gameService.SendMessage(default(EndTurnPingMessage));
            __instance._gameService.SendMessage(new PingRageBubbleMessage
            {
                lineIndex = lineIndex,
                rage = rage,
            });
            __instance._nextAllowedPingTime = now + PingRageTracker.DebounceMsec;

            // Local display immediately (we skip re-handling our own bubble message).
            PingDialogue.Create(player, lineIndex, rage);

            return false;
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"SendEndTurnPing override failed, vanilla path: {e.Message}");
            return true;
        }
    }
}

/// <summary>
/// Suppress vanilla / per-client random dialogue. Remotes build the bubble from
/// <see cref="PingRageBubbleMessage"/> so the phrase matches the pinger.
/// </summary>
[HarmonyPatch(typeof(FlavorSynchronizer), "CreateEndTurnPingDialogueIfNecessary")]
public static class CreateEndTurnPingDialoguePatch
{
    public static bool Prefix(FlavorSynchronizer __instance, Player player)
    {
        _ = __instance;
        _ = player;
        // Local already created in SendEndTurnPing; remotes use PingRageBubbleMessage.
        return false;
    }
}

/// <summary>Register bubble sync with the run net bus.</summary>
[HarmonyPatch(typeof(RunManager), "InitializeShared")]
public static class RunManagerInitPingRagePatch
{
    public static void Postfix(RunManager __instance)
    {
        try
        {
            PingRageSync.Attach(__instance.NetService);
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"PingRage sync attach failed: {e}");
        }
    }
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.CleanUp))]
public static class RunManagerCleanUpPingRagePatch
{
    public static void Prefix()
    {
        PingRageSync.Detach();
    }
}

/// <summary>Net handler: apply peer-authored line index + rage.</summary>
internal static class PingRageSync
{
    private static INetGameService? _net;
    private static ulong _localId;

    public static void Attach(INetGameService net)
    {
        Detach();
        _net = net;
        _localId = net.NetId;
        net.RegisterMessageHandler<PingRageBubbleMessage>(OnBubble);
        MainFile.Logger.Info("PingRage bubble sync attached (shared phrases + rage).");
    }

    public static void Detach()
    {
        if (_net == null)
            return;
        try
        {
            _net.UnregisterMessageHandler<PingRageBubbleMessage>(OnBubble);
        }
        catch
        {
            // ignore
        }

        _net = null;
    }

    private static void OnBubble(PingRageBubbleMessage msg, ulong senderId)
    {
        // Sender already created locally in SendEndTurnPing.
        if (senderId == _localId)
            return;

        try
        {
            Player? player = FindPlayer(senderId);
            if (player == null)
            {
                MainFile.Logger.Warn($"PingRage bubble: unknown sender {senderId}");
                return;
            }

            PingDialogue.Create(player, msg.lineIndex, msg.rage);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"PingRage remote bubble failed: {e.Message}");
        }
    }

    private static Player? FindPlayer(ulong netId)
    {
        try
        {
            // RunState.GetPlayer is the usual multiplayer lookup.
            Player? p = RunManager.Instance?.State?.GetPlayer(netId);
            if (p != null)
                return p;

            var players = RunManager.Instance?.State?.Players;
            if (players == null)
                return null;
            foreach (Player player in players)
            {
                if (player.NetId == netId)
                    return player;
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }
}

internal static class PingDialogue
{
    public static void Create(Player player, int lineIndex, float rage)
    {
        if (NRun.Instance == null || player == null)
            return;

        string line = PingLines.Get(lineIndex);

        // Slightly longer on the screen when they're losing it.
        double seconds = 1.35 + rage * 1.4;

        FreeTaggedBubblesFor(player);

        var bubble = NSpeechBubbleVfx.Create(
            line,
            player.Creature,
            seconds,
            player.Character.SpeechBubbleColor);

        if (bubble == null)
            return;

        NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(bubble);

        bubble.SetMeta("PingRage", true);
        bubble.SetMeta("PingRagePlayer", (long)player.NetId);

        ApplyRageVisuals(bubble, rage);
    }

    private static void FreeTaggedBubblesFor(Player player)
    {
        var room = NCombatRoom.Instance;
        var container = room?.CombatVfxContainer;
        if (container == null)
            return;

        foreach (var child in container.GetChildren())
        {
            if (child is not NSpeechBubbleVfx bubble)
                continue;
            if (!bubble.HasMeta("PingRage"))
                continue;
            if (bubble.HasMeta("PingRagePlayer")
                && (long)bubble.GetMeta("PingRagePlayer").AsInt64() == (long)player.NetId)
            {
                bubble.QueueFreeSafely();
            }
        }
    }

    private static void ApplyRageVisuals(NSpeechBubbleVfx bubble, float rage)
    {
        // Start at half vanilla size; grow hard as rage climbs (0.5 → ~2.6).
        float scale = 0.5f + rage * rage * 2.1f;
        bubble.Scale = Vector2.One * scale;

        if (rage > 0.25f)
            bubble.Modulate = new Color(1f, 1f - rage * 0.2f, 1f - rage * 0.35f);

        Callable.From(() => AttachWiggle(bubble, rage)).CallDeferred();
    }

    private static void AttachWiggle(NSpeechBubbleVfx bubble, float rage)
    {
        if (bubble == null || !GodotObject.IsInstanceValid(bubble))
            return;

        foreach (var child in bubble.GetChildren())
        {
            if (child is RageWiggle old)
                old.QueueFree();
        }

        float intensity = 0.12f + rage * rage * 1.6f + rage * 0.5f;
        var wiggle = new RageWiggle
        {
            Name = "PingRageWiggle",
            Target = bubble,
            Intensity = intensity,
            BasePosition = bubble.Position,
        };
        bubble.AddChild(wiggle);
    }
}
