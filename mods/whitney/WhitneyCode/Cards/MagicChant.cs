using Whitney.WhitneyCode.Character;
using Whitney.WhitneyCode.Enchantments;
using Whitney.WhitneyCode.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Whitney.WhitneyCode.Cards;

public class MagicChant : AbstractWhitneyCard
{
    public MagicChant() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromEnchantment<InkboundEnchantment>().Concat(
            [HoverTipFactory.FromPower<InkboundPower>()]);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1);
        List<CardModel> cardsIn = (from c in PileType.Draw.GetPile(Owner).Cards
            orderby c.Rarity, c.Id
            select c).ToList();
        var cardModel = (await CardSelectCmd.FromSimpleGrid(choiceContext, cardsIn, Owner, prefs)).FirstOrDefault();
        // var enchant = ModelDb.Enchantment<InkboundEnchantment>().ToMutable();
        if (cardModel != null)
        {
            // if (enchant.CanEnchant(cardModel))
            //     //Whitney.Enchant(enchant, cardModel);
            //     CardCmd.Enchant(enchant, cardModel, 1);
            if (cardModel.IsUpgradable)
                CardCmd.Upgrade(cardModel);
            await CardPileCmd.Add(cardModel, PileType.Hand);
        }
    }
}