using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;

namespace CharacterCursors;

[HarmonyPatch(typeof(NCursorManager), nameof(NCursorManager._Ready))]
public static class CursorManagerReadyPatch
{
    public static void Postfix()
    {
        // Deferred: run state / player may not exist yet at main-menu ready.
        Callable.From(CursorTint.ApplyLocalCursor).CallDeferred();
    }
}

/// <summary>Game sometimes clears overrides; re-assert character tint.</summary>
[HarmonyPatch(typeof(NCursorManager), nameof(NCursorManager.StopOverridingCursor))]
public static class StopOverridingCursorPatch
{
    public static void Postfix()
    {
        Callable.From(CursorTint.ApplyLocalCursor).CallDeferred();
    }
}

[HarmonyPatch(typeof(NRun), nameof(NRun._Ready))]
public static class RunReadyPatch
{
    public static void Postfix()
    {
        CursorTint.ClearAppliedCache();
        CursorColorSync.ResetHandlersFlag();
        Callable.From(CursorTint.ApplyLocalCursor).CallDeferred();
        Callable.From(CursorColorHud.Ensure).CallDeferred();
        Callable.From(() => CursorColorSync.EnsureHandlers()).CallDeferred();
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterRoomEntered))]
public static class AfterRoomEnteredPatch
{
    public static void Postfix()
    {
        CursorTint.ApplyLocalCursor();
    }
}

/// <summary>Subscribe once when RunManager appears.</summary>
[HarmonyPatch(typeof(RunManager), nameof(RunManager.Instance), MethodType.Getter)]
public static class RunManagerInstancePatch
{
    private static bool _hooked;

    public static void Postfix(RunManager __result)
    {
        if (_hooked || __result == null)
            return;
        try
        {
            __result.RunStarted += _ =>
            {
                CursorTint.ClearAppliedCache();
                Callable.From(CursorTint.ApplyLocalCursor).CallDeferred();
            };
            _hooked = true;
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"RunStarted hook failed: {e.Message}");
        }
    }
}

/// <summary>Remote multiplayer cursors: shader tint to that player's character color.</summary>
[HarmonyPatch(typeof(NRemoteMouseCursor), nameof(NRemoteMouseCursor._Ready))]
public static class RemoteCursorReadyPatch
{
    public static void Postfix(NRemoteMouseCursor __instance)
    {
        RemoteCursorShader.Apply(__instance);
    }
}

[HarmonyPatch(typeof(NRemoteMouseCursor), "UpdateImage")]
public static class RemoteCursorUpdateImagePatch
{
    public static void Postfix(NRemoteMouseCursor __instance, bool isDown, DrawingMode drawingMode)
    {
        // Only tint the normal pointer; leave map draw/erase tools alone.
        if (drawingMode == DrawingMode.None)
            RemoteCursorShader.Apply(__instance);
        else
            RemoteCursorShader.Clear(__instance);
    }
}

internal static class RemoteCursorShader
{
    private const string ShaderCode =
        """
        shader_type canvas_item;
        render_mode blend_mix;

        uniform vec4 tint_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
        uniform float outline_lum_threshold = 0.2;

        void fragment() {
            vec4 tex_color = texture(TEXTURE, UV);
            if (tex_color.a < 0.1) {
                COLOR = vec4(0.0);
            } else {
                float lum = dot(tex_color.rgb, vec3(0.299, 0.587, 0.114));
                if (lum < outline_lum_threshold) {
                    COLOR = vec4(0.12, 0.12, 0.12, tex_color.a);
                } else {
                    COLOR = vec4(lum * tint_color.rgb, tex_color.a);
                }
            }
        }
        """;

    private static Shader? _shader;

    public static void Apply(NRemoteMouseCursor cursor)
    {
        if (cursor == null || !GodotObject.IsInstanceValid(cursor))
            return;

        try
        {
            Color? color = CursorTint.ResolvePeerTintColor(cursor.PlayerId);
            if (color == null)
                return;

            var textureRect = cursor.GetNodeOrNull<TextureRect>("TextureRect");
            if (textureRect == null)
                return;

            _shader ??= CreateShader();
            var material = new ShaderMaterial { Shader = _shader };
            material.SetShaderParameter("tint_color", color.Value);
            material.SetShaderParameter("outline_lum_threshold", CursorTint.OutlineLumThreshold);
            textureRect.Material = material;
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Remote cursor tint failed: {e.Message}");
        }
    }

    /// <summary>Re-tint all remote cursors for a peer after a color message.</summary>
    public static void RefreshPeer(ulong playerId)
    {
        try
        {
            SceneTree? tree = Engine.GetMainLoop() as SceneTree;
            if (tree?.Root == null)
                return;
            RefreshPeerRecursive(tree.Root, playerId);
        }
        catch
        {
            // ignore
        }
    }

    private static void RefreshPeerRecursive(Node node, ulong playerId)
    {
        if (node is NRemoteMouseCursor cursor && cursor.PlayerId == playerId)
            Apply(cursor);
        foreach (Node child in node.GetChildren())
            RefreshPeerRecursive(child, playerId);
    }

    public static void Clear(NRemoteMouseCursor cursor)
    {
        try
        {
            var textureRect = cursor.GetNodeOrNull<TextureRect>("TextureRect");
            if (textureRect != null)
                textureRect.Material = null;
        }
        catch
        {
            // ignore
        }
    }

    private static Shader CreateShader()
    {
        var shader = new Shader { Code = ShaderCode };
        _ = shader.GetRid();
        return shader;
    }
}
