using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Whitney.Scenes.Vfx.HitVfx;

public partial class SparkHitVfx : Node2D
{
    private float _timeLeft = 1.5f;

    #region 子节点与资产
    private ShaderMaterial? _big;
    private ShaderMaterial Big => _big ??= (ShaderMaterial)GetNode<GpuParticles2D>("big").Material;
    private ShaderMaterial? _lasting;
    private ShaderMaterial Lasting => _lasting ??= (ShaderMaterial)GetNode<GpuParticles2D>("lasting").Material;
    private ShaderMaterial? _fade;
    private ShaderMaterial Fade => _fade ??= (ShaderMaterial)GetNode<GpuParticles2D>("fade").Material;
    #endregion

    public override void _Process(double delta)
    {
        if (_timeLeft > 0)
            _timeLeft -= (float)delta;
        else
        {
            this.QueueFreeSafely();
        }
    }
    public void SetColor(Vector4 color)
    {
        Big.SetShaderParameter("spark_color", color);
        Lasting.SetShaderParameter("spark_color", color);
        Fade.SetShaderParameter("spark_color", color);
    }

    public static SparkHitVfx? Create(string name)
    {
        return GD.Load<PackedScene>($"res://Scenes/Vfx/HitVfx/{name}.tscn")?.Instantiate<SparkHitVfx>();
    }

    public static SparkHitVfx? Create(Vector2 targetPos,string name)
    {
        var vfx = Create(name);
        if  (vfx != null)
            vfx.Position = targetPos;
        return vfx;
    }
    public static Node2D? Create(NCreature? target, string name)
    {
        return target?.VfxSpawnPosition == null ? null : Create(target.VfxSpawnPosition,name);
    }
    public static Node2D? Create(NCreature? target, string name,Vector4 color)
    {
        var vfx = target?.VfxSpawnPosition == null ? null : Create(target.VfxSpawnPosition,name);
        vfx?.SetColor(color);
        return vfx;
    }
}