using Godot;
using Whitney.WhitneyCode.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Whitney.WhitneyCode.Potions;

public class ShroomBrew : AbstractWhitneyPotion
{
    public override PotionRarity Rarity => PotionRarity.Rare;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("PowStr", 3),
        new DynamicVar("PowDex", 3),
        new CardsVar(2),
        new EnergyVar(2),
        new DynamicVar("PowRegen", 5),
        new DynamicVar("PowBuffer", 1),
        new DynamicVar("Coin", 70)
    ];

    public override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>(),
        HoverTipFactory.FromPower<BufferPower>(),
        HoverTipFactory.FromPower<RegenPower>()
    ];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        NCombatRoom.Instance?.PlaySplashVfx(Owner.Creature, new Color("83ebdf"));
        foreach (var item in await CardSelectCmd.FromHand(prefs: new CardSelectorPrefs(SelectionScreenPrompt, 1, 1), context: choiceContext, player: Owner, filter: null, source: this))
        {
            await CardCmd.Exhaust(choiceContext, item);
            switch (item.Type)
            {
                case CardType.Attack:
                    await PowerCmd.Apply<StrengthPower>(choiceContext, Owner.Creature, DynamicVars["PowStr"].BaseValue, Owner.Creature, null);
                    break;
                case CardType.Skill:
                    await PowerCmd.Apply<DexterityPower>(choiceContext, Owner.Creature, DynamicVars["PowDex"].BaseValue, Owner.Creature, null);
                    break;
                case CardType.Power:
                    await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
                    await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
                    break;
                case CardType.Curse:
                    await PowerCmd.Apply<BufferPower>(choiceContext, Owner.Creature, DynamicVars["PowBuffer"].BaseValue, Owner.Creature, null);
                    break;
                case CardType.Status:
                    await PowerCmd.Apply<RegenPower>(choiceContext, Owner.Creature, DynamicVars["PowRegen"].BaseValue, Owner.Creature, null);
                    break;
                case CardType.Quest:
                    await PowerCmd.Apply<RoyaltiesPower>(choiceContext, Owner.Creature, DynamicVars["Coin"].BaseValue, Owner.Creature, null);
                    break;
            }
        }
    }

    // protected override string GetImagePath()
    // {
    //     return GodotIconPath;
    // }
    //
    // protected override string GetOutlinePath()
    // {
    //     return GodotIconPath;
    // }
}