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
/// Scales with how empty the palette feels vs a design reference (not a hard Ink cap).
/// </summary>
public sealed class NegativeSpace() : WhitneyCard(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    public override WhitneyElement Element => WhitneyElement.Wind;
    protected override int SealCost => 1;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [WhitneyTips.Ink];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(4, ValueProp.Move),
        new DynamicVar("Palette", Ink.PaletteReference),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // After auto-paying 1 seal cost: hits = max(0, paletteRef - remaining ink).
        var empty = System.Math.Max(0, DynamicVars["Palette"].IntValue - Ink.Get(Owner));
        var per = DynamicVars.Damage.BaseValue;
        if (play.Target is not null && empty > 0)
        {
            await CreatureCmd.Damage(
                choiceContext, play.Target, per * empty, ValueProp.Move, Owner.Creature, this);
        }
        NoteBrushPlay();
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}
