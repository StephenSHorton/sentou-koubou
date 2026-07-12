using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace Brennen.BrennenCode.Relics;

/// <summary>Start of combat Block — "Promo season."</summary>
public sealed class RankedBadge : BrennenRelic
{
    public override RelicRarity Rarity => RelicRarity.Common;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(6, ValueProp.Unpowered)];

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not CombatRoom) return;
        if (Owner?.Creature is null) return;
        Flash();
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Unpowered, null);
    }
}
