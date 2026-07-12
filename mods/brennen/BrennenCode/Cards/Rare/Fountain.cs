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

namespace Brennen.BrennenCode.Cards.Rare;

public sealed class Fountain() : BrennenCard(2, CardType.Skill, CardRarity.Rare, TargetType.None)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [BrennenTips.Fed];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("HealPerFed", 3)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (Owner.Creature is null)
            return;
        var heal = DynamicVars["HealPerFed"].IntValue * Fed.Get(Owner);
        if (heal > 0)
            await CreatureCmd.Heal(Owner.Creature, heal);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["HealPerFed"].UpgradeValueBy(1m);
    }
}
