using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using Brennen.BrennenCode.Cards;
using Brennen.BrennenCode.Cards.Basic;
using Brennen.BrennenCode.Cards.Uncommon;
using Brennen.BrennenCode.Extensions;
using Brennen.BrennenCode.Relics;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;

namespace Brennen.BrennenCode.Character;

/// <summary>
/// Brennen — older brother, League nights, Corvette energy.
/// Family meme pack character #1 for sentou-koubou.
/// </summary>
public class Brennen : PlaceholderCharacterModel
{
    public const string CharacterId = "Brennen";

    /// <summary>Warm ember red — aggressive lane energy.</summary>
    public static readonly Color Color = new("e85d4c");

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Masculine;
    public override int StartingHp => 74;

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
}
