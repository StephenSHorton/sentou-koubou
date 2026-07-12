using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Whitney.WhitneyCode.Relics;

[Pool(typeof(EventRelicPool))]
public class BreadOfAWashokuLover : AbstractWhitneyRelic
{
    public override RelicRarity Rarity => RelicRarity.Event;

    private int _triggers;

    public override string FlashSfx => "event:/sfx/ui/relic_activate_draw";

    public override bool ShowCounter => true;

    public override int DisplayAmount => Triggers;

    public override bool IsUsedUp => Triggers >= DynamicVars.Cards.IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(13),
        new HealVar(1),
        new MaxHpVar(13)
    ];

    [SavedProperty]
    public int Triggers
    {
        get => _triggers;
        set
        {
            AssertMutable();
            _triggers = value;
            UpdateDisplay();
        }
    }

    private void UpdateDisplay()
    {
        if (Triggers == DynamicVars.Cards.IntValue - 1)
        {
            Status = RelicStatus.Active;
        }
        else if (Triggers >= DynamicVars.Cards.IntValue)
        {
            Status = RelicStatus.Disabled;
        }
        else
        {
            Status = RelicStatus.Normal;
        }

        InvokeDisplayAmountChanged();
    }

    public override Task AfterObtained()
    {
        _triggers = 0;
        return base.AfterObtained();
    }

    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (card.Owner == Owner && card.Type is CardType.Curse or CardType.Status && !IsUsedUp)
        {
            await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
            Triggers++;
            if (Triggers >= DynamicVars.Cards.IntValue)
            {
                await CreatureCmd.GainMaxHp(Owner.Creature, DynamicVars.MaxHp.BaseValue);
            }
        }
    }
}