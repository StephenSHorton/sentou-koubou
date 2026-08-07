using Godot;

namespace BattleBall;

/// <summary>Simple 2D confetti shower drawn in the ball world (no GPU particles dependency).</summary>
public sealed class ConfettiBurst
{
    private readonly List<Particle> _parts = new(96);
    private float _life;

    private struct Particle
    {
        public Vector2 Pos;
        public Vector2 Vel;
        public Color Color;
        public float Size;
        public float Spin;
        public float SpinVel;
        public float Age;
        public float MaxAge;
    }

    private static readonly Color[] Palette =
    [
        new(1f, 0.25f, 0.3f),
        new(1f, 0.85f, 0.2f),
        new(0.3f, 0.85f, 1f),
        new(0.45f, 1f, 0.4f),
        new(1f, 0.45f, 0.85f),
        new(1f, 1f, 1f),
        new(1f, 0.55f, 0.15f),
    ];

    public bool Alive => _parts.Count > 0;

    public void Explode(Vector2 origin, int count = 72)
    {
        _parts.Clear();
        _life = 0f;
        var rng = new Random();
        for (int i = 0; i < count; i++)
        {
            float ang = (float)(rng.NextDouble() * Math.PI * 2);
            float spd = 280f + (float)rng.NextDouble() * 520f;
            _parts.Add(new Particle
            {
                Pos = origin + new Vector2(
                    (float)(rng.NextDouble() - 0.5) * 20f,
                    (float)(rng.NextDouble() - 0.5) * 12f),
                Vel = new Vector2(MathF.Cos(ang), MathF.Sin(ang) - 0.35f) * spd,
                Color = Palette[rng.Next(Palette.Length)],
                Size = 3.5f + (float)rng.NextDouble() * 5f,
                Spin = (float)rng.NextDouble() * MathF.Tau,
                SpinVel = ((float)rng.NextDouble() - 0.5f) * 14f,
                Age = 0f,
                MaxAge = 0.9f + (float)rng.NextDouble() * 0.9f,
            });
        }
    }

    public void Update(float dt)
    {
        if (_parts.Count == 0)
            return;
        _life += dt;
        for (int i = _parts.Count - 1; i >= 0; i--)
        {
            Particle p = _parts[i];
            p.Age += dt;
            if (p.Age >= p.MaxAge)
            {
                _parts.RemoveAt(i);
                continue;
            }
            p.Vel.Y += 900f * dt;
            p.Vel *= 0.985f;
            p.Pos += p.Vel * dt;
            p.Spin += p.SpinVel * dt;
            _parts[i] = p;
        }
    }

    public void Draw(CanvasItem canvas)
    {
        foreach (Particle p in _parts)
        {
            float t = 1f - p.Age / p.MaxAge;
            Color c = p.Color;
            c.A = Math.Clamp(t, 0f, 1f);
            // Tiny rotated rect as confetti scrap
            Vector2 half = new(p.Size, p.Size * 0.45f);
            Vector2[] pts =
            [
                Rotate(new Vector2(-half.X, -half.Y), p.Spin) + p.Pos,
                Rotate(new Vector2(half.X, -half.Y), p.Spin) + p.Pos,
                Rotate(new Vector2(half.X, half.Y), p.Spin) + p.Pos,
                Rotate(new Vector2(-half.X, half.Y), p.Spin) + p.Pos,
            ];
            canvas.DrawColoredPolygon(pts, c);
        }
    }

    private static Vector2 Rotate(Vector2 v, float ang)
    {
        float c = MathF.Cos(ang);
        float s = MathF.Sin(ang);
        return new Vector2(v.X * c - v.Y * s, v.X * s + v.Y * c);
    }
}
