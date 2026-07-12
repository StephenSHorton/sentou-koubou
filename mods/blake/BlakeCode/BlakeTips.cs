using Blake.BlakeCode.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Blake.BlakeCode;

/// <summary>
/// Card hover-tip helpers for Blake keywords.
/// Pattern matches Whitney's <c>HoverTipFactory.FromPower&lt;InkPower&gt;()</c>:
/// tip-only (or real) powers with Localization, attached via ExtraHoverTips.
/// </summary>
public static class BlakeTips
{
    public static IHoverTip Charge => HoverTipFactory.FromPower<ChargePower>();
    public static IHoverTip Rev => HoverTipFactory.FromPower<RevPower>();
    public static IHoverTip Unleash => HoverTipFactory.FromPower<UnleashPower>();
    public static IHoverTip Sweetspot => HoverTipFactory.FromPower<SweetspotPower>();
    public static IHoverTip Combo => HoverTipFactory.FromPower<ComboPower>();
    public static IHoverTip FollowThrough => HoverTipFactory.FromPower<FollowThroughPower>();
    public static IHoverTip SuperArmor => HoverTipFactory.FromPower<SuperArmorPower>();
    public static IHoverTip Weak => HoverTipFactory.FromPower<WeakPower>();
}
