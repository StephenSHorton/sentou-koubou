using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Whitney.WhitneyCode.Cards;

namespace Whitney.WhitneyCode.Relics;

/// <summary>When you play a Seal, gain Block.</summary>
public sealed class MossCharm : WhitneyRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(3, ValueProp.Unpowered)];

    public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner) return;
        if (Owner?.Creature is null) return;
        if (cardPlay.Card is not WhitneyCard { IsSeal: true }) return;

        Flash();
        await CreatureCmd.GainBlock(
            Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Unpowered, cardPlay, false);
    }
}
