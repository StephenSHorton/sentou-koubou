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
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.ValueProps;

namespace Whitney.WhitneyCode.Cards;

public class MagicalR360 : AbstractWhitneyCard
{
    public MagicalR360() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    //no art yet
    //public override string PortraitPath => "res://Whitney/images/cards/whitney-test_whitney_card.png";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(6, ValueProp.Move)
    ];

    public override bool GainsBlock => true;

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3);
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromEnchantment<InkboundEnchantment>().Concat([HoverTipFactory.FromPower<InkboundPower>()]);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        var zaEnchantment = ModelDb.Enchantment<InkboundEnchantment>().ToMutable();
        var cardModel =
            (await CardSelectCmd.FromHand(
                choiceContext,
                Owner,
                new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1),
                model => zaEnchantment.CanEnchant(model),
                this)
            ).FirstOrDefault();
        if (cardModel != null)
        {
            //Whitney.Enchant(zaEnchantment, cardModel);
            CardCmd.Enchant(zaEnchantment, cardModel, 1);
        }
    }
}