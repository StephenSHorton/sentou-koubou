using Godot;
using Whitney.WhitneyCode.Powers;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Whitney.Scenes.Vfx.Inkbound;

public partial class InkboundVfx : Node2D
{
    public const string ScenePath = "res://Scenes/Vfx/Inkbound/InkboundVfx.tscn";
    #region Child Node and Material
    private Sprite2D? _star;
    public Sprite2D Star => _star ??= GetNode<Sprite2D>("star");
    private ShaderMaterial? _starShader;
    private ShaderMaterial StarShader => _starShader ??= (ShaderMaterial)Star.Material;
    
    private Sprite2D? _ring;
    public Sprite2D Ring => _ring ??= GetNode<Sprite2D>("ring");
    private ShaderMaterial? _ringShader;
    private ShaderMaterial RingShader => _ringShader ??= (ShaderMaterial)Ring.Material;
    
    private GpuParticles2D? _particle;
    public GpuParticles2D Particle => _particle ??= GetNode<GpuParticles2D>("particle");
    private GpuParticles2D? _explodeParticle;
    public GpuParticles2D ExplodeParticle => _explodeParticle ??= GetNode<GpuParticles2D>("explode");
    #endregion
    
    #region 公开属性
    public Vector2 Velocity = Vector2.Zero;
    public NCreature? NCreatureOwner;
    public InkboundPower? PowerOwner;
    #endregion
    #region 
    private float _startSpeed;
    private float _orbitPhase = Mathf.Tau;
    private float _orbitSpeed = 0.7f;
    private float _ellipseRotation = Mathf.Tau;
    private float _ellipseRotationSpeed = 0.13f;
    private float _majorRadius;
    private float _minorRadius;
    private bool _exploding;
    private float _explodeTime;
    private bool _killAfterExplode;
    #endregion
    private void UpdateWandering(float delta)
    {
        if (NCreatureOwner == null)
            return;

        Vector2 center = NCreatureOwner.Hitbox.GlobalPosition + NCreatureOwner.Hitbox.Size / 2f + new Vector2(0,-50);

        _orbitPhase += _orbitSpeed * delta;
        _ellipseRotation += _ellipseRotationSpeed * delta;

        // 椭圆上的局部点
        Vector2 localTarget = new Vector2(
            Mathf.Cos(_orbitPhase) * _majorRadius,
            Mathf.Sin(_orbitPhase) * _minorRadius
        );

        // 椭圆整体旋转
        Vector2 targetPos = center + localTarget.Rotated(_ellipseRotation);

        Vector2 toTarget = targetPos - Position;
        float dist = toTarget.Length();
        
        float desiredSpeed = Mathf.Lerp(
            0.35f * _startSpeed,
            0.85f * _startSpeed,
            Mathf.Clamp(dist / 80f, 0f, 1f)
        );
        if (_exploding) desiredSpeed *= 0.15f;
        if (toTarget.Length() <= desiredSpeed)
        {
            Velocity = toTarget;
        }
        else
        {
            Vector2 desiredVelocity = toTarget.Normalized() * desiredSpeed;
            float steering = 4.0f;
            Velocity = Velocity.Lerp(desiredVelocity, steering * delta);
        }
        Position += Velocity * delta;
    }
    public void StartExplosion(bool killAfterExplode = false)
    {
        _exploding = true;
        _explodeTime = 0f;
        ExplodeParticle.Emitting = true;
        RingShader.SetShaderParameter("power", 1.0f);
        //ExplodeParticle.Restart();
        _killAfterExplode = killAfterExplode;
        if (killAfterExplode)
        {
            Particle.Emitting = false;
            Star.Visible = false;
        }
    }
    public override void _Process(double delta)
    {
        UpdateWandering((float)delta);
        if (!_exploding) return;
        _explodeTime += (float)delta;
        Ring.Scale *= 1.1f;
        RingShader.SetShaderParameter("power", 1.0f - _explodeTime);
        if (!(_explodeTime > 1.0f)) return;
        if (_killAfterExplode)
        {
            this.QueueFreeSafely();
        }
        else
        {
            _exploding = false;
            PowerOwner?.UpdateVfx();
            Ring.Scale = new Vector2(1, 1);
        }
    }
    
    public void ApplySize(int starliCount)
    {
        int size = Mathf.Clamp(starliCount, 1, 256);
        var logRatio = Mathf.Log(size) / Mathf.Log(2) / 8.0f;
        var scale = 3f + 3f * logRatio;
        Scale = new Vector2(scale, scale);
        float armFactor = 1f + 0.5f* logRatio;
        StarShader.SetShaderParameter("arm_factor", armFactor);
        RingShader.SetShaderParameter("power", logRatio);
        Particle.AmountRatio = 0.2f + 0.8f* logRatio;
    }

    public void MovementInit()
    {
        Vector2 containerSize = NCombatRoom.Instance?.CombatVfxContainer.Size ?? new Vector2(1,640);
        float baseScale = containerSize.Y;
        float dampingDistance = 0.15f * baseScale;//减速距离为0.15个屏幕宽度
        
        float speed = 2.0f* dampingDistance / 0.5f;
        _startSpeed = speed;
        //随机化方向
        float dir = -0.4f * Mathf.Pi + 0.8f * Mathf.Pi * GD.Randf();
        Velocity = speed * Vector2.FromAngle(dir);
        
        _majorRadius = baseScale * 0.2f *(float)GD.RandRange(1f, 1.25f);
        _minorRadius = baseScale * 0.1f*(float)GD.RandRange(0.8f, 1f);
        _orbitPhase *= GD.Randf();
        _ellipseRotation *= GD.Randf();
        _orbitSpeed = (float)GD.RandRange(0.7f, 1.4f);
        _ellipseRotationSpeed = (float)GD.RandRange(0.1f, 0.3f);
        if (GD.Randf() < 0.5f)
            _ellipseRotationSpeed *= -1;
    }
    
    public static InkboundVfx Create(bool darkVersion = false)
    {
        var instance = GD.Load<PackedScene>(ScenePath)
            .Instantiate<InkboundVfx>();
        //if (darkVersion)instance.Particle.Material = 
        return instance;
    }
    public static InkboundVfx Create(InkboundPower powerOwner,bool darkVersion = false)
    {
        InkboundVfx vfx = Create(darkVersion);
        //vfx.Position = ownerCreature.Hitbox.GlobalPosition + ownerCreature.Hitbox.Size / 2f + new Vector2(0,-10);
        vfx.PowerOwner = powerOwner;
        var owner = powerOwner.Owner.GetCreatureNode();
        if (owner == null) return vfx;
        vfx.Position = owner.VfxSpawnPosition;
        vfx.NCreatureOwner = owner;
        NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(vfx);
        vfx.MovementInit();
        return vfx;
    }
}

