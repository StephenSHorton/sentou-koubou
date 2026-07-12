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


namespace Whitney.WhitneyCode.Cards.Uncommon;

public sealed class TideScript() : WhitneyCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override WhitneyElement Element => WhitneyElement.Water;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<TideScriptPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(2, ValueProp.Unpowered)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (Owner.Creature is not null)
            await PowerCmd.Apply<TideScriptPower>(
                choiceContext, Owner.Creature, DynamicVars.Block.BaseValue, Owner.Creature, this);
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(1m);
}
