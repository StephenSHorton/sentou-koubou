using System.Threading.Tasks;
using Brennen.BrennenCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Brennen.BrennenCode;

/// <summary>Helpers for Brennen's Fed counter.</summary>
public static class Fed
{
    public static int Get(Player? player) =>
        player?.Creature?.GetPower<FedPower>()?.Amount ?? 0;

    public static async Task Gain(
        PlayerChoiceContext choiceContext,
        Player owner,
        int amount,
        CardModel? cardSource = null)
    {
        if (amount <= 0 || owner.Creature is null)
            return;

        // Bounty: double Fed gains while Tilted.
        if (Tilted.IsTilted(owner) && owner.Creature.GetPower<BountyPower>() is not null)
            amount *= 2;

        await PowerCmd.Apply<FedPower>(
            choiceContext, owner.Creature, amount, owner.Creature, cardSource);

        // Snowball: on Fed gain → Block + draw 1
        var snow = owner.Creature.GetPower<SnowballPower>();
        if (snow is not null && snow.Amount > 0)
        {
            await CreatureCmd.GainBlock(owner.Creature, snow.Amount, ValueProp.Unpowered, null);
            await CardPileCmd.Draw(choiceContext, 1, owner);
        }
    }
}
