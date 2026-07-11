using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Brennen.BrennenCode.Relics;

/// <summary>On kill: gain Block — "gg ez."</summary>
public sealed class GgBadge : BrennenRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(4, ValueProp.Unpowered)];

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (dealer != Owner?.Creature) return;
        if (target is null || !result.WasTargetKilled) return;
        if (Owner?.Creature is null) return;
        if (target.Side == Owner.Creature.Side) return;
        Flash();
        await CreatureCmd.GainBlock(
            Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Unpowered, null);
    }
}
