using Whitney.WhitneyCode.Cards.Colorless;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Whitney.WhitneyCode.Potions;

public class BottledSpark : AbstractWhitneyPotion
{
    public override PotionRarity Rarity => PotionRarity.Common;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.Self;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];

    public override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<Spark>()];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (Owner.Creature.CombatState != null)
            await Spark.CreateInHand(Owner, DynamicVars.Cards.IntValue, Owner.Creature.CombatState);
    }

    // protected override string GetImagePath()
    // {
    //     return PotionIconPath;
    // }
    //
    // protected override string GetOutlinePath()
    // {
    //     return PotionOutlinePath;
    // }
}