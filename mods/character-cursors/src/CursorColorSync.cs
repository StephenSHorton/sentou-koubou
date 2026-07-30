using Godot;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace CharacterCursors;

/// <summary>
/// Tracks per-peer custom cursor colors (in-run picker) and rebroadcasts local choice.
/// </summary>
public static class CursorColorSync
{
    private static readonly Dictionary<ulong, Color> PeerCustom = new();
    private static bool _handlersRegistered;
    private static ulong _lastSentMsec;

    public static void EnsureHandlers()
    {
        if (_handlersRegistered)
            return;
        try
        {
            INetGameService? net = RunManager.Instance?.NetService;
            if (net == null)
                return;
            net.RegisterMessageHandler<CursorColorMessage>(OnMessage);
            _handlersRegistered = true;
            MainFile.Logger.Info("Cursor color net sync ready.");
            // Announce current local choice to peers.
            BroadcastLocal(force: true);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Cursor color sync register failed: {e.Message}");
        }
    }

    public static void ResetHandlersFlag()
    {
        // Run end may dispose net service; allow re-register on next run.
        _handlersRegistered = false;
        PeerCustom.Clear();
    }

    public static Color? TryGetPeerCustom(ulong netId) =>
        PeerCustom.TryGetValue(netId, out Color c) ? c : null;

    public static void SetLocalAndBroadcast(Color color, bool useCustom)
    {
        CursorConfig.UseCustomColor = useCustom;
        if (useCustom)
            CursorConfig.CustomColor = color;
        CursorConfig.NotifyChanged();
        BroadcastLocal(force: true);
    }

    public static void BroadcastLocal(bool force = false)
    {
        try
        {
            INetGameService? net = RunManager.Instance?.NetService;
            if (net == null || !net.IsConnected)
                return;
            if ((RunManager.Instance?.State?.Players?.Count ?? 1) <= 1)
                return;

            ulong now = Time.GetTicksMsec();
            if (!force && now - _lastSentMsec < 200)
                return;
            _lastSentMsec = now;

            Color c = CursorConfig.UseCustomColor
                ? CursorConfig.CustomColor
                : (CursorTint.TryGetLocalPrimaryColor() ?? Colors.White);

            net.SendMessage(new CursorColorMessage
            {
                r = c.R,
                g = c.G,
                b = c.B,
                a = c.A,
                useCustom = CursorConfig.UseCustomColor,
            });
        }
        catch (Exception e)
        {
            MainFile.Logger.Debug($"Cursor color broadcast: {e.Message}");
        }
    }

    private static void OnMessage(CursorColorMessage msg, ulong senderId)
    {
        try
        {
            if (msg.useCustom)
                PeerCustom[senderId] = new Color(msg.r, msg.g, msg.b, msg.a);
            else
                PeerCustom.Remove(senderId);

            // Refresh that peer's remote cursor if present.
            RemoteCursorShader.RefreshPeer(senderId);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Cursor color message: {e.Message}");
        }
    }
}
