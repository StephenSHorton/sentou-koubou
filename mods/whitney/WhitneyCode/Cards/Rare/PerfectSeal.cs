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

public sealed class PerfectSeal() : WhitneyCard(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    public override WhitneyElement Element => WhitneyElement.Earth;
    protected override int SealCost => 4;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<InkPower>(),
        HoverTipFactory.FromPower<AttunementPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(26, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // Attunement already applies once via ModifyDamageAdditive for seals;
        // double attunement: add another Attunement amount manually.
        var attune = Owner.Creature?.GetPower<AttunementPower>()?.Amount ?? 0;
        if (attune > 0 && play.Target is not null)
        {
            await CreatureCmd.Damage(
                choiceContext, play.Target, attune, ValueProp.Unpowered, Owner.Creature, this);
        }
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(6m);
}
