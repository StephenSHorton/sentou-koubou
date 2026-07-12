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
/// Energy X-cost: gain Block equal to Block×X, and gain X Ink.
/// </summary>
public sealed class Monsoon() : WhitneyCard(-1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override WhitneyElement Element => WhitneyElement.Water;
    public override bool GainsBlock => true;

    protected override bool HasEnergyCostX => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [WhitneyTips.Ink];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(5, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var x = EnergyCost.CapturedXValue;
        if (x <= 0)
            x = Owner.PlayerCombatState?.Energy ?? 0;

        if (x > 0)
        {
            var block = DynamicVars.Block.BaseValue * x;
            // Temporarily scale block for CardBlock helpers.
            var stored = DynamicVars.Block.BaseValue;
            DynamicVars.Block.BaseValue = block;
            try
            {
                await CommonActions.CardBlock(this, play);
            }
            finally
            {
                DynamicVars.Block.BaseValue = stored;
            }

            await Ink.Gain(choiceContext, Owner, x, this);
        }
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(2m);
}
