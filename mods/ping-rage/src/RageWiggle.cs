using Godot;

namespace PingRage;

/// <summary>
/// Attaches to a speech bubble and applies chaotic position/rotation
/// proportional to <see cref="Intensity"/> (0–1+).
/// </summary>
public partial class RageWiggle : Node
{
    public Control? Target { get; set; }
    public float Intensity { get; set; }
    public Vector2 BasePosition { get; set; }

    private float _phase;

    public override void _Process(double delta)
    {
        if (Target == null || !GodotObject.IsInstanceValid(Target))
        {
            QueueFree();
            return;
        }

        if (Intensity < 0.02f)
            return;

        _phase += (float)delta * (10f + Intensity * 55f);

        // Position thrash
        float amp = Intensity * Intensity * 14f + Intensity * 6f;
        float ox = Mathf.Sin(_phase * 1.7f) * amp + Mathf.Sin(_phase * 4.3f) * amp * 0.35f;
        float oy = Mathf.Cos(_phase * 1.9f) * amp * 0.85f + Mathf.Sin(_phase * 5.1f) * amp * 0.25f;
        Target.Position = BasePosition + new Vector2(ox, oy);

        // Rotation thrash (radians)
        float rotAmp = Intensity * Intensity * 0.35f + Intensity * 0.08f;
        Target.Rotation = Mathf.Sin(_phase * 2.4f) * rotAmp + Mathf.Sin(_phase * 6.1f) * rotAmp * 0.4f;
    }
}
