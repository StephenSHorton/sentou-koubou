using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using Whitney.WhitneyCode.Powers;

namespace Whitney.WhitneyCode;

/// <summary>
/// Card hover-tip helpers for Whitney keywords (Ink / Blend / Attunement / elements).
/// Pattern matches Blake/Brennen tip-via-power approach.
/// </summary>
public static class WhitneyTips
{
    public static IHoverTip Ink => HoverTipFactory.FromPower<InkPower>();
    public static IHoverTip Blend => HoverTipFactory.FromPower<BlendPower>();
    public static IHoverTip Attunement => HoverTipFactory.FromPower<AttunementPower>();
    public static IHoverTip Weak => HoverTipFactory.FromPower<WeakPower>();
    public static IHoverTip Vulnerable => HoverTipFactory.FromPower<VulnerablePower>();
    public static IHoverTip Barricade => HoverTipFactory.FromPower<BarricadePower>();
}
