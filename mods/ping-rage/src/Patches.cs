using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game.Flavor;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace PingRage;

/// <summary>
/// Faster local debounce + custom dialogue creation with random lines / rage FX.
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

            __instance._gameService.SendMessage(default(EndTurnPingMessage));
            __instance._nextAllowedPingTime = now + PingRageTracker.DebounceMsec;

            var player = __instance._playerCollection.GetPlayer(__instance._localPlayerId);
            if (player != null)
                PingDialogue.Create(player);

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
/// Remote (and any remaining vanilla path) dialogue: still use our funny bubbles.
/// </summary>
[HarmonyPatch(typeof(FlavorSynchronizer), "CreateEndTurnPingDialogueIfNecessary")]
public static class CreateEndTurnPingDialoguePatch
{
    public static bool Prefix(FlavorSynchronizer __instance, Player player)
    {
        try
        {
            _ = __instance;
            if (player == null)
                return false;
            PingDialogue.Create(player);
            return false;
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Create dialogue override failed, vanilla path: {e.Message}");
            return true;
        }
    }
}

internal static class PingDialogue
{
    public static void Create(Player player)
    {
        if (NRun.Instance == null || player == null)
            return;

        float rage = PingRageTracker.RegisterPing(player.NetId);
        string line = PingLines.Next();

        // Slightly longer on the screen when they're losing it.
        double seconds = 1.35 + rage * 1.4;

        // Free previous bubble for this player if still up.
        // FlavorSynchronizer keeps a dict — we free any child speech bubbles we tagged.
        FreeTaggedBubblesFor(player);

        var bubble = NSpeechBubbleVfx.Create(
            line,
            player.Creature,
            seconds,
            player.Character.SpeechBubbleColor);

        if (bubble == null)
            return;

        NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(bubble);

        // Tag for cleanup / identity
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
        // Scale: 1.0 → ~2.8 at full rage
        float scale = 1f + rage * 1.8f;
        bubble.Scale = Vector2.One * scale;

        // Start a bit snappier when enraged
        if (rage > 0.35f)
            bubble.Modulate = new Color(1f, 1f - rage * 0.15f, 1f - rage * 0.25f);

        Callable.From(() => AttachWiggle(bubble, rage)).CallDeferred();
    }

    private static void AttachWiggle(NSpeechBubbleVfx bubble, float rage)
    {
        if (bubble == null || !GodotObject.IsInstanceValid(bubble))
            return;

        // Kill old wiggles
        foreach (var child in bubble.GetChildren())
        {
            if (child is RageWiggle old)
                old.QueueFree();
        }

        if (rage < 0.08f)
            return;

        var wiggle = new RageWiggle
        {
            Name = "PingRageWiggle",
            Target = bubble,
            Intensity = rage,
            BasePosition = bubble.Position,
        };
        bubble.AddChild(wiggle);
    }
}
