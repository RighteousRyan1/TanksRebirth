using Microsoft.Xna.Framework;

namespace TanksRebirth.Internals.Common.Utilities;

public static class MouseUtils {
    static uint _lastUsedUpdate;
    static Vector2 _velCache;
    static Vector2 _oldMousePos;
    public static int MouseX => (int)MousePosition.X;
    public static int MouseY => (int)MousePosition.Y;

    /// <summary>A useful developer utility to help debug things without having to constantly change a value and inspect manually.</summary>
    /// <remarks>This value is the mouse's position (X, Y) divided by the window's bounds (Width, Height).</remarks>
    public static Vector2 Test => MousePosition / WindowUtils.WindowBounds;
    public static Vector2 MousePosition;
    public static bool MouseOnScreen => MousePosition.X >= 0 && MousePosition.X <= WindowUtils.WindowWidth && MousePosition.Y >= 0 && MousePosition.Y < WindowUtils.WindowHeight;
    public static bool MouseOnScreenProtected => MousePosition.X > 16 && MousePosition.X < WindowUtils.WindowWidth - 16 && MousePosition.Y > 16 && MousePosition.Y < WindowUtils.WindowHeight - 16;

    /// <summary>
    /// The velocity of the mouse (the old position - new position)
    /// </summary>
    public static Vector2 MouseVelocity { get; internal set; }
    public static Vector2 GetMouseVelocity(Vector2 fromOffset = default) {
        if (RuntimeData.UpdateCount == _lastUsedUpdate)
            return _velCache;
        var pos = fromOffset == default ? MousePosition : fromOffset;
        var diff = pos - _oldMousePos;

        _lastUsedUpdate = RuntimeData.UpdateCount;
        _velCache = diff;
        return diff;
    }
}