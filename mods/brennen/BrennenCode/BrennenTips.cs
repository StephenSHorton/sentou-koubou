using Brennen.BrennenCode.Powers;
using MegaCrit.Sts2.Core.HoverTips;

namespace Brennen.BrennenCode;

/// <summary>
/// Card hover-tip helpers for Brennen keywords (Fed / Tilted).
/// Pattern matches Blake's tip-via-power approach.
/// </summary>
public static class BrennenTips
{
    public static IHoverTip Fed => HoverTipFactory.FromPower<FedPower>();
    public static IHoverTip Tilted => HoverTipFactory.FromPower<TiltedPower>();
}
