using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace Whitney.WhitneyCode.PatchesNModels;

public static class WhitneyCardKeyWords
{
    [CustomEnum("Amplify")]
    [KeywordProperties(AutoKeywordPosition.None)]
    public static CardKeyword Amplify;
    [CustomEnum("Steal")]
    [KeywordProperties(AutoKeywordPosition.None)]
    public static CardKeyword Steal;
}

public static class WhitneyCardTags
{
    [CustomEnum]
    public static CardTag Spark;
}