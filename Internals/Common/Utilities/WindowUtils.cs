using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TanksRebirth.Internals.Common.Utilities;

// this is demanding a rewrite.
public static class WindowUtils {
    // probably prefer a constant "RenderScale" instead of calling ToResolution literally fucking everywhere...
    public static Vector2 RenderResolution => new(1920, 1080);
    public static Vector2 ToResolution(this Vector2 input) => input * (WindowBounds / RenderResolution);
    public static Vector3 ToResolution(this Vector3 input) {
        var scale = (WindowBounds / RenderResolution);

        return input * new Vector3(scale.X, scale.Y, scale.X);
    }
    public static Vector2 ToResolution(this Vector2 input, Vector2 baseRes) => input * (baseRes / WindowBounds);
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
    /// <summary>Converts pixel coordinates (0..WindowWidth, 0..WindowHeight) to normalized coordinates (-1..1, -1..1)</summary>
    public static Vector2 ToNormalisedCoordinates(this Vector2 input) => new Vector2(input.X / WindowWidth - 0.5f, input.Y / WindowHeight - 0.5f) * 2;
    /// <summary>Converts pixel coordinates (0..WindowWidth, 0..WindowHeight) to cartesian coordinates (0..1, 0..1)</summary>
    public static Vector2 ToCartesianCoordinates(this Vector2 input) => new(input.X / WindowWidth, input.Y / WindowHeight);
    /// <summary>Applies <paramref name="kind"/> with the resolution saved in the settings.</summary>
    public static void ChangeWindowKind(WindowKind kind) => ApplyDisplayMode(kind, TankGame.Settings.ResWidth, TankGame.Settings.ResHeight);

    /// <summary>
    /// Switches the window mode and resolution in one go, always starting from a plain window so every switch
    /// (windowed, borderless, fullscreen in any order) ends up in the same state.
    /// </summary>
    /// <remarks>
    /// Windowed uses <paramref name="width"/> x <paramref name="height"/> (clamped to the desktop) and centers the window.
    /// Borderless always covers the desktop at its native resolution.
    /// Fullscreen is exclusive and switches the monitor to <paramref name="width"/> x <paramref name="height"/>
    /// (falls back to the desktop resolution if the monitor doesn't support it).
    /// </remarks>
    public static void ApplyDisplayMode(WindowKind kind, int width, int height) {
        var graphics = TankGame.Instance.Graphics;
        var window = TankGame.Instance.Window;

        // leave fullscreen first: the desktop mode has to be back before we can measure it
        if (graphics.IsFullScreen) {
            graphics.IsFullScreen = false;
            graphics.ApplyChanges();
        }
        var desktop = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;

        switch (kind) {
            case WindowKind.Fullscreen: {
                var supported = false;
                foreach (var mode in GraphicsAdapter.DefaultAdapter.SupportedDisplayModes)
                    if (mode.Width == width && mode.Height == height)
                        supported = true;
                if (!supported) {
                    width = desktop.Width;
                    height = desktop.Height;
                }
                window.IsBorderless = false;
                graphics.HardwareModeSwitch = true;
                graphics.PreferredBackBufferWidth = width;
                graphics.PreferredBackBufferHeight = height;
                graphics.IsFullScreen = true;
                graphics.ApplyChanges();
                break;
            }
            case WindowKind.FullscreenBorderless:
                graphics.HardwareModeSwitch = false;
                window.IsBorderless = true;
                graphics.PreferredBackBufferWidth = desktop.Width;
                graphics.PreferredBackBufferHeight = desktop.Height;
                graphics.ApplyChanges();
                window.Position = Point.Zero;
                break;

            default: {
                graphics.HardwareModeSwitch = false;
                window.IsBorderless = false;
                width = Math.Clamp(width, 640, desktop.Width);
                height = Math.Clamp(height, 480, desktop.Height);
                graphics.PreferredBackBufferWidth = width;
                graphics.PreferredBackBufferHeight = height;
                graphics.ApplyChanges();
                window.Position = new Point((desktop.Width - width) / 2, Math.Max(0, (desktop.Height - height) / 2));
                break;
            }
        }
    }
}
