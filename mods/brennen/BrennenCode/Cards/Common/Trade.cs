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

namespace Brennen.BrennenCode.Cards.Common;

/// <summary>Self-damage trade: 10 dmg / 2 HP. Upgrade doubles both.</summary>
public sealed class Trade() : BrennenCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10, ValueProp.Move),
        new DynamicVar("HpLoss", 2),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        if (Owner.Creature is not null)
        {
            await CreatureCmd.Damage(
                choiceContext,
                [Owner.Creature],
                DynamicVars["HpLoss"].IntValue,
                ValueProp.Unblockable | ValueProp.Unpowered,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade()
    {
        // Double damage and self-damage: 10→20, 2→4.
        DynamicVars.Damage.UpgradeValueBy(10m);
        DynamicVars["HpLoss"].UpgradeValueBy(2m);
    }
}
