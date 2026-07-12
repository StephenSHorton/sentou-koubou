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

namespace Brennen.BrennenCode.Cards.Basic;

/// <summary>Secure the kill — Fatal: Fed + draw. (Duo Queue also grants Fed on any enemy death.)</summary>
public sealed class LastHit() : BrennenCard(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [BrennenTips.Fed];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(8, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        if (Fed.IsFatal(play.Target))
        {
            // Duo Queue also grants +1 Fed on enemy death; Last Hit is the "secure CS" bonus:
            // extra Fed + draw so finishing with this card feels better than a random last hit.
            await Fed.Gain(choiceContext, Owner, 1, this);
            await CardPileCmd.Draw(choiceContext, 1, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
