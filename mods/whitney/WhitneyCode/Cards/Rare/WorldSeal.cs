using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Whitney.WhitneyCode;
using Whitney.WhitneyCode.Powers;

namespace Whitney.WhitneyCode.Cards.Rare;

/// <summary>
/// Ink X-cost: spend all Ink. Deal (Base + Attunement) × Ink spent to ALL enemies.
/// </summary>
public sealed class WorldSeal() : WhitneyCard(1, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    public override WhitneyElement Element => WhitneyElement.Earth;

    /// <summary>Star-X: game spends all current Ink and fills <see cref="CardModel.LastStarsSpent"/>.</summary>
    public override bool HasStarCostX => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        WhitneyTips.Ink,
        WhitneyTips.Attunement,
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(2, ValueProp.Move)];

    protected override bool ShouldGlowGoldInternal =>
        Ink.Get(Owner) > 0;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // Prefer LastStarsSpent (auto star-X); fall back if the pipeline left it 0.
        var spent = LastStarsSpent > 0 ? LastStarsSpent : Ink.Get(Owner);
        if (spent <= 0)
        {
            NoteBrushPlay();
            return;
        }

        // If star-X didn't auto-spend (edge case), dump remaining ink.
        if (LastStarsSpent <= 0 && Ink.Get(Owner) > 0)
            await Ink.TrySpend(choiceContext, Owner, Ink.Get(Owner), this);

        var attune = Owner.Creature?.GetPower<AttunementPower>()?.Amount ?? 0;
        var perInk = DynamicVars.Damage.BaseValue + attune;
        var dmg = perInk * spent;

        if (Owner.Creature?.CombatState is not null && dmg > 0)
        {
            await CreatureCmd.Damage(
                choiceContext,
                Owner.Creature.CombatState.HittableEnemies,
                dmg,
                ValueProp.Move,
                Owner.Creature,
                this);
        }
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}
