using Godot;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace BattleBall;

/// <summary>
/// Combat minigame: one or more orange balls + mid-screen floor + right-side hoop.
/// Visuals live as children of a screen-space CanvasLayer.
/// </summary>
public class BallWorld : Node
{
    public static BallWorld? Instance { get; private set; }

    public const float Radius = 29f;
    public const float Gravity = 2400f;
    public const float Bounce = 0.74f;
    public const float WallBounce = 0.8f;
    public const float RimBounce = 0.88f;
    public const float BackboardBounce = 0.78f;
    public const float GroundFriction = 0.9f;
    public const float AirDragPerSec = 0.96f;
    public const float FloorYFrac = 0.62f;
    public const float MaxSpeed = 2400f;
    public const float MinThrowSpeed = 280f;
    public const float GrabSlack = 30f;
    public const float ScoreCooldownout = 1.15f;
    public const float HoopDrawH = 198f;
    public const int CanvasLayerOrder = 80;
    public const int PhysSubsteps = 6;
    public const int MinBalls = 1;
    public const int MaxBalls = 8;

    private CanvasLayer? _layer;
    private Control? _hud;
    private Label? _scoreLabel;
    private Label? _countLabel;
    private Button? _btnAdd;
    private Button? _btnRemove;
    private Sprite2D? _hoop;
    private Sprite2D? _hoopFront;
    private ConfettiCanvas? _confettiCanvas;
    private readonly ConfettiBurst _confetti = new();
    private readonly List<BallBody> _balls = new();
    private int _localSeq;
    private ulong _lastTickMsec;
    private float _scoreCooldown;
    private int _score;
    private bool _frameHooked;
    private HoopGeom _hoopGeom;
    private Vector2 _viewSize = new(1920, 1080);
    private Texture2D? _ballTex;
    private float _ballScale = 1f;

    private readonly record struct HoopGeom(
        Vector2 RimCenter,
        float OpeningHalfW,
        Vector2 LeftPost,
        Vector2 RightPost,
        float SideRadius,
        Vector2 BoardA,
        Vector2 BoardB);

    // ------------------------------------------------------------ lifecycle

    public static void AttachTo(NCombatRoom room)
    {
        Instance?.Teardown();

        try
        {
            var layer = new CanvasLayer
            {
                Name = "BattleBallLayer",
                Layer = CanvasLayerOrder,
                FollowViewportEnabled = false,
                ProcessMode = ProcessModeEnum.Always,
            };

            var world = new BallWorld
            {
                Name = "BattleBallWorld",
                ProcessMode = ProcessModeEnum.Always,
            };
            world._layer = layer;

            room.AddChild(layer);
            layer.AddChild(world);

            world.BuildVisuals(layer);
            world.SetProcess(true);
            world.SetPhysicsProcess(true);
            world.SetProcessInput(true);
            world.HookFrameTick();
            Instance = world;

            // Starter ball — shared id 0 so peers begin with the same ball.
            world.CreateBall(id: 0, at: null, announce: false);
            try
            {
                SceneTree? tree = world.GetTree();
                if (tree != null)
                {
                    SceneTreeTimer t = tree.CreateTimer(0.2);
                    BallWorld captured = world;
                    t.Timeout += () =>
                    {
                        if (GodotObject.IsInstanceValid(captured))
                            captured.LayoutVisuals(captured.ViewSize());
                    };
                }
            }
            catch (Exception bootEx)
            {
                MainFile.Logger.Warn($"Battle Ball deferred boot: {bootEx.Message}");
            }

            MainFile.Logger.Info(
                $"Battle Ball attached (CanvasLayer {CanvasLayerOrder}, multi-ball).");
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"Battle Ball attach failed: {e}");
            Instance = null;
        }
    }

    public void Teardown()
    {
        if (Instance == this)
            Instance = null;
        UnhookFrameTick();
        foreach (BallBody b in _balls)
            b.DestroyVisuals();
        _balls.Clear();
        try
        {
            if (_layer != null && GodotObject.IsInstanceValid(_layer))
            {
                _layer.QueueFree();
                _layer = null;
                return;
            }
        }
        catch
        {
            // fall through
        }
        if (GodotObject.IsInstanceValid(this))
            QueueFree();
    }

    private void HookFrameTick()
    {
        if (_frameHooked)
            return;
        try
        {
            SceneTree? tree = GetTree();
            if (tree == null)
                return;
            tree.ProcessFrame += OnSceneTreeFrame;
            _frameHooked = true;
            _lastTickMsec = Time.GetTicksMsec();
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Battle Ball frame hook failed: {e.Message}");
        }
    }

    private void UnhookFrameTick()
    {
        if (!_frameHooked)
            return;
        try
        {
            SceneTree? tree = GetTree();
            if (tree != null)
                tree.ProcessFrame -= OnSceneTreeFrame;
        }
        catch
        {
            // ignore
        }
        _frameHooked = false;
    }

    private void OnSceneTreeFrame()
    {
        if (!GodotObject.IsInstanceValid(this))
        {
            UnhookFrameTick();
            return;
        }
        TickSimulation();
    }

    private void BuildVisuals(CanvasLayer layer)
    {
        _hud = new Control
        {
            Name = "BattleBallHud",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ClipContents = false,
            ProcessMode = ProcessModeEnum.Always,
        };
        layer.AddChild(_hud);

        _scoreLabel = new Label
        {
            Name = "Score",
            Text = "0",
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = 5,
        };
        _scoreLabel.AddThemeColorOverride("font_color", new Color(1f, 0.95f, 0.8f, 0.95f));
        _scoreLabel.AddThemeFontSizeOverride("font_size", 40);
        _hud.AddChild(_scoreLabel);

        // Compact ball-count controls (bottom-left of the playfield).
        _btnRemove = MakeToolbarButton("−", "Remove a free ball");
        _btnRemove.Pressed += OnRemovePressed;
        _hud.AddChild(_btnRemove);

        _countLabel = new Label
        {
            Name = "BallCount",
            Text = "1",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = 50,
        };
        _countLabel.AddThemeColorOverride("font_color", new Color(1f, 0.95f, 0.85f, 0.95f));
        _countLabel.AddThemeFontSizeOverride("font_size", 22);
        _hud.AddChild(_countLabel);

        _btnAdd = MakeToolbarButton("+", "Spawn another ball");
        _btnAdd.Pressed += OnAddPressed;
        _hud.AddChild(_btnAdd);

        _ballTex = BallAssets.Ball;
        if (_ballTex == null)
        {
            MainFile.Logger.Warn("ball.png missing — using procedural fallback texture.");
            _ballTex = MakeBallTexture(128);
        }
        float texW = _ballTex.GetSize().X;
        if (texW < 1f)
            texW = 128f;
        _ballScale = (Radius * 2f) / texW;

        Texture2D? hoopTex = BallAssets.Hoop;
        float hoopScale = 1f;
        if (hoopTex != null)
        {
            Vector2 ts = hoopTex.GetSize();
            if (ts.Y > 1f)
                hoopScale = HoopDrawH / ts.Y;

            _hoop = new Sprite2D
            {
                Name = "HoopBack",
                Texture = hoopTex,
                Centered = true,
                FlipH = true,
                ZIndex = 12,
                ZAsRelative = false,
                TextureFilter = CanvasItem.TextureFilterEnum.LinearWithMipmaps,
                Visible = true,
            };
            _hoop.Scale = new Vector2(hoopScale, hoopScale);
            layer.AddChild(_hoop);
        }

        Texture2D? frontTex = BallAssets.HoopFront;
        if (frontTex != null)
        {
            _hoopFront = new Sprite2D
            {
                Name = "HoopFront",
                Texture = frontTex,
                Centered = true,
                FlipH = true,
                ZIndex = 28,
                ZAsRelative = false,
                TextureFilter = CanvasItem.TextureFilterEnum.LinearWithMipmaps,
                Visible = true,
            };
            _hoopFront.Scale = new Vector2(hoopScale, hoopScale);
            layer.AddChild(_hoopFront);
        }

        _confettiCanvas = new ConfettiCanvas(_confetti)
        {
            Name = "Confetti",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = 40,
        };
        _hud.AddChild(_confettiCanvas);

        HoopLayout.Load();
        UpdateCountUi();
    }

    private static Button MakeToolbarButton(string text, string tooltip)
    {
        var b = new Button
        {
            Text = text,
            TooltipText = tooltip,
            MouseFilter = Control.MouseFilterEnum.Stop,
            FocusMode = Control.FocusModeEnum.None,
            ZIndex = 50,
            CustomMinimumSize = new Vector2(40f, 36f),
        };
        return b;
    }

    private static Texture2D MakeBallTexture(int size)
    {
        var img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);
        float cx = size * 0.5f;
        float cy = size * 0.5f;
        float r = size * 0.46f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - cx;
                float dy = y + 0.5f - cy;
                float d = MathF.Sqrt(dx * dx + dy * dy);
                if (d > r)
                    continue;
                float edge = Math.Clamp((r - d) / 3f, 0f, 1f);
                float hi = Math.Clamp(
                    1f - ((dx + 12f) * (dx + 12f) + (dy + 14f) * (dy + 14f)) / (r * r * 0.9f),
                    0f, 1f);
                var col = new Color(
                    Math.Clamp(0.95f + 0.05f * hi, 0f, 1f),
                    Math.Clamp(0.32f + 0.45f * hi, 0f, 1f),
                    Math.Clamp(0.05f + 0.15f * hi, 0f, 1f),
                    edge);
                if (d > r - 4f)
                    col = new Color(0.25f, 0.08f, 0.02f, edge);
                img.SetPixel(x, y, col);
            }
        }
        return ImageTexture.CreateFromImage(img);
    }

    // ------------------------------------------------------------ ball lifecycle

    private int AllocBallId()
    {
        _localSeq++;
        ulong lid = BallSync.Instance?.LocalId ?? 1UL;
        // Unique across peers: (low bits of net id) << 16 | local sequence. Id 0 reserved for starter.
        int id = (int)(((lid & 0x7FFFUL) << 16) | (uint)(_localSeq & 0xFFFF));
        if (id == 0)
            id = 1;
        return id;
    }

    private BallBody? FindBall(int id)
    {
        for (int i = 0; i < _balls.Count; i++)
        {
            if (_balls[i].Id == id)
                return _balls[i];
        }
        return null;
    }

    private BallBody CreateBall(int id, Vector2? at, bool announce)
    {
        if (FindBall(id) != null)
            return FindBall(id)!;
        if (_balls.Count >= MaxBalls)
            return _balls[^1];

        CanvasLayer layer = _layer
            ?? throw new InvalidOperationException("Battle Ball layer missing.");
        if (_hud == null)
            throw new InvalidOperationException("Battle Ball hud missing.");

        Texture2D tex = _ballTex ?? MakeBallTexture(128);
        var sprite = new Sprite2D
        {
            Name = $"Ball_{id}",
            Texture = tex,
            Centered = true,
            ZIndex = 20,
            ZAsRelative = false,
            TextureFilter = CanvasItem.TextureFilterEnum.LinearWithMipmaps,
            Visible = true,
            Scale = new Vector2(_ballScale, _ballScale),
        };
        layer.AddChild(sprite);
        // Keep front rim above all balls.
        if (_hoopFront != null && GodotObject.IsInstanceValid(_hoopFront))
            _hoopFront.ZIndex = 28;

        var grab = new Control
        {
            Name = $"BallGrab_{id}",
            MouseFilter = Control.MouseFilterEnum.Stop,
            ZIndex = 25,
            ClipContents = false,
        };
        int capturedId = id;
        grab.GuiInput += ev => OnGrabGuiInput(capturedId, ev);
        _hud.AddChild(grab);

        var ball = new BallBody(id, sprite, grab);
        Vector2 size = ViewSize();
        LayoutVisuals(size);
        Vector2 spawn = at ?? new Vector2(
            size.X * (0.30f + 0.08f * (_balls.Count % 4)),
            size.Y * (FloorYFrac - 0.20f - 0.03f * (_balls.Count % 3)));
        ball.Pos = ClampPosition(spawn, size);
        ball.PrevPos = ball.Pos;
        ball.Vel = new Vector2(size.X * 0.04f, -size.Y * 0.18f);
        ball.Authority = BallSync.Instance?.LocalId;
        _balls.Add(ball);
        SyncBallSprite(ball);
        UpdateCountUi();

        if (announce)
            BallSync.Instance?.SendSpawn(id, ball.Pos);

        MainFile.Logger.Info($"Ball spawned id={id} at=({ball.Pos.X:0},{ball.Pos.Y:0}) count={_balls.Count}");
        return ball;
    }

    private bool TryDespawn(int id, bool announce)
    {
        BallBody? ball = FindBall(id);
        if (ball == null)
            return false;
        if (_balls.Count <= MinBalls)
            return false;
        if (ball.IsHeld)
            return false;

        _balls.Remove(ball);
        ball.DestroyVisuals();
        UpdateCountUi();
        if (announce)
            BallSync.Instance?.SendDespawn(id);
        MainFile.Logger.Info($"Ball despawned id={id} count={_balls.Count}");
        return true;
    }

    private void OnAddPressed()
    {
        if (_balls.Count >= MaxBalls)
            return;
        CreateBall(AllocBallId(), at: null, announce: true);
    }

    private void OnRemovePressed()
    {
        // Remove the most recently free ball (not held).
        for (int i = _balls.Count - 1; i >= 0; i--)
        {
            BallBody b = _balls[i];
            if (!b.IsHeld && TryDespawn(b.Id, announce: true))
                return;
        }
    }

    private void UpdateCountUi()
    {
        if (_countLabel != null && GodotObject.IsInstanceValid(_countLabel))
            _countLabel.Text = _balls.Count.ToString();
        if (_btnAdd != null && GodotObject.IsInstanceValid(_btnAdd))
            _btnAdd.Disabled = _balls.Count >= MaxBalls;
        if (_btnRemove != null && GodotObject.IsInstanceValid(_btnRemove))
            _btnRemove.Disabled = _balls.Count <= MinBalls;
    }

    // ------------------------------------------------------------ remote apply

    public void ApplyRemoteGrab(int ballId, ulong holder, Vector2 pos)
    {
        BallBody? ball = FindBall(ballId);
        if (ball == null)
            ball = CreateBall(ballId, pos, announce: false);
        if (ball.HeldLocal)
            return;
        ball.HeldLocal = false;
        ball.HeldByRemote = holder;
        ball.Authority = holder;
        ball.Pos = ClampPosition(pos, ViewSize());
        ball.PrevPos = ball.Pos;
        ball.Vel = Vector2.Zero;
        SyncBallSprite(ball);
    }

    public void ApplyRemoteThrow(int ballId, ulong thrower, Vector2 pos, Vector2 vel)
    {
        BallBody? ball = FindBall(ballId);
        if (ball == null)
            ball = CreateBall(ballId, pos, announce: false);
        if (ball.HeldLocal)
            return;
        ball.HeldLocal = false;
        ball.HeldByRemote = null;
        ball.Authority = thrower;
        ball.Pos = ClampPosition(pos, ViewSize());
        ball.PrevPos = ball.Pos;
        ball.Vel = ClampVel(vel);
        SyncBallSprite(ball);
    }

    public void ApplyRemoteState(int ballId, ulong authority, Vector2 pos, Vector2 vel, bool held)
    {
        BallBody? ball = FindBall(ballId);
        if (ball == null)
        {
            // Late join / missed spawn — materialize from state.
            ball = CreateBall(ballId, pos, announce: false);
        }

        if (ball.HeldLocal)
            return;

        // Another peer holds it — only the holder may update.
        if (ball.HeldByRemote.HasValue && ball.HeldByRemote != authority && !held)
            return;

        // We still own free-flight authority — ignore foreign free-flight.
        if (!held
            && !ball.HeldByRemote.HasValue
            && ball.Authority == BallSync.Instance?.LocalId
            && BallSync.Instance is { IsMultiplayer: true })
            return;

        Vector2 size = ViewSize();
        ball.Authority = authority;
        if (held)
        {
            ball.HeldByRemote = authority;
            ball.PrevPos = ball.Pos;
            // Snappy follow while grabbed so remotes see cursor track.
            ball.Pos = ClampPosition(ball.Pos.Lerp(ClampPosition(pos, size), 0.85f), size);
            ball.Vel = Vector2.Zero;
        }
        else
        {
            if (ball.HeldByRemote == authority)
                ball.HeldByRemote = null;
            ball.PrevPos = ball.Pos;
            ball.Pos = ClampPosition(ball.Pos.Lerp(ClampPosition(pos, size), 0.55f), size);
            ball.Vel = vel;
        }
        SyncBallSprite(ball);
    }

    public void ApplyRemoteScore(int ballId, int side, Vector2 confettiAt)
    {
        _ = ballId;
        _ = side;
        _score++;
        _scoreCooldown = ScoreCooldownout;
        _confetti.Explode(confettiAt);
        if (_scoreLabel != null)
            _scoreLabel.Text = _score.ToString();
        _confettiCanvas?.QueueRedraw();
    }

    public void ApplyRemoteSpawn(int ballId, Vector2 pos)
    {
        if (FindBall(ballId) != null)
            return;
        if (_balls.Count >= MaxBalls)
            return;
        CreateBall(ballId, pos, announce: false);
    }

    public void ApplyRemoteDespawn(int ballId)
    {
        TryDespawn(ballId, announce: false);
    }

    // ------------------------------------------------------------ layout

    private void FitHud(Vector2 size)
    {
        _viewSize = size;
        if (_hud == null || !GodotObject.IsInstanceValid(_hud))
            return;
        _hud.AnchorLeft = 0f;
        _hud.AnchorTop = 0f;
        _hud.AnchorRight = 0f;
        _hud.AnchorBottom = 0f;
        _hud.Position = Vector2.Zero;
        _hud.Size = size;
        _hud.CustomMinimumSize = size;
        _hud.Visible = true;
    }

    private void LayoutVisuals(Vector2 size)
    {
        FitHud(size);
        float floorY = size.Y * FloorYFrac;

        if (_scoreLabel != null && GodotObject.IsInstanceValid(_scoreLabel))
        {
            _scoreLabel.Text = _score.ToString();
            _scoreLabel.Position = new Vector2(size.X * 0.5f - 48f, 24f);
            _scoreLabel.Size = new Vector2(96f, 52f);
            _scoreLabel.Visible = true;
        }

        // Toolbar: bottom-left above the floor strip.
        float barY = floorY + 10f;
        if (barY + 40f > size.Y - 8f)
            barY = size.Y - 48f;
        float bx = 16f;
        if (_btnRemove != null && GodotObject.IsInstanceValid(_btnRemove))
        {
            _btnRemove.Position = new Vector2(bx, barY);
            _btnRemove.Size = new Vector2(40f, 36f);
        }
        if (_countLabel != null && GodotObject.IsInstanceValid(_countLabel))
        {
            _countLabel.Position = new Vector2(bx + 44f, barY);
            _countLabel.Size = new Vector2(36f, 36f);
        }
        if (_btnAdd != null && GodotObject.IsInstanceValid(_btnAdd))
        {
            _btnAdd.Position = new Vector2(bx + 84f, barY);
            _btnAdd.Size = new Vector2(40f, 36f);
        }

        if (_confettiCanvas != null && GodotObject.IsInstanceValid(_confettiCanvas))
        {
            _confettiCanvas.Position = Vector2.Zero;
            _confettiCanvas.Size = size;
        }

        float margin = 16f;
        float spriteHalfW = 95f;
        float spriteHalfH = HoopDrawH * 0.5f;
        if (_hoop?.Texture != null)
        {
            Vector2 ts = _hoop.Texture.GetSize() * _hoop.Scale;
            spriteHalfW = ts.X * 0.5f;
            spriteHalfH = ts.Y * 0.5f;
        }

        HoopLayout L = HoopLayout.Instance;
        float cx = size.X - margin - spriteHalfW;
        float cy = floorY - L.HoopAboveFloor;

        var hoopPos = new Vector2(cx, cy);
        if (_hoop != null && GodotObject.IsInstanceValid(_hoop))
        {
            _hoop.Position = hoopPos;
            _hoop.Visible = true;
        }
        if (_hoopFront != null && GodotObject.IsInstanceValid(_hoopFront))
        {
            _hoopFront.Position = hoopPos;
            _hoopFront.Visible = true;
        }

        Vector2 rimCenter = new(
            cx + L.RimOffsetX * spriteHalfW,
            cy + L.RimOffsetY * spriteHalfH);
        float open = Math.Max(20f, L.OpeningHalfW * spriteHalfW);
        float sideR = Math.Max(10f, L.SideRadius * spriteHalfW);
        float ang = L.RimAngleDeg * (MathF.PI / 180f);
        Vector2 axis = new(MathF.Cos(ang), MathF.Sin(ang));
        Vector2 leftPost = rimCenter - axis * open;
        Vector2 rightPost = rimCenter + axis * open;
        Vector2 boardA = new(cx + L.BoardAX * spriteHalfW, cy + L.BoardAY * spriteHalfH);
        Vector2 boardB = new(cx + L.BoardBX * spriteHalfW, cy + L.BoardBY * spriteHalfH);
        _hoopGeom = new HoopGeom(rimCenter, open, leftPost, rightPost, sideR, boardA, boardB);

        foreach (BallBody ball in _balls)
            SyncBallSprite(ball);
    }

    private void SyncBallSprite(BallBody ball)
    {
        float diameter = Radius * 2f;
        if (ball.Sprite != null && GodotObject.IsInstanceValid(ball.Sprite))
        {
            ball.Sprite.Position = ball.Pos;
            float texW = ball.Sprite.Texture?.GetSize().X ?? 128f;
            if (texW < 1f)
                texW = 128f;
            float s = diameter / texW;
            ball.Sprite.Scale = new Vector2(s, s);
            ball.Sprite.Rotation = ball.Spin;
            ball.Sprite.Modulate = ball.HeldLocal
                ? new Color(1.15f, 1.1f, 1f)
                : ball.HeldByRemote.HasValue
                    ? new Color(0.75f, 0.9f, 1.15f)
                    : Colors.White;
            ball.Sprite.Visible = true;
            ball.Sprite.Show();
        }

        if (ball.GrabHit != null && GodotObject.IsInstanceValid(ball.GrabHit))
        {
            float hit = (Radius + GrabSlack) * 2f;
            ball.GrabHit.Position = new Vector2(ball.Pos.X - hit * 0.5f, ball.Pos.Y - hit * 0.5f);
            ball.GrabHit.Size = new Vector2(hit, hit);
            ball.GrabHit.Visible = true;
            ball.GrabHit.MouseFilter = Control.MouseFilterEnum.Stop;
        }
    }

    // ------------------------------------------------------------ size / mouse

    private Vector2 ViewSize()
    {
        try
        {
            Viewport? vp = GetViewport();
            if (vp != null)
            {
                Vector2 s = vp.GetVisibleRect().Size;
                if (s.X > 8f && s.Y > 8f)
                    return s;
            }
            if (_hud != null && _hud.Size.X > 8f && _hud.Size.Y > 8f)
                return _hud.Size;
        }
        catch
        {
            // fall through
        }
        return _viewSize.X > 8f ? _viewSize : new Vector2(1920, 1080);
    }

    private Vector2 MouseLocal()
    {
        try
        {
            Viewport? vp = GetViewport();
            if (vp != null)
                return vp.GetMousePosition();
        }
        catch
        {
            // fall through
        }
        return Vector2.Zero;
    }

    // ------------------------------------------------------------ input

    private void OnGrabGuiInput(int ballId, InputEvent @event)
    {
        if (@event is not InputEventMouseButton { ButtonIndex: MouseButton.Left } mb)
            return;
        BallBody? ball = FindBall(ballId);
        if (ball == null)
            return;
        Vector2 mouse = MouseLocal();
        if (mb.Pressed)
        {
            if (!ball.HeldLocal && CanGrabAt(ball, mouse))
            {
                BeginLocalGrab(ball, mouse);
                ball.GrabHit.AcceptEvent();
            }
        }
        else if (ball.HeldLocal)
        {
            EndLocalGrab(ball, mouse);
            ball.GrabHit.AcceptEvent();
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventMouseButton { ButtonIndex: MouseButton.Left } mb)
            return;
        Vector2 mouse = MouseLocal();
        if (mb.Pressed)
        {
            BallBody? ball = FindBallUnderMouse(mouse);
            if (ball != null && !ball.HeldLocal && CanGrabAt(ball, mouse))
            {
                BeginLocalGrab(ball, mouse);
                GetViewport()?.SetInputAsHandled();
            }
        }
        else
        {
            // Release any locally held ball.
            foreach (BallBody ball in _balls)
            {
                if (!ball.HeldLocal)
                    continue;
                EndLocalGrab(ball, mouse);
                GetViewport()?.SetInputAsHandled();
                break;
            }
        }
    }

    private BallBody? FindBallUnderMouse(Vector2 mouse)
    {
        BallBody? best = null;
        float bestD = float.MaxValue;
        foreach (BallBody b in _balls)
        {
            if (b.HeldByRemote.HasValue)
                continue;
            float d = b.Pos.DistanceTo(mouse);
            if (d <= Radius + GrabSlack && d < bestD)
            {
                bestD = d;
                best = b;
            }
        }
        return best;
    }

    private void DragHeldTo(BallBody ball, Vector2 mouse, float dt)
    {
        mouse = ClampPosition(mouse, ViewSize());
        if (dt > 0.0001f && dt < 0.1f)
        {
            Vector2 inst = (mouse - ball.Pos) / dt;
            ball.Vel = ball.Vel.Lerp(inst, 0.55f);
        }
        ball.PrevPos = ball.Pos;
        ball.Pos = mouse;
        SyncBallSprite(ball);
    }

    private static bool CanGrabAt(BallBody ball, Vector2 mouse)
    {
        if (ball.HeldByRemote.HasValue)
            return false;
        if (Time.GetTicksMsec() < ball.IgnoreGrabUntilMsec)
            return false;
        return ball.Pos.DistanceTo(mouse) <= Radius + GrabSlack;
    }

    private void BeginLocalGrab(BallBody ball, Vector2 mouse)
    {
        if (ball.HeldLocal)
            return;
        ball.HeldLocal = true;
        ball.HeldByRemote = null;
        ball.Authority = BallSync.Instance?.LocalId;
        ball.Pos = ClampPosition(mouse, ViewSize());
        ball.PrevPos = ball.Pos;
        ball.Vel = Vector2.Zero;
        _lastTickMsec = Time.GetTicksMsec();
        BallSync.Instance?.SendGrab(ball.Id, ball.Pos);
        // Force an immediate held snapshot so peers track from the first frame.
        BallSync.Instance?.SendState(ball.Id, ball.Pos, Vector2.Zero, held: true, force: true);
        SyncBallSprite(ball);
    }

    private void EndLocalGrab(BallBody ball, Vector2 mouse)
    {
        if (!ball.HeldLocal)
            return;
        ball.HeldLocal = false;

        Vector2 size = ViewSize();
        mouse = ClampPosition(mouse, size);
        ball.PrevPos = ball.Pos;
        ball.Pos = mouse;

        float speed = ball.Vel.Length();
        if (speed > 12f && speed < MinThrowSpeed)
            ball.Vel = ball.Vel.Normalized() * MinThrowSpeed;
        if (speed <= 12f)
            ball.Vel = new Vector2(0f, 120f);

        ball.Vel = ClampVel(ball.Vel);
        ball.Spin = -ball.Vel.X * 0.0025f;
        ball.Authority = BallSync.Instance?.LocalId;
        ball.IgnoreGrabUntilMsec = Time.GetTicksMsec() + 120;
        ball.FreeFlightLogLeft = 4;
        BallSync.Instance?.SendThrow(ball.Id, ball.Pos, ball.Vel);
        SyncBallSprite(ball);
    }

    // ------------------------------------------------------------ sim

    public override void _Process(double delta)
    {
        if (!_frameHooked)
            TickSimulation();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_frameHooked)
            TickSimulation();
    }

    private void TickSimulation()
    {
        ulong now = Time.GetTicksMsec();
        float dt = (now - _lastTickMsec) / 1000f;
        if (_lastTickMsec == 0 || dt <= 0f)
        {
            _lastTickMsec = now;
            return;
        }
        if (dt > 0.05f)
            dt = 1f / 60f;
        _lastTickMsec = now;

        if (_scoreCooldown > 0f)
            _scoreCooldown -= dt;

        _confetti.Update(dt);

        Vector2 size = ViewSize();
        LayoutVisuals(size);

        foreach (BallBody ball in _balls.ToArray())
            TickBall(ball, dt, size);

        _confettiCanvas?.QueueRedraw();
    }

    private void TickBall(BallBody ball, float dt, Vector2 size)
    {
        if (ball.HeldLocal)
        {
            DragHeldTo(ball, MouseLocal(), dt);
            if (!Input.IsMouseButtonPressed(MouseButton.Left))
                EndLocalGrab(ball, MouseLocal());
            else
            {
                // Stream grab position so remotes track the held ball.
                BallSync.Instance?.SendState(ball.Id, ball.Pos, ball.Vel, held: true);
            }
            return;
        }

        if (ball.HeldByRemote.HasValue)
        {
            // Position updated via ApplyRemoteState snapshots.
            SyncBallSprite(ball);
            return;
        }

        bool weOwn = ball.Authority == null
                     || ball.Authority == BallSync.Instance?.LocalId
                     || BallSync.Instance is not { IsMultiplayer: true };

        ball.PrevPos = ball.Pos;
        Integrate(ball, dt, size);

        if (weOwn)
        {
            TryScore(ball, size);
            BallSync.Instance?.SendState(ball.Id, ball.Pos, ball.Vel, held: false);
        }

        ball.Spin += ball.Vel.X * dt * 0.004f;
        SyncBallSprite(ball);
    }

    private void Integrate(BallBody ball, float dt, Vector2 size)
    {
        float step = dt / PhysSubsteps;
        for (int i = 0; i < PhysSubsteps; i++)
            IntegrateStep(ball, step, size);
    }

    private void IntegrateStep(BallBody ball, float dt, Vector2 size)
    {
        ball.Vel.Y += Gravity * dt;
        float drag = MathF.Pow(AirDragPerSec, dt);
        ball.Vel *= drag;
        ball.Vel = ClampVel(ball.Vel);
        ball.Pos += ball.Vel * dt;

        CollideWorld(ball, size);
        CollideHoop(ball, _hoopGeom);
        // Soft ball-ball separation so stacks don't occupy one pixel.
        CollideBalls(ball);
        ball.Pos = ClampPosition(ball.Pos, size);
    }

    private void CollideWorld(BallBody ball, Vector2 size)
    {
        float floorY = size.Y * FloorYFrac;
        float minX = Radius + 8f;
        float maxX = size.X - Radius - 8f;
        float minY = Radius + 8f;
        float maxY = floorY - Radius;

        if (ball.Pos.X < minX)
        {
            ball.Pos.X = minX;
            if (ball.Vel.X < 0f)
                ball.Vel.X = -ball.Vel.X * WallBounce;
        }
        else if (ball.Pos.X > maxX)
        {
            ball.Pos.X = maxX;
            if (ball.Vel.X > 0f)
                ball.Vel.X = -ball.Vel.X * WallBounce;
        }

        if (ball.Pos.Y < minY)
        {
            ball.Pos.Y = minY;
            if (ball.Vel.Y < 0f)
                ball.Vel.Y = -ball.Vel.Y * Bounce * 0.55f;
        }

        if (ball.Pos.Y > maxY)
        {
            ball.Pos.Y = maxY;
            if (ball.Vel.Y > 50f)
                ball.Vel.Y = -ball.Vel.Y * Bounce;
            else
                ball.Vel.Y = 0f;
            ball.Vel.X *= GroundFriction;
            if (Math.Abs(ball.Vel.X) < 18f)
                ball.Vel.X = 0f;
        }
    }

    private void CollideBalls(BallBody a)
    {
        foreach (BallBody b in _balls)
        {
            if (b.Id == a.Id || b.IsHeld)
                continue;
            Vector2 delta = a.Pos - b.Pos;
            float dist = delta.Length();
            float min = Radius * 2f;
            if (dist >= min || dist < 0.0001f)
                continue;
            Vector2 n = delta / dist;
            float pen = min - dist;
            a.Pos += n * (pen * 0.5f);
            b.Pos -= n * (pen * 0.5f);
            float va = a.Vel.Dot(n);
            float vb = b.Vel.Dot(n);
            if (va - vb < 0f)
            {
                float rest = 0.7f;
                float impulse = (1f + rest) * (vb - va) * 0.5f;
                a.Vel += n * impulse;
                b.Vel -= n * impulse;
            }
        }
    }

    private void CollideHoop(BallBody ball, HoopGeom hoop)
    {
        if (hoop.OpeningHalfW <= 0f)
            return;
        CollideCircleSegment(ball, hoop.BoardA, hoop.BoardB, BackboardBounce);
        CollideCircleCircle(ball, hoop.LeftPost, hoop.SideRadius, RimBounce);
        CollideCircleCircle(ball, hoop.RightPost, hoop.SideRadius, RimBounce);
    }

    private static void CollideCircleCircle(BallBody ball, Vector2 center, float otherR, float restitution)
    {
        float minDist = otherR + Radius;
        Vector2 delta = ball.Pos - center;
        float dist = delta.Length();
        if (dist >= minDist || dist < 0.0001f)
            return;

        Vector2 n = delta / dist;
        ball.Pos = center + n * minDist;
        float vn = ball.Vel.Dot(n);
        if (vn < 0f)
        {
            ball.Vel -= n * ((1f + restitution) * vn);
            ball.Vel *= 0.97f;
            ball.Spin += -MathF.Sign(n.X) * 0.15f;
        }
    }

    private static void CollideCircleSegment(BallBody ball, Vector2 a, Vector2 b, float restitution)
    {
        Vector2 ab = b - a;
        float abLenSq = ab.LengthSquared();
        if (abLenSq < 0.0001f)
            return;

        float t = Math.Clamp((ball.Pos - a).Dot(ab) / abLenSq, 0f, 1f);
        Vector2 closest = a + ab * t;
        Vector2 delta = ball.Pos - closest;
        float dist = delta.Length();
        if (dist >= Radius || dist < 0.0001f)
        {
            if (dist < 0.0001f)
            {
                Vector2 n0 = new(-ab.Y, ab.X);
                if (n0.LengthSquared() < 0.0001f)
                    return;
                n0 = n0.Normalized();
                if (n0.X > 0f)
                    n0 = -n0;
                ball.Pos = closest + n0 * (Radius + 0.5f);
                float vn0 = ball.Vel.Dot(n0);
                if (vn0 < 0f)
                    ball.Vel -= n0 * ((1f + restitution) * vn0);
            }
            return;
        }

        Vector2 n = delta / dist;
        float pen = Radius - dist;
        ball.Pos += n * (pen + 0.5f);
        float vn = ball.Vel.Dot(n);
        if (vn < 0f)
        {
            ball.Vel -= n * ((1f + restitution) * vn);
            ball.Vel *= 0.97f;
        }
    }

    private void TryScore(BallBody ball, Vector2 size)
    {
        _ = size;
        if (_scoreCooldown > 0f || ball.IsHeld)
            return;
        if (_hoopGeom.OpeningHalfW <= 0f)
            return;
        if (CrossedDownThroughRim(ball.PrevPos, ball.Pos, ball.Vel, _hoopGeom))
            RegisterScore(ball, _hoopGeom);
    }

    private static bool CrossedDownThroughRim(Vector2 prev, Vector2 cur, Vector2 vel, HoopGeom hoop)
    {
        if (vel.Y < 50f)
            return false;

        Vector2 axis = hoop.RightPost - hoop.LeftPost;
        float axisLen = axis.Length();
        if (axisLen < 1f)
            return false;
        axis /= axisLen;
        Vector2 perp = new(-axis.Y, axis.X);
        if (perp.Y < 0f)
            perp = -perp;

        float prevAlong = (prev - hoop.RimCenter).Dot(axis);
        float curAlong = (cur - hoop.RimCenter).Dot(axis);
        float prevThru = (prev - hoop.RimCenter).Dot(perp);
        float curThru = (cur - hoop.RimCenter).Dot(perp);

        if (prevThru > 4f || curThru < 0f)
            return false;
        float half = hoop.OpeningHalfW * 0.92f;
        return Math.Abs(curAlong) <= half && Math.Abs(prevAlong) <= half * 1.15f;
    }

    private void RegisterScore(BallBody ball, HoopGeom hoop)
    {
        _scoreCooldown = ScoreCooldownout;
        _score++;
        if (_scoreLabel != null)
            _scoreLabel.Text = _score.ToString();

        Vector2 at = hoop.RimCenter + new Vector2(0f, 18f);
        _confetti.Explode(at);
        ball.Vel.Y = Math.Max(ball.Vel.Y, 280f);
        ball.Vel.X *= 0.55f;
        ball.Pos.Y = hoop.RimCenter.Y + Radius * 0.5f;
        BallSync.Instance?.SendScore(ball.Id, 1, at);
        MainFile.Logger.Info($"Basket! score={_score} ball={ball.Id}");
        SyncBallSprite(ball);
        _confettiCanvas?.QueueRedraw();
    }

    private static Vector2 ClampPosition(Vector2 p, Vector2 size)
    {
        float minX = Radius + 8f;
        float maxX = size.X - Radius - 8f;
        float minY = Radius + 8f;
        float maxY = size.Y * FloorYFrac - Radius;
        if (maxX < minX)
            maxX = minX;
        if (maxY < minY)
            maxY = minY;
        return new Vector2(Math.Clamp(p.X, minX, maxX), Math.Clamp(p.Y, minY, maxY));
    }

    private static Vector2 ClampVel(Vector2 v)
    {
        float len = v.Length();
        if (len > MaxSpeed && len > 0.01f)
            return v * (MaxSpeed / len);
        return v;
    }

    private sealed class ConfettiCanvas : Control
    {
        private readonly ConfettiBurst _burst;

        public ConfettiCanvas(ConfettiBurst burst) => _burst = burst;

        public override void _Draw() => _burst.Draw(this);
    }
}
