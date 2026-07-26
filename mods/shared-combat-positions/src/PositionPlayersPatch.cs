using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace SharedCombatPositions;

/// <summary>
/// Replaces <see cref="NCombatRoom.PositionPlayersAndPets"/> with the vanilla layout
/// algorithm, except players are ordered by <see cref="RunState.GetPlayerSlotIndex"/>
/// (lobby/host order) instead of local-first.
/// </summary>
[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom.PositionPlayersAndPets))]
public static class PositionPlayersPatch
{
    /// <summary>Skip original; run shared-order layout.</summary>
    public static bool Prefix(List<NCreature> creatureNodes, float scaling, bool fullyCenterPlayers)
    {
        try
        {
            PositionPlayersAndPetsShared(creatureNodes, scaling, fullyCenterPlayers);
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"Shared layout failed, falling back to vanilla: {e}");
            return true; // run original
        }

        return false;
    }

    private sealed class PlayerAndPets
    {
        public required NCreature Player { get; init; }
        public List<NCreature> Pets { get; } = [];
        public int SlotIndex { get; init; }
    }

    private static void PositionPlayersAndPetsShared(
        List<NCreature> creatureNodes,
        float scaling,
        bool fullyCenterPlayers)
    {
        var list = new List<PlayerAndPets>();
        foreach (var creatureNode in creatureNodes)
        {
            if (!creatureNode.Entity.IsPlayer)
                continue;

            list.Add(new PlayerAndPets
            {
                Player = creatureNode,
                SlotIndex = GetSlotIndex(creatureNode.Entity),
            });
        }

        // Host/lobby order: same order as RunState.Players on every peer.
        list.Sort((a, b) => a.SlotIndex.CompareTo(b.SlotIndex));

        foreach (var creature in creatureNodes)
        {
            if (creature.Entity.IsPlayer)
                continue;

            var owner = creature.Entity.PetOwner;
            var group = list.FirstOrDefault(p => p.Player.Entity.Player == owner);
            group?.Pets.Add(creature);
        }

        float num = 960f / scaling;
        float num2 = 70f;
        int num3 = (int)Math.Ceiling(Math.Sqrt(list.Count));
        int num4 = (int)Math.Ceiling((double)list.Count / num3);
        float num5 = creatureNodes.Take(num3).Sum(n => n.Visuals.Bounds.Size.X);
        float num6 = num5 + (num3 - 1) * num2;
        float num7 = num5 * 0.33f;
        float num8 = num4 > 1 ? num7 / (num4 - 1) : 0f;
        float num9 = num4 > 1 ? 120f / (num4 - 1) : 0f;
        float num10;

        if (fullyCenterPlayers)
        {
            num10 = creatureNodes.First(c => c.Entity.IsPlayer).Visuals.Bounds.Size.X * -0.5f;
        }
        else
        {
            num10 = (num - num6) * 0.5f;
            num10 = Math.Max(num10, 150f);
            if (list.Count >= num3 * 2)
                num5 += num7;
            if (num10 + num6 > num)
            {
                num2 = (num - 150f - num5) / (num3 - 1);
                num6 = num5 + (num3 - 1) * num2;
                num10 = (num - num6) * 0.5f;
            }
        }

        for (int num11 = 0; num11 < num3; num11++)
        {
            float targetXPos = num10 + num8 * num11;
            for (int num12 = 0; num12 < num3; num12++)
            {
                int num13 = num11 * num3 + num12;
                if (num13 >= list.Count)
                    break;

                var playerAndPets = list[num13];
                var player = playerAndPets.Player;
                var pets = playerAndPets.Pets;

                player.Position = new Vector2(
                    0f - targetXPos - player.Visuals.Bounds.Size.X * 0.5f,
                    200f - num9 * num11);

                // Keep Necrobinder Osty special-case for the *local* player only
                // (vanilla ties this to local UI / Osty control).
                if (LocalContext.IsMe(player.Entity) && player.Entity.Player?.Character is Necrobinder)
                {
                    NCreature? osty = null;
                    for (int num14 = 0; num14 < pets.Count; num14++)
                    {
                        var nCreature = pets[num14];
                        if (nCreature.Entity.Monster is Osty)
                        {
                            osty = nCreature;
                            pets.RemoveAt(num14);
                            break;
                        }
                    }

                    PositionLocalPlayerOsty(ref targetXPos, player.Position.Y, player, osty);
                }

                float num15 = pets.Count > 1 ? player.Visuals.Bounds.Size.X / (pets.Count - 1) : 0f;
                for (int num16 = 0; num16 < pets.Count; num16++)
                {
                    var nCreature2 = pets[num16];
                    nCreature2.Position = new Vector2(
                        0f - targetXPos + 20f - num16 * num15 - nCreature2.Visuals.Bounds.Size.X * 0.5f,
                        player.Position.Y + 10f);
                }

                if (num11 > 0)
                {
                    playerAndPets.Player.Visuals.Modulate = new Color(0.5f, 0.5f, 0.5f);
                    foreach (var item2 in pets)
                        item2.Visuals.Modulate = new Color(0.5f, 0.5f, 0.5f);
                }

                targetXPos += playerAndPets.Player.Visuals.Bounds.Size.X + num2;
            }
        }

        // Draw order: first in list ends up on top (same as vanilla MoveChild(…, 0) loop).
        // With host-first slot order, the host is drawn in front on every peer.
        foreach (var item3 in list)
        {
            item3.Player.GetParent().MoveChildSafely(item3.Player, 0);
            for (int num17 = 0; num17 < item3.Pets.Count; num17++)
            {
                var nCreature3 = item3.Pets[num17];
                nCreature3.GetParent().MoveChildSafely(nCreature3, num17 + 1);
                if (!LocalContext.IsMe(item3.Player.Entity))
                    nCreature3.Visuals.Bounds.Visible = false;
            }
        }
    }

    private static void PositionLocalPlayerOsty(
        ref float targetXPos,
        float playerYPosition,
        NCreature player,
        NCreature? osty)
    {
        var position = player.Position;
        position.X = player.Position.X - 150f;
        player.Position = position;
        if (osty != null)
        {
            osty.Position = new Vector2(0f - targetXPos, playerYPosition)
                            + NCreature.GetOstyOffsetFromPlayer(osty.Entity);
        }

        targetXPos += 100f;
    }

    private static int GetSlotIndex(Creature creature)
    {
        var player = creature.Player;
        if (player == null)
            return int.MaxValue;

        try
        {
            var state = RunManager.Instance?.State;
            if (state != null)
                return state.GetPlayerSlotIndex(player);
        }
        catch
        {
            // fall through
        }

        // Stable fallback if run state is missing mid-transition.
        return unchecked((int)player.NetId);
    }
}
