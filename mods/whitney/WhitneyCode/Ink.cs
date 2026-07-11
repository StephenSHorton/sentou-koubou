using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Whitney.WhitneyCode.Powers;

namespace Whitney.WhitneyCode;

/// <summary>Helpers for Whitney's Ink second mana.</summary>
public static class Ink
{
    public static int Get(Creature? creature)
    {
        if (creature is null)
            return 0;
        return creature.GetPower<InkPower>()?.Amount ?? 0;
    }

    public static int Get(Player? player) => Get(player?.Creature);

    public static async Task Gain(
        PlayerChoiceContext choiceContext,
        Player owner,
        int amount,
        CardModel? cardSource = null)
    {
        if (amount <= 0 || owner.Creature is null)
            return;

        await PowerCmd.Apply<InkPower>(
            choiceContext,
            owner.Creature,
            amount,
            owner.Creature,
            cardSource);
    }

    public static async Task<bool> TrySpend(
        PlayerChoiceContext choiceContext,
        Player owner,
        int amount,
        CardModel? cardSource = null)
    {
        if (amount <= 0)
            return true;
        if (owner.Creature is null)
            return false;

        var ink = owner.Creature.GetPower<InkPower>();
        if (ink is null || ink.Amount < amount)
            return false;

        await PowerCmd.ModifyAmount(choiceContext, ink, -amount, owner.Creature, cardSource);
        return true;
    }

    public static bool CanAfford(Player? owner, int amount)
    {
        if (amount <= 0)
            return true;
        return Get(owner) >= amount;
    }
}
