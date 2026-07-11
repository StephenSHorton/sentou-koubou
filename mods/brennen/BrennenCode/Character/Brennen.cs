using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using Brennen.BrennenCode.Cards.Basic;
using Brennen.BrennenCode.Extensions;
using Brennen.BrennenCode.Relics;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;

namespace Brennen.BrennenCode.Character;

/// <summary>
/// Brennen — older brother, League nights, tank main energy.
/// Peels, bodyblocks, and still feeds for the meme.
/// Family meme pack character #1 for sentou-koubou.
/// </summary>
public class Brennen : PlaceholderCharacterModel
{
    public const string CharacterId = "Brennen";

    /// <summary>Warm ember red — frontline engage energy.</summary>
    public static readonly Color Color = new("e85d4c");

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Masculine;
    /// <summary>Tankier baseline — soaks so the "ADC" can cook.</summary>
    public override int StartingHp => 82;

    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<StrikeBrennen>(),
        ModelDb.Card<StrikeBrennen>(),
        ModelDb.Card<StrikeBrennen>(),
        ModelDb.Card<StrikeBrennen>(),
        ModelDb.Card<DefendBrennen>(),
        ModelDb.Card<DefendBrennen>(),
        ModelDb.Card<DefendBrennen>(),
        ModelDb.Card<DefendBrennen>(),
        ModelDb.Card<DefendBrennen>(),
        ModelDb.Card<Feed>(),
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
    /// Full-screen character select backdrop (otherwise vanilla Ironclad scene is reused).
    /// Scene lives at res://scenes/screens/char_select/char_select_bg_brennen.tscn per BaseLib.
    /// </summary>
    public override string CustomCharacterSelectBg =>
        "res://scenes/screens/char_select/char_select_bg_brennen.tscn";
}
