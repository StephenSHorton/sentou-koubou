"""Whitney D3 kit art prompts: cohesion, pose variety, selective presence."""
from __future__ import annotations
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
cards = json.loads((ROOT / "docs/whitney-cards.json").read_text(encoding="utf-8"))

STYLE = (
    "Official Slay the Spire 2 card portrait: bold hand-painted Mega Crit graphic style matching Whitney D3 lock — "
    "chunky brush, simplified shapes, limited facial detail, strong silhouette, dark vignette, "
    "indigo/cyan/cream/ink palette. NOT anime, NOT photoreal, NOT flat cartoon. Landscape. No text, no UI, no green screen."
)

CHAR = (
    "SAME young woman as the reference: blonde wavy hair, bright BLUE eyes, big round black glasses, "
    "indigo tall witch hat matching indigo dress, cyan glowing ink quill when armed. "
    "Varied pose and expression — not the same ready stance every time. "
    "When in combat, body often orients right but face stays readable (three-quarter)."
)

UNIVERSE = (
    "Shared Whitney atelier universe props when relevant: cyan ink quill, indigo hat/dress, "
    "elemental ink seals (fire ember / water cyan / earth brown / air white), inkwell, "
    "glowing glyphs, ribbons, brush strokes of colored ink."
)

# presence, short scene beat, props
SCENES: dict[str, tuple[str, str, list[str]]] = {
    # basics
    "spark": ("full", "Whitney casting a bright ember spark from cyan quill tip, small fire ink glyph, three-quarter face readable", ["quill", "fire"]),
    "ripple": ("full", "Whitney bracing behind a translucent water-ink ripple shield, defensive ready, face readable", ["quill", "water"]),
    "channelink": ("full", "Whitney channeling cyan ink streams into an open inkwell, focused calm expression, ink rising", ["inkwell", "quill"]),
    "noviceseal": ("full", "Whitney pressing a glowing novice seal stamp with quill, soft apprentice smile", ["seal", "quill"]),
    # commons fire/water/earth/air
    "emberstroke": ("full", "Whitney sweeping a fiery ink stroke slash with quill, dynamic three-quarter action, varied pose", ["quill", "fire"]),
    "cinderpin": ("partial", "Sharp ember pin of ink piercing a foe silhouette, Whitney partial with quill", ["fire", "quill"]),
    "tideguard": ("full", "Whitney holding a tide of blue ink as a protective barrier, calm protector face", ["water", "quill"]),
    "rippleward": ("partial", "Rippling water-ink ward glyphs floating, Whitney silhouette behind", ["water"]),
    "stoneglyph": ("partial", "Heavy carved stone-ink glyph slamming forward, Whitney secondary", ["earth", "seal"]),
    "dustseal": ("none", "Crumbling dust-earth seal stamp still-life, brown ink glyphs, no face", ["earth", "seal"]),
    "gust": ("partial", "Wind slash arcs of white-cyan ink, Whitney partial mid-cast three-quarter", ["air", "quill"]),
    "zephyrdraft": ("partial", "Floating cards/pages blown by zephyr ink wind, Whitney small figure", ["air"]),
    "drenchseal": ("none", "Water seal dripping, weak-blue droplets, pure prop", ["water", "seal"]),
    "sparkler": ("partial", "Sparkler burst of tiny fire ink sparks, Whitney hand and quill only", ["fire", "quill"]),
    "puddle": ("none", "Glowing ink puddle reflecting glyphs, still-life", ["water"]),
    "pebble": ("none", "Ink-stone pebble with earth rune, still-life", ["earth"]),
    "draft": ("none", "Wind-ink pages and draft swirls, still-life", ["air"]),
    "sootmark": ("partial", "Soot black fire-ink mark branding a surface, Whitney partial", ["fire"]),
    "mosscoat": ("partial", "Mossy earth-ink coat of block growing over armor, Whitney partial", ["earth"]),
    "crossbreeze": ("full", "Whitney whipping crosswind ink slashes at multiple foe silhouettes, active but not cookie-cutter ready pose", ["air", "quill"]),
    "inkdrip": ("none", "Cyan ink drips filling a small well, pure still-life", ["inkwell"]),
    "sealpress": ("full", "Whitney both hands pressing a large seal into glowing paper, determined face", ["seal"]),
    "wetstone": ("none", "Wet stone with water-ink sheen and glyphs", ["water", "earth"]),
    "kindling": ("partial", "Kindling fire-ink sparks feeding vigor glow, Whitney silhouette", ["fire"]),
    # uncommons
    "magmaflow": ("full", "Whitney guiding a river of magma-ink with quill, intense expression, three-quarter", ["fire", "quill"]),
    "steamburst": ("partial", "Steam burst of fire+water ink explosion, Whitney partial covering face with arm", ["fire", "water"]),
    "grandseal": ("full", "Whitney raising an enormous glowing grand seal overhead, awe expression, full figure", ["seal", "quill"]),
    "focusedstroke": ("full", "Whitney mid precise single ink stroke, eyes narrowed focus, close three-quarter", ["quill"]),
    "blizzardseal": ("none", "Ice-blizzard seal radiating frost ink, pure prop", ["water", "seal"]),
    "lightningrod": ("partial", "Lightning rod of air-ink channeling bolts, Whitney small figure grounding it", ["air"]),
    "tidaldraw": ("full", "Whitney pulling a tidal wave of ink as block, braced stance different from ready", ["water", "quill"]),
    "quake": ("partial", "Earth quake cracks with ink glyphs, Whitney partial stomping", ["earth"]),
    "updraft": ("partial", "Updraft wind lifting papers and ink, Whitney coat flaring", ["air"]),
    "inkwell": ("none", "Ornate traveling inkwell overflowing cyan ink, still-life hero prop", ["inkwell"]),
    "scorchline": ("full", "Whitney drawing a burning scorch line on the ground with quill, lunging side-three-quarter", ["fire", "quill"]),
    "icelattice": ("none", "Crystalline ice lattice of frozen ink, pure effect", ["water"]),
    "rootbind": ("partial", "Ink roots binding a foe, Whitney casting from behind roots", ["earth"]),
    "galecut": ("full", "Whitney multi-slash gale cuts with quill flourishes, spinning pose", ["air", "quill"]),
    "refill": ("full", "Whitney refilling quill from glowing inkwell, gentle smile, quieter pose", ["inkwell", "quill"]),
    "confluence": ("partial", "Four elemental ink streams confluencing, Whitney center silhouette", ["seal"]),
    "searingmist": ("partial", "Searing mist cloud of fire-water ink, Whitney partial obscured", ["fire", "water"]),
    "mudslide": ("none", "Mudslide of earth-water ink cascading, still-life disaster", ["earth", "water"]),
    "tempest": ("full", "Whitney at center of tempest ink storm, hair and dress wind-whipped, dramatic pose", ["air", "quill"]),
    "attunedeep": ("full", "Whitney meditative deep attune, eyes closed then glowing, seated or kneeling pose", ["seal"]),
    "inkshield": ("full", "Whitney behind a solid ink-script shield wall, defensive crouch", ["quill", "inkwell"]),
    "sealstorm": ("partial", "Storm of flying seals raining damage, Whitney tiny casting figure", ["seal"]),
    "whirlpool": ("none", "Ink whirlpool vortex, pure water effect", ["water"]),
    "pyreglyph": ("partial", "Giant fire pyre glyph, Whitney arm raised with quill", ["fire", "seal"]),
    "skywrite": ("full", "Whitney writing glowing words across the sky with quill, looking up, unique pose", ["air", "quill"]),
    "floodgate": ("partial", "Floodgate of water-ink bursting open, Whitney partial opening seals", ["water"]),
    "emberarmor": ("full", "Whitney coated in ember-ink armor plates, confident stance different from ready", ["fire"]),
    "stonescript": ("none", "Floating stone script tablets with earth runes, still-life power", ["earth", "seal"]),
    "windscript": ("none", "Wind script ribbons of air-ink text, still-life", ["air"]),
    "tidescript": ("none", "Tide script scrolls dripping water-ink, still-life", ["water"]),
    "flamescript": ("none", "Flame script burning pages, still-life", ["fire"]),
    "dualquill": ("full", "Whitney dual-wielding two cyan quills, playful grin, dynamic pose", ["quill"]),
    "spray": ("partial", "Ink spray burst hitting many silhouettes, Whitney partial", ["quill"]),
    "bedrock": ("partial", "Bedrock pillars of earth-ink rising, Whitney standing atop one small", ["earth"]),
    "afterburn": ("full", "Whitney after a fiery cast, afterburn trails, looking over shoulder three-quarter", ["fire", "quill"]),
    # rares
    "elementalform": ("full", "Whitney transformed in four-color elemental ink aura, powerful upright pose, face clear", ["quill", "seal"]),
    "cataclysmseal": ("full", "Whitney both hands on world-shaking cataclysm seal, strained powerful expression", ["seal"]),
    "monsoon": ("partial", "Monsoon curtains of water-ink, Whitney small under umbrella-like ward", ["water"]),
    "wildfire": ("full", "Whitney silhouette in wildfire ink inferno, quill raised, dramatic backlit three-quarter", ["fire", "quill"]),
    "mountainheart": ("partial", "Mountain heart crystal of earth-ink, Whitney kneeling touching it", ["earth"]),
    "skyfall": ("full", "Whitney calling skyfall of air-ink meteors, arms raised looking up", ["air", "quill"]),
    "perfectseal": ("full", "Whitney completing a perfect glowing seal, serene satisfied expression, precise hands", ["seal", "quill"]),
    "inktide": ("full", "Whitney riding a tide of pure cyan ink, exhilarated face, unique motion pose", ["inkwell", "quill"]),
    "fourwinds": ("partial", "Four winds directional ink gusts, Whitney center tiny", ["air"]),
    "obsidianscript": ("none", "Obsidian black script tablets absorbing ink, still-life rare power", ["seal"]),
    "aquascript": ("none", "Aqua script water calligraphy forming block waves, still-life", ["water"]),
    "galeform": ("full", "Whitney in gale-form wind body, dress and hair extreme motion, three-quarter face", ["air", "quill"]),
    "infernoseal": ("full", "Whitney branding a massive inferno seal into the ground, kneeling powerful pose", ["fire", "seal"]),
    "glacier": ("partial", "Glacier wall of ice-ink, Whitney partial behind frost", ["water"]),
    "tectonic": ("partial", "Tectonic plates splitting with earth-ink, Whitney small on cliff", ["earth"]),
    "hurricane": ("full", "Whitney eye-of-hurricane calm face while storm rages, arms out", ["air", "quill"]),
    "masterwork": ("full", "Whitney presenting a masterwork seal masterpiece, proud soft smile, portrait-forward three-quarter", ["seal", "quill"]),
    "livingink": ("full", "Living ink creatures swirling around Whitney, wonder expression, quill as conductor", ["inkwell", "quill"]),
    "eclipseseal": ("partial", "Eclipse seal dark sun glyph, Whitney silhouette before it", ["seal"]),
    "sanctuary": ("full", "Whitney inside a sanctuary dome of protective ink, peaceful kneeling", ["quill"]),
    "prismburst": ("partial", "Prism burst of multi-color elemental ink rays, Whitney partial casting", ["seal"]),
    "eternalquill": ("full", "Whitney with legendary eternal cyan quill radiating, heroic three-quarter, unique pose", ["quill"]),
    "cataclysmlite": ("partial", "Lesser cataclysm cracks and seals, Whitney small figure", ["seal"]),
    "archive": ("none", "Archive of floating ink tomes and seals, still-life library", ["seal", "inkwell"]),
    "worldseal": ("full", "Whitney sealing the world with ultimate world seal, arms wide, awe and power, face readable", ["seal", "quill"]),
}

RELICS = {
    "travelersinkpot": "Single relic artifact cutout pure green screen: traveling bronze inkpot with cyan ink drip, strap, STS2 painted prop only, no person",
    "apprenticeribbon": "Single relic artifact cutout pure green screen: soft indigo apprentice ribbon bow, STS2 painted prop only, no person",
    "sealedvial": "Single relic artifact cutout pure green screen: sealed glass vial of swirling cyan ink, cork and wax seal, STS2 painted prop only",
    "elementalribbon": "Single relic artifact cutout pure green screen: four-color elemental ribbon (fire water earth air), STS2 painted prop only",
    "mosscharm": "Single relic artifact cutout pure green screen: mossy earth charm pendant, STS2 painted prop only",
    "fourcolorpalette": "Single relic artifact cutout pure green screen: small four-color ink palette with elemental blobs, STS2 painted prop only",
    "journalquill": "Single relic artifact cutout pure green screen: cyan glowing journal quill pen, STS2 painted prop only",
    "brushcase": "Single relic artifact cutout pure green screen: leather brush case with ink brushes, STS2 painted prop only",
}


def presence_clause(p: str) -> str:
    if p == "full":
        return f"FEATURE {CHAR} as clear readable focus."
    if p == "partial":
        return f"Include {CHAR} as recognizable secondary figure or silhouette (same hair, blue eyes, glasses, indigo hat/dress), not always centered."
    return "NO full character portrait — object/effect still-life only, same universe props and palette. Tiny distant silhouette at most."


def main():
    out = {}
    for c in cards:
        if c.get("rarity") == "relic" or c.get("type") == "Relic":
            continue
        cid = c["id"]
        if cid not in SCENES:
            SCENES[cid] = ("partial", f"Elemental ink magic beat for '{c.get('name', cid)}' with cyan quill and seal motifs", ["quill", "seal"])
        presence, scene, props = SCENES[cid]
        out[cid] = {
            "presence": presence,
            "props": props,
            "name": c.get("name", cid),
            "prompt": f"{scene}. {presence_clause(presence)} {UNIVERSE} {STYLE}",
        }
    payload = {
        "character_ref": "docs/assets/whitney/variants/whitney_locked_d3.jpg",
        "combat_ref": "docs/assets/whitney/variants/whitney_combat_right_green.jpg",
        "cards": out,
        "relics": {k: {"prompt": v} for k, v in RELICS.items()},
    }
    path = ROOT / "tools" / "whitney_d3_prompts.json"
    path.write_text(json.dumps(payload, indent=2), encoding="utf-8")
    from collections import Counter
    print("wrote", path, "cards", len(out), Counter(v["presence"] for v in out.values()))


if __name__ == "__main__":
    main()
