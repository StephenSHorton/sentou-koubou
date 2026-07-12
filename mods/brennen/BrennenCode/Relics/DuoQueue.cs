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

/// <summary>
/// Starter: whenever an enemy dies, gain 1 Fed. Also tracks turn play counts
/// for cards that care about attacks-this-turn.
/// </summary>
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

    /// <summary>
    /// Primary kill path — mirrors vanilla Gremlin Horn (AfterDeath), not AfterDamageGiven.
    /// AfterDamageGiven's dealer can be null for some card attacks, so kills were silently dropped.
    /// </summary>
    public override async Task AfterDeath(
        PlayerChoiceContext choiceContext,
        Creature creature,
        bool wasRemovalPrevented,
        float deathAnimLength)
    {
        if (wasRemovalPrevented)
            return;
        if (Owner?.Creature is null)
            return;
        // Only enemy deaths (not Brennen / allies).
        if (creature.Side == Owner.Creature.Side)
            return;

        Flash();
        await Fed.Gain(choiceContext, Owner, 1);
    }
}
