using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using Brennen.BrennenCode.Cards.Basic;
using Brennen.BrennenCode.Extensions;
using Brennen.BrennenCode.Relics;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Brennen.BrennenCode.Character;

/// <summary>
/// Brennen — frontline tank.
/// Peels, pays HP for tempo, keeps Block (Proxy Camp), slams with Tower Dive.
/// Feed is a reward-pool meme, not a starter.
/// </summary>
public class Brennen : PlaceholderCharacterModel
{
    public const string CharacterId = "Brennen";

    /// <summary>Warm ember red — frontline engage energy.</summary>
    public static readonly Color Color = new("e85d4c");

    public override Color NameColor => Color;

    /// <summary>Map path / pen color when drawing routes.</summary>
    public override Color MapDrawingColor => Color;

    /// <summary>Multiplayer remote-target line matches map / name color.</summary>
    public override Color RemoteTargetingLineColor => Color;

    public override CharacterGender Gender => CharacterGender.Masculine;
    /// <summary>Tankier baseline — soaks so the "ADC" can cook.</summary>
    public override int StartingHp => 82;

    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<StrikeBrennen>(),
        ModelDb.Card<StrikeBrennen>(),
        ModelDb.Card<StrikeBrennen>(),
        ModelDb.Card<StrikeBrennen>(),
        ModelDb.Card<StrikeBrennen>(),
        ModelDb.Card<DefendBrennen>(),
        ModelDb.Card<DefendBrennen>(),
        ModelDb.Card<DefendBrennen>(),
        ModelDb.Card<DefendBrennen>(),
        ModelDb.Card<DefendBrennen>(),
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<DuoQueue>(),
    ];

    public override CardPoolModel CardPool => ModelDb.CardPool<BrennenCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<BrennenRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<BrennenPotionPool>();

    public override Control CustomIcon
    {
        get
        {
            var icon = NodeFactory<Control>.CreateFromResource(CustomIconTexturePath);
            icon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            return icon;
        }
    }

    public override string CustomIconTexturePath => "character_icon_brennen.png".CharacterUiPath();
    public override string CustomCharacterSelectIconPath => "char_select_brennen.png".CharacterUiPath();
    public override string CustomCharacterSelectLockedIconPath => "char_select_brennen_locked.png".CharacterUiPath();
    public override string CustomMapMarkerPath => "map_marker_brennen.png".CharacterUiPath();

    /// <summary>
    /// Full-screen character select backdrop (otherwise BaseLib reuses Ironclad).
    /// Scene is injected into the .pck after pack — PckPacker cannot ship .tscn.
    /// </summary>
    public override string CustomCharacterSelectBg =>
        "res://scenes/screens/char_select/char_select_bg_brennen.tscn";

    /// <summary>
    /// Combat body: Blender flipbook (idle / attack / hit / dead) instead of Ironclad.
    /// See tools/char-anim-pipeline/BLENDER_PIPELINE.md.
    /// </summary>
    public override NCreatureVisuals? CreateCustomVisuals() => BrennenCombatVisuals.Create();

    /// <summary>Dead clip is 20 frames @ 24fps ≈ 0.83s; give a little headroom.</summary>
    public override float DeathAnimTime => 1.0f;
}
