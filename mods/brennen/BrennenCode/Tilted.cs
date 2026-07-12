using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;

namespace Brennen.BrennenCode;

/// <summary>Tilted while at or below 50% max HP.</summary>
public static class Tilted
{
    public static bool IsTilted(Creature? creature)
    {
        if (creature is null || creature.MaxHp <= 0)
            return false;
        return creature.CurrentHp * 2 <= creature.MaxHp;
    }

    public static bool IsTilted(Player? player) => IsTilted(player?.Creature);
}
