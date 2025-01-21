using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.Internals.Common.Utilities;

public static class WindowUtils
{
    public static Vector2 RenderResolution => new(1920, 1080);
    public static Vector2 ToResolution(this Vector2 input) => input * (WindowBounds / RenderResolution);
    public static Vector2 ToResolution(this float input) => input * (WindowBounds / RenderResolution);
    public static float ToResolutionF(this float input) => input * (WindowBounds / RenderResolution).Length();
    public static Rectangle ToResolution(this Rectangle input) => new((int)(input.X * (WindowBounds.X / 1920)), (int)(input.Y * WindowBounds.Y / 1080), (int)(input.Width * (WindowBounds.X / 1920)), (int)(input.Height * WindowBounds.Y / 1080));
    public static float ToResolutionX(this float input) => ToResolution(input).X;
    public static float ToResolutionY(this float input) => ToResolution(input).Y;
    public static float ToResolutionX(this int input) => ToResolution(input).X;
    public static float ToResolutionY(this int input) => ToResolution(input).Y;
    public static int WindowWidth => TankGame.Instance.Window.ClientBounds.Width;
    public static int WindowHeight => TankGame.Instance.Window.ClientBounds.Height;
    public static Vector2 WindowBounds => new(WindowWidth, WindowHeight);
    public static Vector2 WindowCenter => WindowBounds / 2;
    public static Vector2 WindowBottom => new(WindowWidth / 2, WindowHeight);
    public static Vector2 WindowTop => new(WindowWidth / 2, 0);
    public static Vector2 WindowTopRight => new(WindowWidth, 0);
    public static Vector2 WindowBottomRight => new(WindowWidth, WindowHeight);
    public static Vector2 WindowTopLeft => new(0, 0);
    public static Vector2 WindowBottomLeft => new(0, WindowHeight);
    public static Vector2 WindowLeft => new(0, WindowHeight / 2);
    public static Vector2 WindowRight => new(WindowWidth, WindowHeight / 2);
    public static bool WindowActive => TankGame.Instance.IsActive;
    public static Rectangle ScreenRect => new(0, 0, WindowWidth, WindowHeight);
    public static Vector2 ToNormalisedCoordinates(this Vector2 input) => new Vector2(input.X / WindowWidth - 0.5f, input.Y / WindowHeight - 0.5f) * 2;
    public static Vector2 ToCartesianCoordinates(this Vector2 input) => new(input.X / WindowWidth, input.Y / WindowHeight);
}
