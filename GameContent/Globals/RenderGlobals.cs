using HidSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.Globals;

#pragma warning disable CA2211
public static class RenderGlobals {

    public static Color BackBufferColor = Color.Transparent;
    public static RasterizerState DefaultRasterizer => DebugManager.RenderWireframe ? new() { FillMode = FillMode.WireFrame } : RasterizerState.CullNone;

    public static readonly DepthStencilState DefaultStencilState = DepthStencilState.Default;

    public static SamplerState WrappingSampler { get; } = new() {
        AddressU = TextureAddressMode.Wrap,
        AddressV = TextureAddressMode.Wrap,
    };
    public static SamplerState ClampingSampler { get; } = new() {
        AddressU = TextureAddressMode.Clamp,
        AddressV = TextureAddressMode.Clamp,
    };

    public static RenderTarget2D EnsureOK(ref RenderTarget2D? rt, GraphicsDevice device, int desiredWidth, int desiredHeight, string? name = null) {
        if (rt == null || rt.IsDisposed || rt.Width != desiredWidth || rt.Height != desiredHeight) {
            rt?.Dispose();
            var presentationParams = TankGame.Instance.GraphicsDevice.PresentationParameters;
            rt = new RenderTarget2D(device,
                desiredWidth, desiredHeight, false,
                presentationParams.BackBufferFormat, presentationParams.DepthStencilFormat, 0, RenderTargetUsage.PreserveContents) {
                Name = name
            };
        }

        return rt;
    }
}
