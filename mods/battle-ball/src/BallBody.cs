using Godot;

namespace BattleBall;

/// <summary>One physics ball + its grab hitbox / sprite.</summary>
internal sealed class BallBody
{
    public int Id { get; }
    public Vector2 Pos;
    public Vector2 Vel;
    public Vector2 PrevPos;
    public bool HeldLocal;
    public ulong? HeldByRemote;
    public ulong? Authority;
    public float Spin;
    public ulong IgnoreGrabUntilMsec;
    public int FreeFlightLogLeft;
    public Sprite2D Sprite { get; }
    public Control GrabHit { get; }

    public BallBody(int id, Sprite2D sprite, Control grabHit)
    {
        Id = id;
        Sprite = sprite;
        GrabHit = grabHit;
    }

    public bool IsHeld => HeldLocal || HeldByRemote.HasValue;

    public void DestroyVisuals()
    {
        try
        {
            if (GodotObject.IsInstanceValid(GrabHit))
                GrabHit.QueueFree();
        }
        catch
        {
            // ignore
        }
        try
        {
            if (GodotObject.IsInstanceValid(Sprite))
                Sprite.QueueFree();
        }
        catch
        {
            // ignore
        }
    }
}
