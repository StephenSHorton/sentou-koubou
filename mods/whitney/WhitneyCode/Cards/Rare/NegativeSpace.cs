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

public sealed class NegativeSpace() : WhitneyCard(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    public override WhitneyElement Element => WhitneyElement.Wind;
    protected override int SealCost => 1;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<InkPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(4, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // After auto-paying 1 seal cost, empty slots = MaxInk - remaining ink.
        var empty = Ink.MaxInk - Ink.Get(Owner);
        var hits = System.Math.Max(0, empty);
        var per = DynamicVars.Damage.BaseValue;
        if (play.Target is not null && hits > 0)
        {
            await CreatureCmd.Damage(
                choiceContext, play.Target, per * hits, ValueProp.Move, Owner.Creature, this);
        }
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}
