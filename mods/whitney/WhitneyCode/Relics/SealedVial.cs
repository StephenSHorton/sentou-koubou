using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using Whitney.WhitneyCode;

namespace Whitney.WhitneyCode.Relics;

/// <summary>Bonus Ink at combat start (stacks with Inkpot).</summary>
public sealed class SealedVial : WhitneyRelic
{
    public override RelicRarity Rarity => RelicRarity.Common;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Ink", 2)];

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not CombatRoom) return;
        if (Owner is null) return;
        Flash();
        await Ink.Gain(new ThrowingPlayerChoiceContext(), Owner, DynamicVars["Ink"].IntValue);
    }
}
