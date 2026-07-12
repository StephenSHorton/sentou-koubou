using BaseLib.Abstracts;
using Whitney.WhitneyCode.Character;
using Whitney.WhitneyCode.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;

namespace Whitney.WhitneyCode.Events;

public class HungryForMushroomsWhitney : CustomEventModel
{
    public override bool IsAllowed(IRunState runState)
    {
        return false;
    }

    public override string CustomInitialPortraitPath => ImageHelper.GetImagePath("events/hungry_for_mushrooms.png");

    public override LocString InitialDescription => L10NLookup("HUNGRY_FOR_MUSHROOMS.pages.INITIAL.description");

    private async Task BigMushroom()
    {
        await RelicCmd.Obtain<BigMushroom>(Owner!);
        SetEventFinished(L10NLookup("HUNGRY_FOR_MUSHROOMS.pages.BIG_MUSHROOM.description"));
    }

    private async Task FragrantMushroom()
    {
        await RelicCmd.Obtain<FragrantMushroom>(Owner!);
        SetEventFinished(L10NLookup("HUNGRY_FOR_MUSHROOMS.pages.FRAGRANT_MUSHROOM.description"));
    }

    private async Task PackThemAll()
    {
        await RelicCmd.Obtain<ShroomBag>(Owner!);
        SetEventFinished(L10NLookup("WHITNEY-HUNGRY_FOR_MUSHROOMS_WHITNEY.pages.PACK_THEM_ALL.description"));
    }

    private async Task BigShroomBag()
    {
        var relic = Owner!.Relics.FirstOrDefault(x => x is ShroomBag);
        await RelicCmd.Remove(relic!);
        await RelicCmd.Obtain<BigShroomBag>(Owner);

        SetEventFinished(L10NLookup("WHITNEY-HUNGRY_FOR_MUSHROOMS_WHITNEY.pages.PACK_THEM_ALL.description"));
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        EventOption option;
        if (Owner!.Character is not WhitneyCharacter)
        {
            option = new EventOption(this, null, "WHITNEY-HUNGRY_FOR_MUSHROOMS_WHITNEY.options.LOCKED");
        }
        else if (Owner.Relics.Any(x => x is ShroomBag))
        {
            option = new EventOption(this, BigShroomBag, "WHITNEY-HUNGRY_FOR_MUSHROOMS_WHITNEY.options.PACK_THEM_ALL",
                HoverTipFactory.FromRelic<BigShroomBag>()
            );
            //option = RelicOption<BigShroomBag>(BigShroomBag);
        }
        else
        {
            option = new EventOption(this, PackThemAll, "WHITNEY-HUNGRY_FOR_MUSHROOMS_WHITNEY.options.PACK_THEM_ALL",
                HoverTipFactory.FromRelic<ShroomBag>()
            );
            //option = RelicOption<ShroomBag>(PackThemAll);
        }

        return
        [
            RelicOption<BigMushroom>(BigMushroom),
            RelicOption<FragrantMushroom>(FragrantMushroom).ThatDoesDamage(15m),
            option
        ];
    }
}