using Whitney.WhitneyCode.Character;
using Whitney.WhitneyCode.Enchantments;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace Whitney.WhitneyCode.Relics;

public class UnstableMagicTool : AbstractWhitneyRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromEnchantment<InkboundEnchantment>();

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(3)
    ];

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is CombatRoom)
        {
            Flash();
            var zaEnchantment = ModelDb.Enchantment<InkboundEnchantment>().ToMutable();
            var cards = PileType.Draw.GetPile(Owner).Cards.Where(x => zaEnchantment.CanEnchant(x)).ToList().StableShuffle(Owner.RunState.Rng.CombatCardSelection)
                .Take(DynamicVars.Cards.IntValue)
                .ToList();
            foreach (var card in cards)
            {
                //Whitney.Enchant(zaEnchantment, card);
                CardCmd.Enchant(zaEnchantment, card, 1);
                zaEnchantment = ModelDb.Enchantment<InkboundEnchantment>().ToMutable();
            }

            CardCmd.Preview(cards);
        }

        return Task.CompletedTask;
    }
}