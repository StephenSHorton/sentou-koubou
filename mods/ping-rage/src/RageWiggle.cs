using Godot;

namespace PingRage;

/// <summary>
/// Chaotic position/rotation thrash on a speech bubble.
/// Intensity is already pre-shaped by the caller (higher when mashing).
/// </summary>
public partial class RageWiggle : Node
{
    public Control? Target { get; set; }
    public float Intensity { get; set; }
    public Vector2 BasePosition { get; set; }

    private float _phase;
    private float _seed;

    public override void _Ready()
    {
        _seed = (float)GD.RandRange(0.0, 100.0);
    }

    public override void _Process(double delta)
    {
        if (Target == null || !GodotObject.IsInstanceValid(Target))
        {
            QueueFree();
            return;
        }

        float i = Mathf.Max(0f, Intensity);
        // Spin up faster the angrier we are.
        _phase += (float)delta * (14f + i * 90f + i * i * 40f);

        float p = _phase + _seed;
        // Multi-frequency thrash — gets wild at high intensity.
        float amp = i * 8f + i * i * 28f + i * i * i * 18f;
        float ox = Mathf.Sin(p * 1.9f) * amp
                   + Mathf.Sin(p * 5.7f) * amp * 0.55f
                   + Mathf.Sin(p * 11.3f) * amp * 0.25f;
        float oy = Mathf.Cos(p * 2.1f) * amp * 0.9f
                   + Mathf.Sin(p * 6.4f) * amp * 0.4f
                   + Mathf.Cos(p * 13.1f) * amp * 0.2f;
        Target.Position = BasePosition + new Vector2(ox, oy);

        float rotAmp = i * 0.12f + i * i * 0.55f + i * i * i * 0.45f;
        Target.Rotation = Mathf.Sin(p * 2.8f) * rotAmp
                          + Mathf.Sin(p * 7.2f) * rotAmp * 0.55f
                          + Mathf.Cos(p * 14.5f) * rotAmp * 0.3f;

        // Slight scale jitter when fully unhinged.
        if (i > 0.7f)
        {
            float sJitter = 1f + Mathf.Sin(p * 18f) * (i - 0.7f) * 0.12f;
            // Don't overwrite base rage scale completely — multiply gently via modulate size is hard;
            // apply tiny extra scale on top of existing if we stored base; skip if unknown.
            // Use skew-ish via rotation only above.
            _ = sJitter;
        }
    }
}
