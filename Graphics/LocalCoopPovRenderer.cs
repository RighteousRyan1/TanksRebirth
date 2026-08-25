using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TanksRebirth.GameContent.Systems.LocalCoop;

namespace TanksRebirth.Graphics;

public static class LocalCoopPovRenderer {
    private static RenderTarget2D? _playerOneTarget;
    private static RenderTarget2D? _playerTwoTarget;

    public static RenderTarget2D? PlayerOneTarget => _playerOneTarget;
    public static RenderTarget2D? PlayerTwoTarget => _playerTwoTarget;

    public static void EnsureTargets(GraphicsDevice device, LocalCoopPovLayout layout) {
        EnsureTarget(device, ref _playerOneTarget, LocalCoopPovPolicy.TargetSize(layout.PlayerOne));
        EnsureTarget(device, ref _playerTwoTarget, LocalCoopPovPolicy.TargetSize(layout.PlayerTwo));
    }

    public static void DisposeTargets() {
        _playerOneTarget?.Dispose();
        _playerTwoTarget?.Dispose();
        _playerOneTarget = null;
        _playerTwoTarget = null;
    }

    private static void EnsureTarget(GraphicsDevice device, ref RenderTarget2D? target, Point desiredSize) {
        var currentSize = target is null ? Point.Zero : new Point(target.Width, target.Height);
        if (target is not null && !LocalCoopPovPolicy.ShouldRecreateTarget(currentSize, target.IsDisposed, desiredSize))
            return;

        target?.Dispose();
        var presentation = device.PresentationParameters;
        target = new RenderTarget2D(
            device,
            desiredSize.X,
            desiredSize.Y,
            false,
            presentation.BackBufferFormat,
            presentation.DepthStencilFormat,
            0,
            RenderTargetUsage.DiscardContents);
    }
}
