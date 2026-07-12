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

public sealed class WorldSeal() : WhitneyCard(1, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    public override WhitneyElement Element => WhitneyElement.Earth;
    // X-cost seal: no fixed star cost; spend all Ink in OnPlay (min 4).
    protected override int SealCost => 0;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<InkPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(5, ValueProp.Move)];

    protected override bool IsPlayable =>
        base.IsPlayable && Ink.Get(Owner) >= 4;

    protected override bool ShouldGlowGoldInternal =>
        Ink.Get(Owner) >= 4;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var spent = Ink.Get(Owner);
        if (spent < 4)
            return;
        await Ink.TrySpend(choiceContext, Owner, spent, this);

        var dmg = DynamicVars.Damage.BaseValue * spent;
        if (Owner.Creature?.CombatState is not null)
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
