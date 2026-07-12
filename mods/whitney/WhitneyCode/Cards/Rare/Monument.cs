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

public sealed class Monument() : WhitneyCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override WhitneyElement Element => WhitneyElement.Earth;
    protected override int SealCost => 3;
    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<InkPower>(),
        HoverTipFactory.FromPower<BarricadePower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(16, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        if (Owner.Creature is not null)
        {
            await PowerCmd.Apply<BarricadePower>(
                choiceContext, Owner.Creature, 1m, Owner.Creature, this);
        }
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(6m);
}
