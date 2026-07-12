using Brennen.BrennenCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Brennen.BrennenCode;

/// <summary>
/// Tilted while at or below 50% max HP, or after cards/powers apply <see cref="TiltedPower"/>.
/// </summary>
public static class Tilted
{
    public static bool IsTilted(Creature? creature)
    {
        if (creature is null)
            return false;
        if (creature.GetPower<TiltedPower>() is not null)
            return true;
        if (creature.MaxHp <= 0)
            return false;
        return creature.CurrentHp * 2 <= creature.MaxHp;
    }

    public static bool IsTilted(Player? player) => IsTilted(player?.Creature);

    /// <summary>Apply the Tilted status for the rest of combat (idempotent).</summary>
    public static async Task Become(
        PlayerChoiceContext choiceContext,
        Player owner,
        CardModel? source = null)
    {
        if (owner.Creature is null)
            return;
        if (owner.Creature.GetPower<TiltedPower>() is not null)
            return;

        await PowerCmd.Apply<TiltedPower>(
            choiceContext,
            owner.Creature,
            1,
            owner.Creature,
            source);
    }
}
