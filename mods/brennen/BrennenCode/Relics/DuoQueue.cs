using System.Collections.Generic;
using System.Threading.Tasks;
using Brennen.BrennenCode;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Brennen.BrennenCode.Relics;

/// <summary>Starter: whenever you kill an enemy, gain 1 Fed. Tracks turn play counts.</summary>
public sealed class DuoQueue : BrennenRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    public override async Task BeforeHandDraw(
        Player player,
        PlayerChoiceContext choiceContext,
        ICombatState combatState)
    {
        if (player != Owner)
            return;
        BrennenTurnState.ResetTurn();
        await Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner)
            return;
        BrennenTurnState.OnCardPlayed(cardPlay.Card);
        await Task.CompletedTask;
    }

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner is null || dealer != Owner.Creature)
            return;
        if (target is null || !result.WasTargetKilled)
            return;
        if (Owner.Creature is null || target.Side == Owner.Creature.Side)
            return;
        Flash();
        await Fed.Gain(choiceContext, Owner, 1);
    }
}
