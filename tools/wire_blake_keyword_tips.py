"""Inject ExtraHoverTips for Blake custom keywords on all cards that need them."""
from pathlib import Path
import re

TIPS = {
    # Basic
    "RevUp": ["Rev", "Charge"],
    "Haymaker": ["Unleash", "Charge"],
    # Common
    "WarmEngine": ["Rev", "Charge"],
    "WindUp": ["Rev", "Charge"],
    "RaptorBoost": ["Rev", "Charge"],
    "GuardUp": ["Charge"],
    "OneTwo": ["Combo"],
    "DashAttack": ["Combo"],
    "ShoulderCheck": ["Sweetspot", "Weak"],
    "FalconDive": ["Rev", "Charge"],
    # Uncommon
    "FullThrottle": ["Rev", "Charge"],
    "Ignition": ["Rev", "Charge"],
    "Redline": ["Rev", "Charge"],
    "Slipstream": ["Rev", "Charge"],
    "PerfectShield": ["Rev", "Charge"],
    "Pressure": ["Charge"],
    "StoredPower": ["Charge"],
    "FalconKick": ["Unleash", "Charge"],
    "CleanKo": ["Unleash", "Charge"],
    "KneeOfJustice": ["Sweetspot"],
    "Yes": ["Unleash"],
    "WarmUpLap": ["Combo"],
    # Rare
    "FalconPunch": ["Unleash", "Charge", "FollowThrough"],
    "BlueFalcon": ["Charge"],
    "GDiffuser": ["Rev", "Charge"],
    "SuperArmor": ["Charge", "SuperArmor"],
    "ChampionsFist": ["Charge"],
    "MuscleMemory": ["Rev", "Unleash", "Charge"],
    "HighlightReel": ["Unleash"],
    "HeatHaze": ["Rev"],
    "HardRead": ["Sweetspot", "Stun"],
    "PhotoFinish": ["Unleash", "Charge", "Sweetspot"],
}

root = Path(__file__).resolve().parents[1] / "mods/blake/BlakeCode/Cards"
patched = 0
for path in sorted(root.rglob("*.cs")):
    if path.name == "BlakeCard.cs":
        continue
    stem = path.stem
    if stem not in TIPS:
        continue
    text = path.read_text(encoding="utf-8")
    if "ExtraHoverTips" in text:
        print(f"skip (already): {path.relative_to(root)}")
        continue

    tips = TIPS[stem]
    tips_expr = ",\n        ".join(f"BlakeTips.{t}" for t in tips)
    tip_block = f"""
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        {tips_expr},
    ];
"""

    # Ensure usings (parent namespace not imported automatically)
    if "using MegaCrit.Sts2.Core.HoverTips;" not in text:
        text = text.replace(
            "using MegaCrit.Sts2.Core.Entities.Cards;\n",
            "using MegaCrit.Sts2.Core.Entities.Cards;\nusing MegaCrit.Sts2.Core.HoverTips;\n",
            1,
        )
        if "using MegaCrit.Sts2.Core.HoverTips;" not in text:
            # fallback: after first using
            text = re.sub(
                r"(using [^\n]+;\n)",
                r"\1using MegaCrit.Sts2.Core.HoverTips;\n",
                text,
                count=1,
            )

    if "using Blake.BlakeCode;" not in text:
        text = text.replace(
            "using Blake.BlakeCode.Powers;\n",
            "using Blake.BlakeCode;\nusing Blake.BlakeCode.Powers;\n",
            1,
        )
        if "using Blake.BlakeCode;" not in text:
            text = re.sub(
                r"(using [^\n]+;\n)",
                r"\1using Blake.BlakeCode;\n",
                text,
                count=1,
            )

    # Insert after CanonicalKeywords / CanonicalVars / class open
    ck = re.search(r"public override IEnumerable<CardKeyword> CanonicalKeywords => [^;]+;\n", text)
    if ck:
        insert_at = ck.end()
    else:
        cv = re.search(
            r"protected override IEnumerable<DynamicVar> CanonicalVars =>[\s\S]*?;\n",
            text,
        )
        if cv:
            insert_at = cv.end()
        else:
            m = re.search(r"public sealed class \w+\(\)[^\n]*\n\{\n", text)
            if not m:
                print(f"FAIL class open: {path}")
                continue
            insert_at = m.end()

    text = text[:insert_at] + tip_block + text[insert_at:]
    path.write_text(text, encoding="utf-8")
    patched += 1
    print(f"patched {path.relative_to(root)}")

print(f"\nTotal patched: {patched}")
