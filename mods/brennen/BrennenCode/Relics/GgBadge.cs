using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Brennen.BrennenCode.Relics;

/// <summary>On enemy death: gain Block — "gg ez."</summary>
public sealed class GgBadge : BrennenRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(4, ValueProp.Unpowered)];

    public override async Task AfterDeath(
        PlayerChoiceContext choiceContext,
        Creature creature,
        bool wasRemovalPrevented,
        float deathAnimLength)
    {
        if (wasRemovalPrevented) return;
        if (Owner?.Creature is null) return;
        if (creature.Side == Owner.Creature.Side) return;
        Flash();
        await CreatureCmd.GainBlock(
            Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Unpowered, null);
    }
}
