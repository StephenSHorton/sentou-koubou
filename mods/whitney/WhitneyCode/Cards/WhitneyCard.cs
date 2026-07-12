using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Whitney.WhitneyCode.Character;
using Whitney.WhitneyCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace Whitney.WhitneyCode.Cards;

public enum WhitneyElement
{
    Fire,
    Water,
    Earth,
    Wind,
}

[Pool(typeof(WhitneyCardPool))]
public abstract class WhitneyCard(int cost, CardType type, CardRarity rarity, TargetType target) :
    CustomCardModel(cost, type, rarity, target)
{
    /// <summary>Elemental affinity for Blend / scripts / Masterwork.</summary>
    public virtual WhitneyElement Element => WhitneyElement.Fire;

    /// <summary>Ink (star) cost. Seals use SealCost &gt; 0.</summary>
    protected virtual int SealCost => 0;

    /// <summary>Alias of SealCost for older card code.</summary>
    protected virtual int InkCost => SealCost;

    /// <summary>Public seal cost for powers (Attunement, Eternal Quill, etc.).</summary>
    public int ResolvedSealCost => SealCost;

    /// <summary>True when this card spends Ink as a seal.</summary>
    public bool IsSeal => SealCost > 0;

    /// <summary>
    /// Maps seal Ink onto the game's star cost pipeline (auto-paid on play).
    /// Non-seals return <c>-1</c> (vanilla "no star cost") so the UI does not show 0 Ink.
    /// </summary>
    public override int CanonicalStarCost => SealCost > 0 ? SealCost : -1;

    /// <summary>Gold glow when the seal is ready (Ink can be paid).</summary>
    protected override bool ShouldGlowGoldInternal =>
        SealCost > 0 && Ink.CanAfford(Owner, SealCost);

    /// <summary>
    /// Whether playing this card would Blend given current brush state.
    /// Sample before side effects; call <see cref="NoteBrushPlay"/> at end of OnPlay.
    /// </summary>
    protected bool IsBlendActive => WhitneyBrush.IsBlend(Owner, Element);

    /// <summary>Register this play with the brush (end of OnPlay).</summary>
    protected void NoteBrushPlay() => WhitneyBrush.NotePlay(Owner, this);

    // Id.Entry e.g. WHITNEY-NOVICE_SEAL → noviceseal.png (files have no underscores).
    private string PortraitStem =>
        Id.Entry.RemovePrefix().Replace("_", "", StringComparison.Ordinal).ToLowerInvariant();

    public override string CustomPortraitPath => $"{PortraitStem}.png".BigCardImagePath();
    public override string PortraitPath => $"{PortraitStem}.png".CardImagePath();
    public override string BetaPortraitPath => $"beta/{PortraitStem}.png".CardImagePath();
}
