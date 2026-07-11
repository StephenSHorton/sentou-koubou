# STS2 Character Animation Pipeline

Local **HTML/JS** tool for planning and previewing combat animation states **without launching Slay the Spire 2**.

Use it to:

- Track multiple characters (Brennen, Whitney, future kits)
- Drop **frame PNGs per state** and scrub/play them (Idle, Attack, Cast, Hit, Dead, Relaxed, Revive)
- Manage **part checklists** (cut-apart art for Spine)
- Keep a **bone hierarchy** template aligned with BaseLib / Spine export
- Export a **manifest JSON** + generated **C# `SetupAnimationState` snippet**

This is **not** a full Spine editor. It is the production desk for the pipeline: what clips exist, how they look, and what still needs rigging.

## Run

Open in a browser (Chrome/Edge/Firefox):

```text
tools/char-anim-pipeline/index.html
```

Double-click the file, or from the repo:

```powershell
start tools/char-anim-pipeline/index.html
```

No build step. Data is stored in **localStorage** (project meta) + **IndexedDB** (images).

## Quick start

1. Click **Seed Brennen / Whitney shells** (or **+** to create a character).
2. Select **Idle** → **Add frames** (or drag images onto the canvas).
3. Repeat for **Attack**, **Hit**, **Dead**, **Cast**.
4. Set a **reference fullbody** to compare scale/pose.
5. Use **Parts** / **Bones** tabs while preparing Spine cutouts.
6. Open **Export guide** → copy C# snippet; **Export manifest JSON** for the team.

## Animation states (BaseLib)

| State    | Typical loop | Notes                          |
|----------|--------------|--------------------------------|
| Idle     | yes          | Required                       |
| Attack   | no           | Strike / attack cards          |
| Cast     | no           | Skills / non-basic             |
| Hit      | no           | Damage reaction                |
| Dead     | no           | Death hold / fall              |
| Relaxed  | yes          | Optional calm pose             |
| Revive   | no           | Optional                       |

Clip names default to lowercase (`idle`, `attack`, …) and are editable per state. In-game, BaseLib can remap missing Spine clips onto idle via `SetupAnimationState`.

## Export types

| Button | Contents |
|--------|----------|
| **Export manifest JSON** | Lightweight: state meta, parts, bones, frame counts (no images) |
| **Export full project** | Includes base64 frame/part blobs (shareable backup; can be large) |
| **Import project** | Restores a full project export into this browser |

## Suggested art → game flow

```text
Locked combat likeness
  → part-split PNG set (Parts tab)
  → Spine rig (Bones tab as reference)
  → animate clips matching state names
  → export .skel / .atlas / .png
  → pack under scenes/creature_visuals/
  → wire CustomVisualPath + SetupCustomAnimationStates
```

While Spine is in progress, use **frame previews** here (and optional Godot `AnimatedSprite2D`) so combat is not Ironclad.

## Files

| File | Role |
|------|------|
| `index.html` | Shell UI |
| `styles.css` | Dark pipeline theme |
| `app.js` | State, IndexedDB, player, export |
| `README.md` | This doc |

## Privacy

Everything stays **in your browser** unless you export a JSON file. Clearing site data wipes projects—export full projects for backup.
