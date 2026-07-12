#!/usr/bin/env python3
"""
Fetch Slay the Spire 2 reference data for balancing character kits.

Pulls from Spire Codex (https://spire-codex.com) — free public API, no key —
and organizes per-character JSON + readable markdown under reference/sts2/.

That tree is gitignored; re-run this script after game patches.

Usage (from repo root):
  python tools/fetch_sts2_reference.py
  python tools/fetch_sts2_reference.py --out reference/sts2
"""

from __future__ import annotations

import argparse
import json
import shutil
import sys
import urllib.error
import urllib.request
from collections import Counter, defaultdict
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

API_BASE = "https://spire-codex.com"
USER_AGENT = "sentou-koubou-reference/1.0 (+https://github.com/StephenSHorton/sentou-koubou)"

# Public catalog endpoints useful for kit design / balance.
ENDPOINTS: dict[str, str] = {
    "characters": "/api/characters",
    "cards": "/api/cards",
    "relics": "/api/relics",
    "potions": "/api/potions",
    "powers": "/api/powers",
    "keywords": "/api/keywords",
    "enchantments": "/api/enchantments",
    "orbs": "/api/orbs",
    "monsters": "/api/monsters",
    "events": "/api/events",
    "afflictions": "/api/afflictions",
    "ancient_pools": "/api/ancient-pools",
    "encounters": "/api/encounters",
    "glossary": "/api/glossary",
    "mechanics_constants": "/api/mechanics/constants",
    "versions": "/api/versions",
}

# Card `color` / relic `pool` keys for the five base characters.
# Character records use a different `color` field (UI palette: red/green/blue/…).
# Join characters → cards via character.id.lower() == card.color.
CHARACTER_POOLS = ("ironclad", "silent", "defect", "necrobinder", "regent")

# Non-character card buckets (still useful for shared content).
SHARED_CARD_COLORS = (
    "colorless",
    "curse",
    "status",
    "token",
    "event",
    "quest",
    "unknown",
)


def character_pool_key(character: dict[str, Any]) -> str:
    """Map a Spire Codex character record to the card.color / relic.pool slug."""
    cid = str(character.get("id") or "").strip().lower()
    if cid in CHARACTER_POOLS:
        return cid
    # Fallback: some dumps may already use pool names as id.
    color = str(character.get("color") or "").strip().lower()
    if color in CHARACTER_POOLS:
        return color
    return cid or color or "unknown"

RARITY_ORDER = ("Basic", "Common", "Uncommon", "Rare", "Ancient", "Event", "Token", "Status", "Curse", "Quest")
TYPE_ORDER = ("Attack", "Skill", "Power", "Status", "Curse", "Quest")


def fetch_json(path: str, timeout: float = 90.0) -> Any:
    url = API_BASE + path
    req = urllib.request.Request(url, headers={"User-Agent": USER_AGENT, "Accept": "application/json"})
    with urllib.request.urlopen(req, timeout=timeout) as resp:
        return json.loads(resp.read().decode("utf-8"))


def write_json(path: Path, data: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


def rarity_key(item: dict[str, Any]) -> str:
    return str(item.get("rarity_key") or item.get("rarity") or "Unknown")


def type_key(item: dict[str, Any]) -> str:
    return str(item.get("type_key") or item.get("type") or "Unknown")


def sorted_by_name(items: list[dict[str, Any]]) -> list[dict[str, Any]]:
    return sorted(items, key=lambda x: (str(x.get("name") or x.get("id") or "").lower(), str(x.get("id") or "")))


def count_by(items: list[dict[str, Any]], key_fn) -> dict[str, int]:
    return dict(sorted(Counter(key_fn(i) for i in items).items(), key=lambda kv: (-kv[1], kv[0])))


def card_line(card: dict[str, Any]) -> str:
    cost = card.get("cost")
    if card.get("is_x_cost"):
        cost_s = "X"
    elif cost is None:
        cost_s = "—"
    else:
        cost_s = str(cost)
    star = card.get("star_cost")
    if star is not None:
        cost_s = f"{cost_s}+{star}★" if cost_s != "—" else f"{star}★"

    dmg = card.get("damage")
    blk = card.get("block")
    hits = card.get("hit_count")
    nums: list[str] = []
    if dmg is not None:
        nums.append(f"{dmg} dmg" + (f" ×{hits}" if hits and hits > 1 else ""))
    if blk is not None:
        nums.append(f"{blk} block")
    nums_s = f" ({', '.join(nums)})" if nums else ""

    desc = (card.get("description") or "").replace("\n", " / ")
    upg = card.get("upgrade_description")
    upg_s = f"\n  - Upgrade: {upg.replace(chr(10), ' / ')}" if upg else ""
    return f"- **{card.get('name')}** `{card.get('id')}` — {cost_s}-cost {type_key(card)}{nums_s}\n  - {desc}{upg_s}"


def write_character_markdown(
    path: Path,
    character: dict[str, Any],
    cards: list[dict[str, Any]],
    relics: list[dict[str, Any]],
    potions: list[dict[str, Any]],
) -> None:
    cid = character.get("id") or character.get("color") or "?"
    name = character.get("name") or cid
    lines: list[str] = [
        f"# {name}",
        "",
        f"- **id:** `{cid}`",
        f"- **starting HP:** {character.get('starting_hp')}",
        f"- **max energy:** {character.get('max_energy')}",
        f"- **starting gold:** {character.get('starting_gold')}",
        f"- **orb slots:** {character.get('orb_slots')}",
        f"- **starting deck:** {', '.join(character.get('starting_deck') or [])}",
        f"- **starting relics:** {', '.join(character.get('starting_relics') or [])}",
        "",
        (character.get("description") or "").strip(),
        "",
        "## Pool summary",
        "",
    ]

    by_rarity = count_by(cards, rarity_key)
    by_type = count_by(cards, type_key)
    reward = [c for c in cards if rarity_key(c) in ("Common", "Uncommon", "Rare")]
    lines.append(f"- **total cards in color:** {len(cards)}")
    lines.append(
        f"- **reward pool (C/U/R):** "
        f"{sum(1 for c in reward if rarity_key(c) == 'Common')}/"
        f"{sum(1 for c in reward if rarity_key(c) == 'Uncommon')}/"
        f"{sum(1 for c in reward if rarity_key(c) == 'Rare')} "
        f"= {len(reward)}"
    )
    lines.append(f"- **by rarity:** {by_rarity}")
    lines.append(f"- **by type:** {by_type}")
    lines.append(f"- **character relics:** {len(relics)}")
    lines.append(f"- **character potions:** {len(potions)}")
    lines.append("")

    # Group cards
    grouped: dict[str, list[dict[str, Any]]] = defaultdict(list)
    for c in cards:
        grouped[rarity_key(c)].append(c)

    for rarity in RARITY_ORDER:
        group = grouped.pop(rarity, [])
        if not group:
            continue
        lines.append(f"## {rarity} ({len(group)})")
        lines.append("")
        # subgroup by type for scanability
        by_t: dict[str, list[dict[str, Any]]] = defaultdict(list)
        for c in group:
            by_t[type_key(c)].append(c)
        for t in TYPE_ORDER:
            sub = by_t.pop(t, [])
            if not sub:
                continue
            lines.append(f"### {t}")
            lines.append("")
            for c in sorted_by_name(sub):
                lines.append(card_line(c))
            lines.append("")
        for t, sub in sorted(by_t.items()):
            lines.append(f"### {t}")
            lines.append("")
            for c in sorted_by_name(sub):
                lines.append(card_line(c))
            lines.append("")

    for rarity, group in sorted(grouped.items()):
        lines.append(f"## {rarity} ({len(group)})")
        lines.append("")
        for c in sorted_by_name(group):
            lines.append(card_line(c))
        lines.append("")

    if relics:
        lines.append("## Character relics")
        lines.append("")
        for r in sorted_by_name(relics):
            lines.append(
                f"- **{r.get('name')}** `{r.get('id')}` — {rarity_key(r)}: "
                f"{(r.get('description') or '').replace(chr(10), ' / ')}"
            )
        lines.append("")

    if potions:
        lines.append("## Character potions")
        lines.append("")
        for p in sorted_by_name(potions):
            lines.append(
                f"- **{p.get('name')}** `{p.get('id')}` — {rarity_key(p)}: "
                f"{(p.get('description') or '').replace(chr(10), ' / ')}"
            )
        lines.append("")

    path.write_text("\n".join(lines).rstrip() + "\n", encoding="utf-8")


def write_index_markdown(
    path: Path,
    meta: dict[str, Any],
    characters: list[dict[str, Any]],
    cards: list[dict[str, Any]],
    relics: list[dict[str, Any]],
) -> None:
    lines = [
        "# STS2 reference data",
        "",
        f"- **fetched_at:** {meta.get('fetched_at')}",
        f"- **source:** {meta.get('source')}",
        f"- **local_game_version:** {meta.get('local_game_version') or 'unknown'}",
        f"- **cards:** {len(cards)} · **relics:** {len(relics)} · **characters:** {len(characters)}",
        "",
        "Regenerate with `python tools/fetch_sts2_reference.py`.",
        "",
        "## Characters",
        "",
        "| Character | HP | Energy | Deck size | Starter relic | Cards (C/U/R) | Total color |",
        "|-----------|----|--------|-----------|---------------|---------------|-------------|",
    ]

    by_color: dict[str, list[dict[str, Any]]] = defaultdict(list)
    for c in cards:
        by_color[str(c.get("color") or "unknown").lower()].append(c)

    for ch in sorted(characters, key=lambda x: str(x.get("name") or "")):
        pool_key = character_pool_key(ch)
        pool = by_color.get(pool_key, [])
        reward = [c for c in pool if rarity_key(c) in ("Common", "Uncommon", "Rare")]
        c_n = sum(1 for c in reward if rarity_key(c) == "Common")
        u_n = sum(1 for c in reward if rarity_key(c) == "Uncommon")
        r_n = sum(1 for c in reward if rarity_key(c) == "Rare")
        deck = ch.get("starting_deck") or []
        relics_s = ", ".join(ch.get("starting_relics") or [])
        lines.append(
            f"| [{ch.get('name')}](by_character/{pool_key}/README.md) "
            f"| {ch.get('starting_hp')} | {ch.get('max_energy')} | {len(deck)} "
            f"| {relics_s} | {c_n}/{u_n}/{r_n} | {len(pool)} |"
        )

    lines.extend(
        [
            "",
            "## Layout",
            "",
            "```",
            "reference/sts2/",
            "  _meta.json",
            "  README.md",
            "  raw/                 # full dumps from Spire Codex",
            "  by_character/<id>/   # per-character cards, relics, potions + README",
            "  shared/              # colorless / curse / status / token / event / quest",
            "```",
            "",
            "## Vanilla reward pool shape",
            "",
            "As of the current Spire Codex dump, each base character's **reward** pool "
            "(Common + Uncommon + Rare only) is approximately **20 / 36 / 26** (~82 cards). "
            "Basics and Ancients sit outside that reward count. Use `by_character/*/summary.json` "
            "for exact numbers after each refresh.",
            "",
        ]
    )
    path.write_text("\n".join(lines), encoding="utf-8")


def try_local_game_version() -> dict[str, Any] | None:
    candidates = [
        Path(r"C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\release_info.json"),
        Path(r"C:\Program Files\Steam\steamapps\common\Slay the Spire 2\release_info.json"),
        Path.home() / "Library/Application Support/Steam/steamapps/common/Slay the Spire 2/release_info.json",
        Path.home() / ".steam/steam/steamapps/common/Slay the Spire 2/release_info.json",
        Path.home() / ".local/share/Steam/steamapps/common/Slay the Spire 2/release_info.json",
    ]
    for p in candidates:
        if p.is_file():
            try:
                return json.loads(p.read_text(encoding="utf-8"))
            except (OSError, json.JSONDecodeError):
                continue
    return None


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--out",
        type=Path,
        default=Path("reference/sts2"),
        help="Output directory (default: reference/sts2)",
    )
    parser.add_argument(
        "--skip",
        nargs="*",
        default=[],
        help="Endpoint keys to skip (e.g. monsters events)",
    )
    args = parser.parse_args()
    out: Path = args.out
    skip = set(args.skip)

    print(f"Fetching STS2 reference → {out.resolve()}")
    raw_dir = out / "raw"
    raw_dir.mkdir(parents=True, exist_ok=True)

    dumps: dict[str, Any] = {}
    errors: dict[str, str] = {}
    for key, path in ENDPOINTS.items():
        if key in skip:
            print(f"  skip  {key}")
            continue
        try:
            print(f"  get   {path} ...", end="", flush=True)
            data = fetch_json(path)
            dumps[key] = data
            write_json(raw_dir / f"{key}.json", data)
            n = len(data) if isinstance(data, list) else "obj"
            print(f" {n}")
        except urllib.error.HTTPError as e:
            errors[key] = f"HTTP {e.code}"
            print(f" FAIL HTTP {e.code}")
        except Exception as e:  # noqa: BLE001 — surface any fetch issue
            errors[key] = str(e)
            print(f" FAIL {e}")

    if "characters" not in dumps or "cards" not in dumps:
        print("ERROR: characters and cards are required; aborting split.", file=sys.stderr)
        return 1

    characters: list[dict[str, Any]] = dumps["characters"]
    cards: list[dict[str, Any]] = dumps["cards"]
    relics: list[dict[str, Any]] = dumps.get("relics") or []
    potions: list[dict[str, Any]] = dumps.get("potions") or []

    # Index by color / pool
    cards_by_color: dict[str, list[dict[str, Any]]] = defaultdict(list)
    for c in cards:
        cards_by_color[str(c.get("color") or "unknown").lower()].append(c)

    relics_by_pool: dict[str, list[dict[str, Any]]] = defaultdict(list)
    for r in relics:
        relics_by_pool[str(r.get("pool") or "shared").lower()].append(r)

    potions_by_pool: dict[str, list[dict[str, Any]]] = defaultdict(list)
    for p in potions:
        potions_by_pool[str(p.get("pool") or "shared").lower()].append(p)

    # Fresh per-character tree (avoids stale folders after key renames)
    by_char_root = out / "by_character"
    if by_char_root.exists():
        shutil.rmtree(by_char_root)

    # Per-character folders (keyed by card pool slug, e.g. ironclad — not UI color)
    for ch in characters:
        pool_key = character_pool_key(ch)
        ui_color = str(ch.get("color") or "").lower()
        cdir = out / "by_character" / pool_key
        cdir.mkdir(parents=True, exist_ok=True)

        ch_cards = sorted_by_name(cards_by_color.get(pool_key, []))
        ch_relics = sorted_by_name(relics_by_pool.get(pool_key, []))
        # de-dupe relics by id
        seen: set[str] = set()
        uniq_relics: list[dict[str, Any]] = []
        for r in ch_relics:
            rid = str(r.get("id"))
            if rid in seen:
                continue
            seen.add(rid)
            uniq_relics.append(r)

        ch_potions = sorted_by_name(potions_by_pool.get(pool_key, []))
        seen_p: set[str] = set()
        uniq_potions: list[dict[str, Any]] = []
        for p in ch_potions:
            pid = str(p.get("id"))
            if pid in seen_p:
                continue
            seen_p.add(pid)
            uniq_potions.append(p)

        reward = [c for c in ch_cards if rarity_key(c) in ("Common", "Uncommon", "Rare")]
        summary = {
            "id": ch.get("id"),
            "name": ch.get("name"),
            "pool": pool_key,
            "ui_color": ui_color,
            "starting_hp": ch.get("starting_hp"),
            "max_energy": ch.get("max_energy"),
            "starting_gold": ch.get("starting_gold"),
            "orb_slots": ch.get("orb_slots"),
            "starting_deck": ch.get("starting_deck"),
            "starting_relics": ch.get("starting_relics"),
            "card_counts": {
                "total": len(ch_cards),
                "by_rarity": count_by(ch_cards, rarity_key),
                "by_type": count_by(ch_cards, type_key),
                "reward_common": sum(1 for c in reward if rarity_key(c) == "Common"),
                "reward_uncommon": sum(1 for c in reward if rarity_key(c) == "Uncommon"),
                "reward_rare": sum(1 for c in reward if rarity_key(c) == "Rare"),
                "reward_total": len(reward),
            },
            "relic_count": len(uniq_relics),
            "potion_count": len(uniq_potions),
        }

        write_json(cdir / "character.json", ch)
        write_json(cdir / "cards.json", ch_cards)
        write_json(cdir / "relics.json", uniq_relics)
        write_json(cdir / "potions.json", uniq_potions)
        write_json(cdir / "summary.json", summary)
        write_character_markdown(cdir / "README.md", ch, ch_cards, uniq_relics, uniq_potions)
        print(f"  wrote by_character/{pool_key}/ ({len(ch_cards)} cards, {len(uniq_relics)} relics)")

    # Shared / non-character buckets
    shared_dir = out / "shared"
    shared_dir.mkdir(parents=True, exist_ok=True)
    for color in SHARED_CARD_COLORS:
        bucket = sorted_by_name(cards_by_color.get(color, []))
        if bucket:
            write_json(shared_dir / f"{color}_cards.json", bucket)
            print(f"  wrote shared/{color}_cards.json ({len(bucket)})")

    write_json(shared_dir / "shared_relics.json", sorted_by_name(relics_by_pool.get("shared", [])))
    write_json(shared_dir / "shared_potions.json", sorted_by_name(potions_by_pool.get("shared", [])))

    local = try_local_game_version()
    meta = {
        "fetched_at": datetime.now(timezone.utc).isoformat(),
        "source": API_BASE,
        "source_note": "Spire Codex public API (community database; not official MegaCrit dumps).",
        "endpoints": {k: API_BASE + v for k, v in ENDPOINTS.items() if k not in skip},
        "errors": errors or None,
        "counts": {k: (len(v) if isinstance(v, list) else "object") for k, v in dumps.items()},
        "local_game_version": (local or {}).get("version"),
        "local_game_release_info": local,
        "character_pools": list(CHARACTER_POOLS),
        "vanilla_reward_target": {"common": 20, "uncommon": 35, "rare": 25, "total": 80},
    }
    write_json(out / "_meta.json", meta)
    write_index_markdown(out / "README.md", meta, characters, cards, relics)

    # Convenience top-level copies (also in raw/)
    write_json(out / "characters.json", characters)
    write_json(out / "cards.json", cards)
    write_json(out / "relics.json", relics)
    if potions:
        write_json(out / "potions.json", potions)

    print()
    print(f"Done. Index: {out / 'README.md'}")
    if errors:
        print(f"Partial failures: {errors}", file=sys.stderr)
        return 2
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
