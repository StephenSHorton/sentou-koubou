using System.Threading.Tasks;
using Brennen.BrennenCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Brennen.BrennenCode;

/// <summary>
/// Brennen's permanent-combat snowball counter ("getting fed" in League terms).
/// Stacks never drop during combat. Scales cards like Farmed Up / Fountain and
/// is granted by Duo Queue (on enemy death), Last Hit (Fatal), Objectives, etc.
/// </summary>
public static class Fed
{
    public static int Get(Player? player) =>
        player?.Creature?.GetPower<FedPower>()?.Amount ?? 0;

    /// <summary>
    /// True if this creature is dead / dying. Prefer over bare <see cref="Creature.IsDead"/>
    /// right after an attack — some pipelines set HP before the IsDead flag settles.
    /// </summary>
    public static bool IsFatal(Creature? target) =>
        target is not null && (target.IsDead || target.CurrentHp <= 0);

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

        try
        {
            await PowerCmd.Apply<FedPower>(
                choiceContext,
                owner.Creature,
                amount,
                owner.Creature,
                cardSource);

            MainFile.Logger.Info(
                $"Fed +{amount} → total {Get(owner)} (source={cardSource?.Id.Entry ?? "relic/hook"})");
        }
        catch (System.Exception ex)
        {
            MainFile.Logger.Error($"Fed.Gain failed: {ex}");
            throw;
        }

        // Snowball: on Fed gain → Block + draw 1
        var snow = owner.Creature.GetPower<SnowballPower>();
        if (snow is not null && snow.Amount > 0)
        {
            await CreatureCmd.GainBlock(owner.Creature, snow.Amount, ValueProp.Unpowered, null);
            await CardPileCmd.Draw(choiceContext, 1, owner);
        }
    }
}
