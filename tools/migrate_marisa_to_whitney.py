#!/usr/bin/env python3
"""
Import STS2 MarisaMod architecture into mods/whitney, rethemed for Whitney ink/violet.

Source of mechanics: projects/STS2_MarisaMod (workshop Marisa character).
We keep Whitney build layout (csproj, Whitney/ asset root, flipbook combat) and
rename Starlit→Inkbound, marisamod→Whitney paths, Marisa→Whitney types.

Does NOT copy Marisa character portraits/spine as final art — UI frames + power/relic
icons are copied as temporary placeholders until Whitney art pass.
"""
from __future__ import annotations

import re
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MARISA = Path(r"C:\Users\4step\projects\STS2_MarisaMod")
WHITNEY_MOD = ROOT / "mods" / "whitney"
CODE_DST = WHITNEY_MOD / "WhitneyCode"
ASSET_DST = WHITNEY_MOD / "Whitney"
SCENES_DST = WHITNEY_MOD / "scenes"
MATERIALS_DST = WHITNEY_MOD / "Materials"
VFX_SCENES_DST = WHITNEY_MOD / "Scenes"  # Marisa capital-S Scenes (VFX scripts)

# Order matters for sequential replace
REPLACEMENTS: list[tuple[str, str]] = [
    # namespaces / assembly paths
    ("marisamod.Scenes.Vfx", "Whitney.Scenes.Vfx"),
    ("marisamod.Scripts.PatchesNModels", "Whitney.WhitneyCode.PatchesNModels"),
    ("marisamod.Scripts.Characters", "Whitney.WhitneyCode.Character"),
    ("marisamod.Scripts.Enchantments", "Whitney.WhitneyCode.Enchantments"),
    ("marisamod.Scripts.Potions", "Whitney.WhitneyCode.Potions"),
    ("marisamod.Scripts.Powers", "Whitney.WhitneyCode.Powers"),
    ("marisamod.Scripts.Relics", "Whitney.WhitneyCode.Relics"),
    ("marisamod.Scripts.Events", "Whitney.WhitneyCode.Events"),
    ("marisamod.Scripts.Nodes", "Whitney.WhitneyCode.Nodes"),
    ("marisamod.Scripts.Cards.Colorless", "Whitney.WhitneyCode.Cards.Colorless"),
    ("marisamod.Scripts.Cards", "Whitney.WhitneyCode.Cards"),
    ("marisamod.Scripts", "Whitney.WhitneyCode"),
    ("marisamod.Scenes", "Whitney.Scenes"),
    ("namespace marisamod", "namespace Whitney.WhitneyCode"),
    # res paths
    ("res://marisamod/", "res://Whitney/"),
    ("res://Materials/", "res://Materials/"),
    ("res://Scenes/", "res://Scenes/"),
    # type renames (longest first)
    ("AbstractMarisaEnchantment", "AbstractWhitneyEnchantment"),
    ("AbstractMarisaPotion", "AbstractWhitneyPotion"),
    ("AbstractMarisaPower", "AbstractWhitneyPower"),
    ("AbstractMarisaRelic", "AbstractWhitneyRelic"),
    ("AbstractMarisaCard", "AbstractWhitneyCard"),
    ("AbstractAmplifiedCard", "AbstractAmplifiedCard"),
    ("MarisaCardKeyWords", "WhitneyCardKeyWords"),
    ("MarisaCardTags", "WhitneyCardTags"),
    ("MarisaCardPool", "WhitneyCardPool"),
    ("MarisaPotionPool", "WhitneyPotionPool"),
    ("MarisaRelicPool", "WhitneyRelicPool"),
    ("MarisaCharacter", "Whitney"),
    ("MarisaCardTrailVfx", "WhitneyCardTrailVfx"),
    ("MarisaCardTrail", "WhitneyCardTrail"),
    ("NMarisaEnergyCounter", "NWhitneyEnergyCounter"),
    ("DefendMarisa", "DefendWhitney"),
    ("cookie_marisa", "cookie_whitney"),
    ("HungryForMushroomsMarisa", "HungryForMushroomsWhitney"),
    ("energy_marisa", "energy_whitney"),
    ("cardEnergyMarisa", "cardEnergyWhitney"),
    # Starlit → Inkbound (stars theme → ink)
    ("StarlitEnchantment", "InkboundEnchantment"),
    ("StarlitPower", "InkboundPower"),
    ("StarlitVfx", "InkboundVfx"),
    ("StarLit", "Inkbound"),
    ("Starlit", "Inkbound"),
    ("starlit", "inkbound"),
    ("STARLIT", "INKBOUND"),
    # id / loc prefixes
    ("MARISAMOD-", "WHITNEY-"),
    ("marisamod", "whitney"),
    ("MarisaMod", "Whitney"),
    ("[MarisaMod]", "[Whitney]"),
    ("Marisa", "Whitney"),
    ("marisa", "whitney"),
    ("MARISA", "WHITNEY"),
]


def apply_replacements(text: str) -> str:
    for old, new in REPLACEMENTS:
        text = text.replace(old, new)
    return text


def copy_tree_filtered(src: Path, dst: Path, *, exts: set[str] | None = None, skip_names: set[str] | None = None):
    skip_names = skip_names or set()
    if dst.exists():
        shutil.rmtree(dst)
    for path in src.rglob("*"):
        if path.is_dir():
            continue
        if path.name in skip_names:
            continue
        if path.suffix in {".uid", ".import"}:
            continue
        if exts is not None and path.suffix.lower() not in exts:
            continue
        rel = path.relative_to(src)
        out = dst / rel
        out.parent.mkdir(parents=True, exist_ok=True)
        if path.suffix.lower() in {".cs", ".tscn", ".tres", ".gdshader", ".json", ".cfg", ".md", ".txt"}:
            text = path.read_text(encoding="utf-8", errors="replace")
            out.write_text(apply_replacements(text), encoding="utf-8")
        else:
            shutil.copy2(path, out)


def rename_starlit_files(root: Path):
    # Rename files that still have starlit/marisa in names after content rewrite
    for path in sorted(root.rglob("*"), key=lambda p: len(str(p)), reverse=True):
        if not path.is_file():
            continue
        name = path.name
        new_name = name
        for a, b in [
            ("Starlit", "Inkbound"),
            ("starlit", "inkbound"),
            ("StarLit", "Inkbound"),
            ("DefendMarisa", "DefendWhitney"),
            ("MarisaCharacter", "Whitney"),
            ("Marisa", "Whitney"),
            ("marisa", "whitney"),
        ]:
            new_name = new_name.replace(a, b)
        if new_name != name:
            dest = path.with_name(new_name)
            dest.parent.mkdir(parents=True, exist_ok=True)
            path.rename(dest)


def main():
    if not MARISA.is_dir():
        raise SystemExit(f"Marisa mod not found: {MARISA}")

    # Preserve Whitney combat visuals + extensions before wipe
    preserve: dict[str, str] = {}
    for rel in [
        "Character/WhitneyCombatVisuals.cs",
        "Extensions/StringExtensions.cs",
    ]:
        p = CODE_DST / rel
        if p.exists():
            preserve[rel] = p.read_text(encoding="utf-8")

    print("Removing old WhitneyCode…")
    if CODE_DST.exists():
        shutil.rmtree(CODE_DST)
    CODE_DST.mkdir(parents=True)

    print("Copying Scripts → WhitneyCode…")
    copy_tree_filtered(
        MARISA / "Scripts",
        CODE_DST,
        exts={".cs"},
    )
    # Entry.cs → MainFile.cs content lives in Entry; keep both names mapped later
    entry = CODE_DST / "Entry.cs"
    if entry.exists():
        # MainFile-compatible initializer is patched after copy
        pass

    print("Copying VFX Scenes (C# + tscn)…")
    if (MARISA / "Scenes").exists():
        copy_tree_filtered(MARISA / "Scenes", VFX_SCENES_DST, exts={".cs", ".tscn", ".tres", ".gdshader"})

    print("Copying Materials…")
    if (MARISA / "Materials").exists():
        copy_tree_filtered(MARISA / "Materials", MATERIALS_DST)

    print("Copying marisamod scenes → Whitney/scenes…")
    scenes_src = MARISA / "marisamod" / "scenes"
    if scenes_src.exists():
        copy_tree_filtered(scenes_src, ASSET_DST / "scenes", exts={".cs", ".tscn", ".tres", ".gdshader"})

    print("Copying localization…")
    loc_src = MARISA / "marisamod" / "localization" / "eng"
    loc_dst = ASSET_DST / "localization" / "eng"
    if loc_dst.exists():
        shutil.rmtree(loc_dst)
    loc_dst.mkdir(parents=True)
    for f in loc_src.glob("*.json"):
        text = apply_replacements(f.read_text(encoding="utf-8", errors="replace"))
        # Thematic loc polish for ink
        text = text.replace("Charge-Up", "Saturate")
        text = text.replace("Charge Up", "Saturate")
        text = text.replace("[gold]Starlit[/gold]", "[gold]Inkbound[/gold]")
        text = text.replace("Inkbound.", "Inkbound.")
        # Character identity
        text = text.replace("The Ordinary Magician", "The Atelier Witch")
        text = text.replace(
            "The witch living in Forest of Magic.\\nSpecializes in light and heat magic.",
            "An atelier witch who paints seals in living ink.\\nEnergy fuels the brush; Amplify deepens the wash.",
        )
        text = text.replace(
            "The witch living in Forest of Magic.\nSpecializes in light and heat magic.",
            "An atelier witch who paints seals in living ink.\nEnergy fuels the brush; Amplify deepens the wash.",
        )
        text = text.replace("Marisa cards will now appear", "Whitney cards will now appear")
        text = text.replace("Marisa Cards", "Whitney Cards")
        # filename renames for enchantment keys
        out_name = f.name.replace("starlit", "inkbound")
        (loc_dst / out_name).write_text(text, encoding="utf-8")

    # Drop zhs for now (optional later)
    print("Copying UI frames + energy orbs (placeholders)…")
    ui_src = MARISA / "marisamod" / "images" / "ui"
    ui_dst = ASSET_DST / "images" / "ui"
    ui_dst.mkdir(parents=True, exist_ok=True)
    if ui_src.exists():
        for f in ui_src.iterdir():
            if f.suffix.lower() in {".png", ".gdshader", ".tres"} and f.is_file():
                name = f.name.replace("MRS", "WTN").replace("marisa", "whitney").replace("Marisa", "Whitney")
                shutil.copy2(f, ui_dst / name)

    # Power/relic/potion icons as temp placeholders
    for sub in ("powers", "relics", "potions", "cards"):
        src = MARISA / "marisamod" / "images" / sub
        dst = ASSET_DST / "images" / sub
        if not src.exists():
            continue
        dst.mkdir(parents=True, exist_ok=True)
        for f in src.rglob("*.png"):
            rel = f.relative_to(src)
            name = str(rel).replace("marisa", "whitney").replace("Marisa", "Whitney")
            name = name.replace("starlit", "inkbound").replace("Starlit", "Inkbound")
            out = dst / name
            out.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(f, out)

    # Card energy atlas bits if present
    for f in (MARISA / "images").rglob("*") if (MARISA / "images").exists() else []:
        if f.suffix.lower() == ".png" and ("energy" in f.name.lower() or "cookie" in f.name.lower()):
            out_name = f.name.replace("Marisa", "Whitney").replace("marisa", "whitney")
            out = ASSET_DST / "images" / out_name
            out.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(f, out)

    print("Renaming files with Marisa/Starlit stems…")
    rename_starlit_files(CODE_DST)
    if VFX_SCENES_DST.exists():
        rename_starlit_files(VFX_SCENES_DST)
    if (ASSET_DST / "scenes").exists():
        rename_starlit_files(ASSET_DST / "scenes")

    # Restore preserved helpers
    for rel, content in preserve.items():
        out = CODE_DST / rel
        out.parent.mkdir(parents=True, exist_ok=True)
        out.write_text(content, encoding="utf-8")
        print(f"  restored {rel}")

    # Fix Abstract card portrait paths to Whitney asset root + lower ids
    # (already replaced res paths)

    # Patch character file to use Whitney flipbook + identity
    char_path = CODE_DST / "Character" / "Whitney.cs"
    if not char_path.exists():
        # might still be named after rename from MarisaCharacter.cs → Whitney.cs
        candidates = list((CODE_DST / "Character").glob("*.cs"))
        print("Character files:", candidates)
        for c in candidates:
            if "Whitney" in c.name or "Character" in c.name:
                char_path = c
                break

    if char_path.exists():
        text = char_path.read_text(encoding="utf-8")
        # Inject combat visuals overrides after class open if missing
        if "WhitneyCombatVisuals" not in text:
            inject = '''
    // Whitney atelier identity — flipbook combat (not Marisa spine).
    public const string CharacterId = "Whitney";
    public static readonly Color Color = new("4B3F8C");
    public override Color MapDrawingColor => Color;
    public override Color RemoteTargetingLineColor => Color;
    public override NCreatureVisuals? CreateCustomVisuals() => WhitneyCombatVisuals.Create();
    public override float DeathAnimTime => 1.1f;
'''
            text = text.replace(
                "public class Whitney : PlaceholderCharacterModel\n{",
                "public class Whitney : PlaceholderCharacterModel\n{" + inject,
            )
            # Drop spine-heavy custom visual path preference when flipbook exists
            text = re.sub(
                r"public override string CustomVisualPath => .*?;\n",
                "// CustomVisualPath omitted — CreateCustomVisuals flipbook used instead\n",
                text,
            )
            text = re.sub(
                r"public override string CustomEnergyCounterPath => .*?;\n",
                "// Energy counter: default until violet counter scene is ready\n",
                text,
            )
            text = re.sub(
                r"public override string CustomMerchantAnimPath => .*?;\n",
                "",
                text,
            )
            text = re.sub(
                r"public override string CustomTrailPath => .*?;\n",
                "",
                text,
            )
            text = re.sub(
                r"public override string CustomIconPath => .*?;\n",
                "",
                text,
            )
            text = re.sub(
                r"public override string CustomCharacterSelectBg => .*?;\n",
                '    public override string CustomCharacterSelectBg =>\n        "res://scenes/screens/char_select/char_select_bg_whitney.tscn";\n',
                text,
            )
            text = re.sub(
                r"public override string CustomCharacterSelectIconPath => .*?;\n",
                '    public override string CustomCharacterSelectIconPath => "res://Whitney/images/charui/char_select_whitney.png";\n',
                text,
            )
            text = re.sub(
                r"public override string CustomCharacterSelectLockedIconPath => .*?;\n",
                '    public override string CustomCharacterSelectLockedIconPath => "res://Whitney/images/charui/char_select_whitney_locked.png";\n',
                text,
            )
            text = re.sub(
                r"public override string CustomIconTexturePath => .*?;\n",
                '    public override string CustomIconTexturePath => "res://Whitney/images/charui/character_icon_whitney.png";\n',
                text,
            )
            if "using MegaCrit.Sts2.Core.Nodes.Combat" not in text:
                text = text.replace(
                    "using MegaCrit.Sts2.Core.Models;",
                    "using MegaCrit.Sts2.Core.Models;\nusing MegaCrit.Sts2.Core.Nodes.Combat;",
                )
            if "using Whitney.WhitneyCode.Character;" not in text and "WhitneyCombatVisuals" in text:
                # same namespace
                pass
            char_path.write_text(text, encoding="utf-8")
            print(f"  patched {char_path.relative_to(ROOT)}")

    # Card pool violet theme
    pool = CODE_DST / "PatchesNModels" / "WhitneyCardPool.cs"
    if pool.exists():
        text = pool.read_text(encoding="utf-8")
        text = text.replace('Title => "whitney"', 'Title => "Whitney"')
        text = text.replace('new("000A7D")', 'new("4B3F8C")')
        # Prefer Whitney energy orbs if present
        text = text.replace(
            "res://Whitney/images/ui/cardOrb.png",
            "res://Whitney/images/charui/big_energy.png",
        )
        text = text.replace(
            "res://Whitney/images/ui/energyOrb-lighter.png",
            "res://Whitney/images/charui/text_energy.png",
        )
        text = text.replace("bg_attack_MRS.png", "bg_attack_WTN.png")
        text = text.replace("bg_power_MRS.png", "bg_power_WTN.png")
        text = text.replace("bg_skill_MRS.png", "bg_skill_WTN.png")
        if "SeenByDefault" not in text:
            text = text.replace(
                "public override bool IsColorless => false;",
                "public override bool IsColorless => false;\n\n    public override bool SeenByDefault => true;",
            )
        # HSV toward violet
        text = re.sub(r"public override float H => .*?;", "public override float H => 0.72f;", text)
        text = re.sub(r"public override float S => .*?;", "public override float S => 0.48f;", text)
        text = re.sub(r"public override float V => .*?;", "public override float V => 0.78f;", text)
        pool.write_text(text, encoding="utf-8")
        print("  patched card pool colors")

    # Write MainFile.cs bridging Entry
    main = CODE_DST / "MainFile.cs"
    main.write_text(
        '''using Godot;
using MegaCrit.Sts2.Core.Modding;

namespace Whitney.WhitneyCode;

/// <summary>
/// Whitney mod entry — kit architecture adapted from MarisaMod (Amplify / Inkbound / Saturate).
/// Mechanics: Amplify kicker costs, Inkbound enchantment (was Starlit), Saturate (was Charge-Up).
/// Theme: atelier ink witch, violet frames, Energy + brush fantasy.
/// </summary>
[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "Whitney";
    public const string ResPath = $"res://{ModId}";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        // Marisa-style Entry.Init: script lookup + Harmony patches (Amplify cost UI, etc.)
        Entry.Init();
        Logger.Info("Whitney loaded — Marisa architecture, ink theme (Inkbound / Amplify / Saturate).");
    }
}
''',
        encoding="utf-8",
    )

    # Ensure Entry stays public Init
    if entry.exists() or (CODE_DST / "Entry.cs").exists():
        ep = CODE_DST / "Entry.cs"
        if ep.exists():
            t = ep.read_text(encoding="utf-8")
            t = t.replace('private const string LogPrefix = "[Whitney]";', 'public const string LogPrefix = "[Whitney]";')
            # Harmony id
            t = t.replace('new Harmony("whitney")', 'new Harmony("Whitney")')
            t = t.replace('ModConfigRegistry.Register("whitney"', 'ModConfigRegistry.Register("Whitney"')
            ep.write_text(t, encoding="utf-8")

    # Design note
    note = ROOT / "docs" / "whitney-marisa-rearch.md"
    note.write_text(
        """# Whitney re-architecture (Marisa → ink)

## Decision

Scrap the prior Whitney seal/element dual-mana kit. Rebuild Whitney on the **MarisaMod**
character architecture (complete Amplify / enchant / power suite), rethemed to
**atelier ink** with violet card chrome.

## Mapping

| Marisa | Whitney |
|--------|---------|
| Starlit enchantment/power | **Inkbound** |
| Charge-Up | **Saturate** (loc); class still `ChargeUpPower` until rename pass |
| Amplify | Amplify (kept) |
| Spark tag/cards | Spark (ink sparks) |
| Blue frames / orbs | Violet Whitney energy + recolored frames |
| Spine combat | Whitney Blender flipbook (`WhitneyCombatVisuals`) |

## Source

Mechanics adapted from local `STS2_MarisaMod` (authors: Flynn, Hell, Hohner_257, Kishin, Samsara).
Artwork is temporary placeholders from that pack until Whitney-generated art ships.

## Art still TODO

- Card portraits for every card (STS2 graphic, D3 Whitney lock)
- Violet recolor of card frames (attack/skill/power)
- Power/relic cutouts in ink palette
- Cookie / merchant / hand UI if we keep those paths
- Optional custom energy counter scene

## Build

```bash
cd mods/whitney
dotnet build -c Release
```
""",
        encoding="utf-8",
    )
    print(f"Wrote {note}")
    print("Done.")


if __name__ == "__main__":
    main()
