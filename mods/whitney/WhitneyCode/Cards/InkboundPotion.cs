using Whitney.WhitneyCode.Character;
using Whitney.WhitneyCode.Enchantments;
using Whitney.WhitneyCode.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;

namespace Whitney.WhitneyCode.Cards;

public class InkboundPotion : AbstractWhitneyCard
{
    public InkboundPotion() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    //public override string PortraitPath => "res://Whitney/images/cards/whitney-test_whitney_card.png";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromEnchantment<InkboundEnchantment>().Concat([HoverTipFactory.FromPower<InkboundPower>()]);

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(3)
    ];

    protected override void OnUpgrade()
    {
        //RemoveKeyword(CardKeyword.Exhaust);
        //EnergyCost.UpgradeBy(-1);
        //AddKeyword(CardKeyword.Retain);
        DynamicVars.Cards.UpgradeValueBy(2);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // var cards = Owner.PlayerCombatState!.Hand.Cards.ToArray();
        // foreach (var card in cards)
        // {
        //     var enchant = ModelDb.Enchantment<InkboundEnchantment>().ToMutable();
        //     if (enchant.CanEnchant(card))
        //         //Whitney.Enchant(enchant, card);
        //         CardCmd.Enchant(enchant, card, 1);
        // }
        //
        // await Cmd.Wait(0.25f);
        var enchantment = ModelDb.Enchantment<InkboundEnchantment>().ToMutable();
        var cards = await CardSelectCmd.FromHand(choiceContext,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 0, DynamicVars.Cards.IntValue),
            enchantment.CanEnchant,
            this);
        
        foreach (var card in cards)
        {
            var enchant = ModelDb.Enchantment<InkboundEnchantment>().ToMutable();
            if (enchant.CanEnchant(card))
                //Whitney.Enchant(enchant, card);
                CardCmd.Enchant(enchant, card, 1);
        }
        await Cmd.Wait(0.25f);
    }
}