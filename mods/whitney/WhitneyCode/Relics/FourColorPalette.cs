using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using Whitney.WhitneyCode.Powers;

namespace Whitney.WhitneyCode.Relics;

/// <summary>Combat start: Attunement — four pigments ready.</summary>
public sealed class FourColorPalette : WhitneyRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Attune", 2)];

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not CombatRoom) return;
        if (Owner?.Creature is null) return;
        Flash();
        await PowerCmd.Apply<AttunementPower>(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars["Attune"].IntValue,
            Owner.Creature,
            null);
    }
}
