using BaseLib.Utils;
using Brennen.BrennenCode;
using Brennen.BrennenCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Brennen.BrennenCode.Cards.Uncommon;

public sealed class Shutdown() : BrennenCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [BrennenTips.Tilted, BrennenTips.Fed];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(9, ValueProp.Move),
        new DamageVar("TiltedDamage", 18, ValueProp.Move),
    ];

    protected override bool ShouldGlowGoldInternal => Tilted.IsTilted(Owner);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var dmg = Tilted.IsTilted(Owner)
            ? DynamicVars["TiltedDamage"].BaseValue
            : DynamicVars.Damage.BaseValue;
        var stored = DynamicVars.Damage.BaseValue;
        DynamicVars.Damage.BaseValue = dmg;
        try
        {
            await CommonActions.CardAttack(this, play).Execute(choiceContext);
        }
        finally
        {
            DynamicVars.Damage.BaseValue = stored;
        }

        if (Tilted.IsTilted(Owner) && Fed.IsFatal(play.Target))
            await Fed.Gain(choiceContext, Owner, 2, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars["TiltedDamage"].UpgradeValueBy(4m);
    }
}
