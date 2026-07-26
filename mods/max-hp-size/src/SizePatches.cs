using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MaxHpSize;

/// <summary>
/// Formula (matches Workshop MaxHpSizeMod):
/// <c>scale = 1 + (maxHp - startingHp) / startingHp</c>, clamped to a minimum of 0.25.
/// Starting HP comes from the character definition; max HP is current run max.
/// </summary>
public static class SizeMath
{
    public const float MinScale = 0.25f;

    public static float CalculateScale(Creature creature)
    {
        int startingHp = creature.Player?.Character.StartingHp ?? 100;
        if (startingHp <= 0)
            startingHp = 100;

        int maxHp = creature.MaxHp;
        float scale = 1f + (maxHp - startingHp) / (float)startingHp;
        if (scale < MinScale)
            scale = MinScale;
        return scale;
    }

    public static void ApplyToCreature(Creature creature, double tweenDuration)
    {
        if (creature is not { IsPlayer: true })
            return;

        try
        {
            var room = NCombatRoom.Instance;
            var node = room?.GetCreatureNode(creature);
            if (node == null || !GodotObject.IsInstanceValid(node))
                return;

            float scale = CalculateScale(creature);
            node.ScaleTo(scale, tweenDuration);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Scale apply failed: {e.Message}");
        }
    }

    public static void ApplyAllPlayersInCombat(double tweenDuration)
    {
        var room = NCombatRoom.Instance;
        if (room == null)
            return;

        try
        {
            foreach (var nCreature in room.CreatureNodes)
            {
                var entity = nCreature.Entity;
                if (entity is not { IsPlayer: true })
                    continue;
                float scale = CalculateScale(entity);
                nCreature.ScaleTo(scale, tweenDuration);
            }
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Scale all players failed: {e.Message}");
        }
    }
}

/// <summary>When max HP changes mid-run, tween the combat sprite.</summary>
[HarmonyPatch(typeof(Creature), "SetMaxHpInternal")]
public static class MaxHpChangedPatch
{
    public static void Postfix(Creature __instance)
    {
        SizeMath.ApplyToCreature(__instance, tweenDuration: 1.0);
    }
}

/// <summary>On room enter, snap scale so combat loads at the right size.</summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterRoomEntered))]
public static class AfterRoomEnteredPatch
{
    public static void Postfix()
    {
        // Instant snap on room load (same as upstream).
        SizeMath.ApplyAllPlayersInCombat(tweenDuration: 0.0);
    }
}
