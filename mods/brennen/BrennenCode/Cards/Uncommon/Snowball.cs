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

public sealed class Snowball() : BrennenCard(1, CardType.Power, CardRarity.Uncommon, TargetType.None)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [BrennenTips.Fed];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Snowball", 4)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (Owner.Creature is not null)
        {
            await PowerCmd.Apply<SnowballPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars["Snowball"].IntValue,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Snowball"].UpgradeValueBy(2m);
    }
}
