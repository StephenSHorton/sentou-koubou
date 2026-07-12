using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using Blake.BlakeCode.Cards.Basic;
using Blake.BlakeCode.Extensions;
using Blake.BlakeCode.Relics;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Blake.BlakeCode.Character;

/// <summary>
/// Blake — The Falcon.
/// Racer-brawler: wind up Charge (Rev = double), protect the fist, Unleash the punch.
/// Captain Falcon homage kit for sentou-koubou family pack.
/// </summary>
public class Blake : PlaceholderCharacterModel
{
    public const string CharacterId = "Blake";

    /// <summary>Falcon blue — racing suit / map pen / G-Diffuser glow.</summary>
    public static readonly Color Color = new("2B6CB0");

    /// <summary>Slightly brighter cobalt for map path drawing (readable on dark map).</summary>
    public static readonly Color MapColor = new("3B82F6");

    public override Color NameColor => Color;
    public override Color MapDrawingColor => MapColor;
    public override Color RemoteTargetingLineColor => MapColor;

    public override CharacterGender Gender => CharacterGender.Masculine;

    /// <summary>Mid-tank HP — needs Block to protect Charge, not a pure wall.</summary>
    public override int StartingHp => 75;

    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<Jab>(),
        ModelDb.Card<Jab>(),
        ModelDb.Card<Jab>(),
        ModelDb.Card<Jab>(),
        ModelDb.Card<Guard>(),
        ModelDb.Card<Guard>(),
        ModelDb.Card<Guard>(),
        ModelDb.Card<Guard>(),
        ModelDb.Card<RevUp>(),
        ModelDb.Card<Haymaker>(),
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<RacersGauntlet>(),
    ];

    public override CardPoolModel CardPool => ModelDb.CardPool<BlakeCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<BlakeRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<BlakePotionPool>();

    public override Control CustomIcon
    {
        get
        {
            var icon = NodeFactory<Control>.CreateFromResource(CustomIconTexturePath);
            icon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            return icon;
        }
    }

    public override string CustomIconTexturePath => "character_icon_blake.png".CharacterUiPath();
    public override string CustomCharacterSelectIconPath => "char_select_blake.png".CharacterUiPath();
    public override string CustomCharacterSelectLockedIconPath => "char_select_blake_locked.png".CharacterUiPath();
    public override string CustomMapMarkerPath => "map_marker_blake.png".CharacterUiPath();

    public override string CustomCharacterSelectBg =>
        "res://scenes/screens/char_select/char_select_bg_blake.tscn";

    /// <summary>Combat body: flipbook idle / attack / hit / dead (see <see cref="BlakeCombatVisuals"/>).</summary>
    public override NCreatureVisuals? CreateCustomVisuals() => BlakeCombatVisuals.Create();

    /// <summary>Dead clip is 12 frames @ 24fps ≈ 0.5s; small headroom.</summary>
    public override float DeathAnimTime => 0.7f;
}
