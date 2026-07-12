using BaseLib.Abstracts;
using BaseLib.Utils;
using Whitney.WhitneyCode.PatchesNModels;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Whitney.WhitneyCode.Cards;

[Pool(typeof(WhitneyCardPool))]
public abstract class AbstractWhitneyCard : CustomCardModel
{
    public override string PortraitPath => $"res://Whitney/images/cards/{Id.Entry.ToLowerInvariant()}.png";

    public AbstractWhitneyCard(int energyCost, CardType type, CardRarity rarity, TargetType targetType, bool shouldShowInCardLibrary = true, bool autoAdd = true)
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary, autoAdd)
    {
    }

    public Task DoFlash()
    {
        if (NCombatRoom.Instance != null)
        {
            var hand = NCombatRoom.Instance.Ui.Hand;
            if (hand.GetCardHolder(this) is NHandCardHolder holder)
            {
                holder.Flash();
            }
        }

        return Task.CompletedTask;
    }
}