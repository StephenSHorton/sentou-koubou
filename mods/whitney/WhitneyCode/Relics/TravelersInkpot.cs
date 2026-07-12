using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using Whitney.WhitneyCode;
using Whitney.WhitneyCode.Powers;

namespace Whitney.WhitneyCode.Relics;

/// <summary>
/// Starter relic — open the pot at the start of each combat: +3 Ink and brush tracker.
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
        if (Owner is null || Owner.Creature is null)
            return;

        Flash();
        WhitneyBrush.Clear(Owner);
        await PowerCmd.Apply<BrushTrackerPower>(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            1,
            Owner.Creature,
            null);
        await Ink.Gain(new ThrowingPlayerChoiceContext(), Owner, DynamicVars["Ink"].IntValue);
    }
}
