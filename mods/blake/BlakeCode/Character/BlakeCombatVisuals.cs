using BaseLib.Extensions;
using Blake.BlakeCode.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MainFile = Blake.BlakeCode.MainFile;

namespace Blake.BlakeCode.Character;

/// <summary>
/// Builds combat <see cref="NCreatureVisuals"/> from flipbook PNGs under
/// <c>res://Blake/images/combat/{idle,attack,hit,dead}/</c>.
/// BaseLib's CustomAnimation finds the nested <see cref="AnimatedSprite2D"/>
/// and plays states by name (Idle / Attack / Hit / Dead / …).
/// </summary>
public static class BlakeCombatVisuals
{
    public const float Fps = 24f;

    /// <summary>Scale so 512×682 export reads closer to vanilla combat body size.</summary>
    public const float SpriteScale = 0.56f;

    /// <summary>
    /// Frame packs — keep ranges in sync with tools that expand combat key poses.
    /// Idle loops; attack/hit/dead play once.
    /// </summary>
    private static readonly (string Folder, int Start, int End, bool Loop, string[] Aliases)[] States =
    [
        ("idle", 1, 24, true, ["idle", "Idle", "relaxed", "Relaxed", "revive", "Revive"]),
        ("attack", 1, 12, false, ["attack", "Attack", "cast", "Cast"]),
        ("hit", 1, 8, false, ["hit", "Hit", "hurt", "Hurt"]),
        ("dead", 1, 12, false, ["dead", "Dead", "die", "Die"]),
    ];

    public static NCreatureVisuals Create()
    {
        var frames = BuildSpriteFrames();
        var first = frames.GetFrameTexture("idle", 0);
        var imgSize = first?.GetSize() ?? new Vector2(512, 682);
        var drawn = imgSize * SpriteScale;
        var boundsSize = drawn * 1.1f;

        var root = new NCreatureVisuals();

        var bounds = new Control();
        root.AddUnique(bounds, "Bounds");
        bounds.Position = new Vector2(-boundsSize.X / 2f, -boundsSize.Y);
        bounds.Size = boundsSize;

        var sprite = new AnimatedSprite2D
        {
            Name = "Visuals",
            SpriteFrames = frames,
            Scale = new Vector2(SpriteScale, SpriteScale),
            Position = new Vector2(0f, -drawn.Y * 0.5f),
            Centered = true,
        };
        root.AddUnique(sprite, "Visuals");
        sprite.Play("idle");

        var center = new Marker2D();
        root.AddUnique(center, "CenterPos");
        center.Position = bounds.Position + bounds.Size * new Vector2(0.5f, 0.6f);

        var intent = new Marker2D();
        root.AddUnique(intent, "IntentPos");
        intent.Position = bounds.Position + bounds.Size * new Vector2(0.5f, 0f) + new Vector2(0, -70);

        var orb = new Marker2D();
        root.AddUnique(orb, "OrbPos");
        orb.Position = center.Position;

        var talk = new Marker2D();
        root.AddUnique(talk, "TalkPos");
        talk.Position = bounds.Position + bounds.Size * new Vector2(0.5f, 0.15f);

        var phobia = new Node2D();
        root.AddUnique(phobia, "PhobiaModeVisuals");

        return root;
    }

    private static SpriteFrames BuildSpriteFrames()
    {
        var sf = new SpriteFrames();
        if (sf.HasAnimation("default"))
            sf.RemoveAnimation("default");

        foreach (var (folder, start, end, loop, aliases) in States)
        {
            var primary = aliases[0];
            if (!sf.HasAnimation(primary))
                sf.AddAnimation(primary);

            sf.SetAnimationLoop(primary, loop);
            sf.SetAnimationSpeed(primary, Fps);
            while (sf.GetFrameCount(primary) > 0)
                sf.RemoveFrame(primary, 0);

            for (var i = start; i <= end; i++)
            {
                var rel = Path.Join("combat", folder, $"{folder}_{i:D4}.png");
                var path = rel.ImagePath().Replace('\\', '/');
                if (!ResourceLoader.Exists(path))
                {
                    MainFile.Logger.Info($"Missing combat frame: {path}");
                    continue;
                }

                var tex = PreloadManager.Cache.GetTexture2D(path);
                sf.AddFrame(primary, tex);
            }

            foreach (var alias in aliases.Skip(1))
            {
                if (sf.HasAnimation(alias))
                    continue;
                sf.AddAnimation(alias);
                sf.SetAnimationLoop(alias, loop);
                sf.SetAnimationSpeed(alias, Fps);
                var count = sf.GetFrameCount(primary);
                for (var f = 0; f < count; f++)
                    sf.AddFrame(alias, sf.GetFrameTexture(primary, f), sf.GetFrameDuration(primary, f));
            }
        }

        return sf;
    }
}
