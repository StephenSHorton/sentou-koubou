using Godot;
using Whitney.Scenes.Vfx.HitVfx;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Whitney.Scenes.Vfx.SparkProjectile;

public partial class VfxSparkProjectile : Node2D
{
    public const string ScenePath = "res://Scenes/Vfx/SparkProjectile/VfxSparkProjectile.tscn";

    #region 公开属性
    public Vector2 Velocity = Vector2.Zero;
    public NCreature? PlayerOwner; 
    public Vector2? Target; 
    public float VfxTime => NoIdle ? _chaseDuration : _chaseDuration + _dampingDuration;
    public bool NoIdle;
    public NCard? TargetCard = null;
    public Vector4 Color = new (0.4f, 0.8f, 0.8f, 1.0f);
    #endregion

    #region AnimationParament
    private float _startSpeed = 50f;
    private float _dampingAcceleration = 0.9f;
    private float _dampingDuration = 0.15f;
    private float _chaseDuration = 0.15f;
    private float _chasingSpeedMixMin = 0.4f;
    private float _chasingDirMixMin = 0.4f;
    const float FadingDuration = 1f;
    #endregion
    public void SetAnimationParament(float dampingDuration = 0.15f, float chaseDuration = 0.15f,
        float chasingSpeedMixMin = 0.4f, float chasingDirMixMin = 0.4f)
    {
        _dampingDuration = dampingDuration;
        _chaseDuration = chaseDuration;
        _chasingSpeedMixMin = chasingSpeedMixMin;
        _chasingDirMixMin = chasingDirMixMin;
    }

    #region Child Node and Material
    private Trail? _trail;
    public Trail Trail => _trail ??= GetNode<Trail>("trail");
    private ShaderMaterial? _trailShader;
    private ShaderMaterial TrailShader => _trailShader ??= (ShaderMaterial)Trail.Material;
    private MeshInstance2D? _slug;
    public MeshInstance2D Slug => _slug ??= GetNode<MeshInstance2D>("slug");
    private ShaderMaterial? _slugShader;
    private ShaderMaterial SlugShader => _slugShader ??= (ShaderMaterial)Slug.Material;
    private GpuParticles2D? _particles;
    public GpuParticles2D Particles => _particles ??= GetNode<GpuParticles2D>("particles");
    private ParticleProcessMaterial? _particleProcess;
    public ParticleProcessMaterial ParticleProcess =>
        _particleProcess ??= (ParticleProcessMaterial)Particles.ProcessMaterial;

    private ShaderMaterial? _particleShader;
    public  ShaderMaterial ParticleShader => _particleShader ??= (ShaderMaterial)Particles.Material;
    #endregion

    #region State
    private enum ProjectileState
    {
        Damping, 
        Idle, 
        Chasing, 
        FadingOut
    }
    private ProjectileState _state = ProjectileState.Damping;
    #endregion

    #region State Time Counter
    private float _dampingTimeLeft;
    private float _chaseTimeLeft;
    private float _fadeTimeLeft;
    #endregion
    
    private void ResetShader()
    {
        SlugShader.SetShaderParameter("visible", 1.0);
        TrailShader.SetShaderParameter("visible", 1.0);
    }

    public void StartDamping()
    {
        if (_state == ProjectileState.FadingOut) ResetShader();
        _state = ProjectileState.Damping;
        _dampingTimeLeft = _dampingDuration;
    }

    private void UpdateDamping(double delta)
    {
        float deltaF = (float)delta;
        float speed = Velocity.Length();
        float minSpeed = _dampingAcceleration * deltaF * 0.001f;
        if (speed > _dampingAcceleration * deltaF)
        {
            speed -= _dampingAcceleration * deltaF;
        }
        else
        {
            speed = minSpeed;
        }

        Velocity = Velocity.Normalized() * speed;

        _dampingTimeLeft -= deltaF;
        Trail.LifeTimeOverwrite = _dampingTimeLeft;
        //切换状态
        if ((_dampingTimeLeft <= 0 || speed <= minSpeed) && _state == ProjectileState.Damping)
        {
            Velocity = Velocity.Normalized() * minSpeed;
            if (NoIdle)
                StartChasing();
            else
                StartIdle();
        }
    }

    public void StartIdle()
    {
        if (_state == ProjectileState.FadingOut) ResetShader();
        _state = ProjectileState.Idle;
    }

    private void UpdateIdle(double delta)
    {
        if (Trail.LifeTimeOverwrite > 4 * (float)delta)
            Trail.LifeTimeOverwrite -= 2 * (float)delta;
        else
            Trail.LifeTimeOverwrite = 0.0001f;
    }

    public void StartChasing(Vector2? target = null)
    {
        if (_state == ProjectileState.FadingOut) ResetShader();
        Trail.CleanTrail();
        if (target != null)
            Target = target;
        _state = ProjectileState.Chasing;
        _chaseTimeLeft = _chaseDuration;
        Particles.Emitting = true;
        Trail.LifeTimeOverwrite = -1;
    }

    private void UpdateChasing(double delta)
    {
        if (Target == null)
        {
            StartFading();
            return;
        }

        Vector2 target = (Vector2)Target;
        float deltaF = (float)delta;
        _chaseTimeLeft -= deltaF;

        if (_chaseTimeLeft <= 0 || deltaF > _chaseTimeLeft + 0.001f)
        {
            // 到达目标
            Position = target;
            //Velocity = Vector2.Zero;
            Particles.Emitting = false;
            StartFading();
            return;
        }

        Vector2 diff = target - Position;
        if (diff.IsZeroApprox())
        {
            Particles.Emitting = false;
            StartFading();
            return;
        }

        // 计算目标速度（基于剩余时间）
        Vector2 targetVelocity = diff / _chaseTimeLeft;
        float targetSpeed = targetVelocity.Length();
        float oldSpeed = Velocity.Length();

        // 速度混合：progress 0→1，时间从 ChaseDuration 到 0
        float progress = 1f - Mathf.Min(1f, _chaseTimeLeft / _chaseDuration);
        float speedMix = Mathf.Clamp(progress + 0.4f, 0f, 1f);
        targetSpeed = Mathf.Lerp(oldSpeed, targetSpeed, speedMix);
        targetVelocity = targetVelocity.Normalized() * targetSpeed;

        float dirMix = Mathf.Clamp(progress + 0.4f, 0f, 1f);
        Vector2 oldDirection = Velocity.LengthSquared() > 0.01f ? Velocity.Normalized() : targetVelocity.Normalized();
        Velocity = targetVelocity * dirMix + oldDirection * targetSpeed * (1f - dirMix);
    }

    public void StartFading()
    {
        _state = ProjectileState.FadingOut;
        _fadeTimeLeft = FadingDuration;
        SparkHitVfx burst = GD.Load<PackedScene>("res://Scenes/Vfx/HitVfx/BurstSpark.tscn").Instantiate<SparkHitVfx>();
        burst.SetColor(Color);
        burst.Position = Position;
        GetParent().AddChild(burst);
    }

    private void UpdateFading(double delta)
    {
        _fadeTimeLeft -= (float)delta;

        var alpha = Mathf.Max(0f, 2.0f * (_fadeTimeLeft - 0.5f));
        SlugShader.SetShaderParameter("visible", alpha);
        TrailShader.SetShaderParameter("visible", alpha);
        SlugShader.SetShaderParameter("projectile_speed", 0f);
        SlugShader.SetShaderParameter("explosion_progress", Mathf.Clamp(5.0*(1.0f-_fadeTimeLeft), 0.0f, 0.88f));
        
        if (_fadeTimeLeft < 0f)
        {
            QueueFree();
        }
    }
    private void UpdateTrailParticles(Vector2 displacement)
    {
        if (_state != ProjectileState.Chasing)
        {
            Particles.Emitting = false;
            return;
        }

        const float particleDensity = 1f;
        float amount = particleDensity * displacement.Length() + 0.5f;
        Particles.Position = -0.5f * displacement;
        Particles.AmountRatio = Mathf.Min(1f, amount / 100f);
        Particles.Rotation = displacement.Angle();
        ParticleProcess.EmissionBoxExtents = new Vector3(displacement.Length(), 1f, 0f);
    }

    private void UpdateMovement(double delta)
    {
        if (Velocity.LengthSquared() > 0.0001f)
        {
            if (_state != ProjectileState.FadingOut)
                Slug.Rotation = Velocity.Angle();
            else
                Slug.Rotation = 0;
            var speedNormalized = Mathf.Clamp(Velocity.Length() / _startSpeed, 0, 1);
            speedNormalized = Mathf.Pow(speedNormalized, 0.4f);
            SlugShader.SetShaderParameter("projectile_speed", speedNormalized);
        }

        Vector2 oldVelocity = Velocity;
        switch (_state)
        {
            case ProjectileState.Damping:
                UpdateDamping(delta);
                break;
            case ProjectileState.Idle:
                UpdateIdle(delta);
                break;
            case ProjectileState.Chasing:
                UpdateChasing(delta);
                break;
            case ProjectileState.FadingOut:
                UpdateFading(delta);
                break;
        }

        // 更新位置和拖尾粒子（仅在移动状态）
        if (_state != ProjectileState.FadingOut)
        {
            Vector2 displacement = Velocity * (float)delta;
            UpdateTrailParticles(displacement);
            Position += displacement;
            float turnAmount = Mathf.Abs(oldVelocity.AngleTo(Velocity)) / (Mathf.Pi / 36f);
            SlugShader.SetShaderParameter("turn_amount", Mathf.Min(turnAmount, 1f));
        }
    }
    

    public override void _Process(double delta)
    {
        UpdateMovement(delta);
    }


    public void SetColor(Vector4 color)
    {
        Color = color;
        SlugShader.SetShaderParameter("spark_color", color);
        TrailShader.SetShaderParameter("spark_color", color);
        ParticleShader.SetShaderParameter("spark_color", color);
    }

    public void ApplySize(float scale)
    {
        Slug.Scale *= scale;
        Trail.Scale *= scale;
    }

    public void ApplySizeFromDamage(int damage)
    {
        float damageCut = Mathf.Clamp(damage, 2f, 128f);
        float scale = Mathf.Log(damageCut) / Mathf.Log(8);
        ApplySize(scale);
    }

    public void VelocityInit(float dir = 0, float spread = 0.4f * Mathf.Pi)
    {
        Vector2 containerSize = NCombatRoom.Instance?.CombatVfxContainer.Size ?? new Vector2(1, 640);
        float baseScale = containerSize.Y;
        float dampingDistance = 0.2f * baseScale;

        float speed = 2.0f * dampingDistance / _dampingDuration;
        _startSpeed = speed;
        _dampingAcceleration = _startSpeed / _dampingDuration;

        dir += -spread + 2 * spread * GD.Randf();

        Velocity = speed * Vector2.FromAngle(dir);
        Velocity *= (float)GD.RandRange(0.95f, 1.05f);
    }

    public static VfxSparkProjectile Create()
    {
        VfxSparkProjectile instance = GD.Load<PackedScene>(ScenePath)
            .Instantiate<VfxSparkProjectile>();
        return instance;
    }

    public static VfxSparkProjectile Create(NCreature player, Vector4 color)
    {
        VfxSparkProjectile vfx = Create();
        vfx.SetColor(color);
        vfx.Position = player.VfxSpawnPosition;
        vfx.PlayerOwner = player;
        vfx.VelocityInit();
        vfx.StartDamping();
        return vfx;
    }

    public static VfxSparkProjectile Create(CardModel card, Vector4 color)
    {
        NCreature? player = card.Owner.Creature.GetCreatureNode();
        if (player == null) return Create();
        VfxSparkProjectile vfx = Create(player, color);
        vfx.ApplySizeFromDamage((int)card.DynamicVars.Damage.PreviewValue);
        return vfx;
    }

    public static VfxSparkProjectile Create(CardModel card, Vector4 color, NCreature target)
    {
        VfxSparkProjectile vfx = Create(card, color);
        vfx.Target = target.VfxSpawnPosition;
        vfx.NoIdle = true;
        return vfx;
    }
}