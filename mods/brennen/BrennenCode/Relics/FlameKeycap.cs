using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Brennen.BrennenCode.Relics;

/// <summary>First Attack each turn gains Vigor — "All-chat flamer."</summary>
public sealed class FlameKeycap : BrennenRelic
{
    private bool _usedThisTurn;

    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Vigor", 3)];

    public override async Task AfterSideTurnStart(
        MegaCrit.Sts2.Core.Combat.CombatSide side,
        IReadOnlyList<MegaCrit.Sts2.Core.Entities.Creatures.Creature> participants,
        MegaCrit.Sts2.Core.Combat.ICombatState combatState)
    {
        if (Owner?.Creature is null) return;
        if (!participants.Contains(Owner.Creature)) return;
        _usedThisTurn = false;
        await Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (_usedThisTurn) return;
        if (cardPlay.Card.Owner != Owner) return;
        if (cardPlay.Card.Type != CardType.Attack) return;
        if (Owner?.Creature is null) return;
        _usedThisTurn = true;
        Flash();
        await PowerCmd.Apply<VigorPower>(
            choiceContext, Owner.Creature, DynamicVars["Vigor"].IntValue, Owner.Creature, null);
    }
}
