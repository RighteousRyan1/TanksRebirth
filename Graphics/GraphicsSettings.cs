using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using TanksRebirth.GameContent.Tanks;

namespace TanksRebirth.Graphics;

/// <summary>
/// Applies the graphics options from <see cref="GameConfig"/> that need engine-side work: anti-aliasing of the
/// game frame buffer, the frame rate cap and the tank track limit. The settings page and the game loop call these.
/// </summary>
public static class GraphicsSettings {
    /// <summary>The MSAA levels offered in the settings (0 = off).</summary>
    public static readonly int[] MSAAOptions = [0, 2, 4, 8];
    /// <summary>The frame rate caps offered in the settings (0 = unlimited).</summary>
    public static readonly int[] FrameRateOptions = [30, 60, 75, 120, 144, 165, 240, 360, 0];
    /// <summary>The tank track limits offered in the settings.</summary>
    public static readonly int[] FootprintLimitOptions = [1000, 2500, 5000, 10000, 20000, 50000];

    static int _frameBufferSamples = -1;

    // ------------------------------------------------------------------------------------------ anti-aliasing

    /// <summary>
    /// True when the game frame buffer has to be (re)created: missing, wrong size, or the MSAA setting changed.
    /// </summary>
    public static bool FrameBufferNeedsRecreate(RenderTarget2D? buffer, Point size) =>
        buffer is null || buffer.IsDisposed || buffer.Width != size.X || buffer.Height != size.Y ||
        _frameBufferSamples != Math.Max(0, TankGame.Settings.MSAASamples);

    /// <summary>Creates the game frame buffer with the MSAA level from the settings.</summary>
    public static RenderTarget2D CreateFrameBuffer(GraphicsDevice device) {
        var pp = device.PresentationParameters;
        _frameBufferSamples = Math.Max(0, TankGame.Settings.MSAASamples);
        return new RenderTarget2D(device, pp.BackBufferWidth, pp.BackBufferHeight, false, pp.BackBufferFormat,
            pp.DepthStencilFormat, _frameBufferSamples, RenderTargetUsage.PreserveContents);
    }

    // ------------------------------------------------------------------------------------------ frame rate

    /// <summary>
    /// Sets the frame pacing from the settings. With VSync on the monitor paces the game; with VSync off
    /// <see cref="GameConfig.TargetFPS"/> caps it (0 = unlimited). Call once per update.
    /// </summary>
    public static void UpdateFrameTiming(Game game) {
        var settings = TankGame.Settings;
        var capped = !settings.Vsync && settings.TargetFPS > 0;
        var fixedStep = capped || !RuntimeData.Interp;
        if (game.IsFixedTimeStep != fixedStep)
            game.IsFixedTimeStep = fixedStep;

        var target = TimeSpan.FromSeconds(RuntimeData.Interp && settings.TargetFPS > 0 ? 1.0 / settings.TargetFPS : 1.0 / 60.0);
        if (game.TargetElapsedTime != target)
            game.TargetElapsedTime = target;
    }

    // ------------------------------------------------------------------------------------------ tank tracks

    /// <summary>Changes how many tank tracks can exist at once, keeping the newest ones.</summary>
    public static void SetFootprintLimit(int limit) {
        limit = Math.Clamp(limit, 100, 100_000);
        TankGame.Settings.TankFootprintLimit = limit;

        var old = TankFootprint.AllFootprints;
        if (old.Length == limit)
            return;

        // youngest first (lifeTime counts up every update)
        var live = old.Where(f => f is not null).OrderBy(f => f.lifeTime).ToList();
        for (int i = limit; i < live.Count; i++)
            live[i].Remove();

        var next = new TankFootprint[limit];
        for (int i = 0; i < Math.Min(limit, live.Count); i++) {
            live[i].Id = i;
            next[i] = live[i];
        }
        TankFootprint.AllFootprints = next;
    }
}
