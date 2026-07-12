using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using Whitney.WhitneyCode;
// ThrowingPlayerChoiceContext lives in Multiplayer namespace (imported above).

namespace Whitney.WhitneyCode.Relics;

/// <summary>
/// Starter relic — open the pot at the start of each combat.
/// </summary>
public sealed class TravelersInkpot : WhitneyRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Ink", 3)];

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not CombatRoom)
            return;
        if (Owner is null)
            return;

        Flash();
        await Ink.Gain(new ThrowingPlayerChoiceContext(), Owner, DynamicVars["Ink"].IntValue);
    }
}
