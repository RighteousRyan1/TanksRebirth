using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.Internals.Common.Utilities;

public static class MouseUtils
{
    public static int MouseX => (int)MousePosition.X;
    public static int MouseY => (int)MousePosition.Y;
    public static Vector2 MousePosition;
    public static bool MouseOnScreen => MousePosition.X >= 0 && MousePosition.X <= WindowUtils.WindowWidth && MousePosition.Y >= 0 && MousePosition.Y < WindowUtils.WindowHeight;
    public static bool MouseOnScreenProtected => MousePosition.X > 16 && MousePosition.X < WindowUtils.WindowWidth - 16 && MousePosition.Y > 16 && MousePosition.Y < WindowUtils.WindowHeight - 16;
    private static uint _lastUsedUpdate;
    private static Vector2 _velCache;
    private static Vector2 _oldMousePos;
    public static Vector2 GetMouseVelocity(Vector2 fromOffset = default)
    {
        if (TankGame.UpdateCount == _lastUsedUpdate)
            return _velCache;
        var pos = fromOffset == default ? MousePosition : fromOffset;
        var diff = pos - _oldMousePos;

        _lastUsedUpdate = TankGame.UpdateCount;
        _velCache = diff;
        return diff;
    }
}
