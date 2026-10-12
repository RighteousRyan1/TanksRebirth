using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using TanksRebirth.GameContent;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.Internals.Common;

namespace TanksRebirth.Graphics.Shaders;

// blurs the screen
public static class GaussianBlur {
    // the frame is shrunk to at most an eighth of its size
    const int MAX_LEVEL = 3;
    // the shrink level is picked so the blur is about this many pixels wide at that size
    const float CURVE = 2.5f;

    static readonly RenderTarget2D?[] _levels = new RenderTarget2D?[MAX_LEVEL + 1];
    static RenderTarget2D? _temp;

    /// <summary>The blurred frame, if blurred at all.</summary>
    public static RenderTarget2D? Result { get; private set; }

    /// <summary>Multiplies the blurred colors.</summary>
    public static float Brightness = 1f;

    /// <summary>Blurs <paramref name="frame"/> into <see cref="Result"/>.</summary>
    public static void Prepare(GraphicsDevice device, SpriteBatch spriteBatch, RenderTarget2D? frame, float radius) {
        Result = null;
        var effect = GameShaders.GaussianBlurShader;
        if (frame is null || effect is null || radius <= 0.0005f) return;

        var blend = device.BlendState;
        var depth = device.DepthStencilState;
        var raster = device.RasterizerState;
        var sampler = device.SamplerStates[0];

        // the old blur sampled a disc out to radius. A gaussian with half that as its sigma looks about the same
        var sigma = radius * 0.5f;
        var sigmaPixels = sigma * frame.Width;
        var level = MathHelper.Clamp((int)MathF.Ceiling(MathF.Log2(sigmaPixels / CURVE)), 1, MAX_LEVEL);

        // halve the frame one step at a time, 2x2 px/step
        Texture2D source = frame;
        RenderTarget2D small = null!;
        for (int i = 1; i <= level; i++) {
            small = RenderGlobals.EnsureOK(ref _levels[i], device, frame.Width >> i, frame.Height >> i);
            Draw(device, spriteBatch, source, small, null);
            source = small;
        }
        var scratch = RenderGlobals.EnsureOK(ref _temp, device, small.Width, small.Height);

        var parameters = effect.Parameters;
        parameters["oStep"].SetValue(new Vector2(sigma / 2f, 0f));
        parameters["oBrightness"].SetValue(1f);
        Draw(device, spriteBatch, small, scratch, effect);

        parameters["oStep"].SetValue(new Vector2(0f, sigma / 2f));
        parameters["oBrightness"].SetValue(Brightness);
        Draw(device, spriteBatch, scratch, small, effect);

        device.BlendState = blend;
        device.DepthStencilState = depth;
        device.RasterizerState = raster;
        device.SamplerStates[0] = sampler;
        device.Textures[0] = null;

        device.SetRenderTarget(null);

        // just in case i have any more issues with rendertargets (i will)
        /*if (InputUtils.KeyJustPressed(Microsoft.Xna.Framework.Input.Keys.F9)) {
            static void Dump(Texture2D t, string name) {
                using var fs = System.IO.File.Create(name + ".png");
                t.SaveAsPng(fs, t.Width, t.Height);
            }
            Dump(frame, "blur_0_frame");
            for (int i = 1; i <= level; i++)
                Dump(_levels[i]!, $"blur_{i}_level");
            Dump(scratch, "blur_h");
            Dump(small, "blur_v");
        }*/

        Result = small;
    }

    /// <summary>The blur shader set up to apply only <see cref="Brightness"/>.</summary>
    public static Effect? Unblurred() {
        var effect = GameShaders.GaussianBlurShader;
        if (effect is null) return null;
        effect.Parameters["oStep"].SetValue(Vector2.Zero);
        effect.Parameters["oBrightness"].SetValue(Brightness);
        return effect;
    }

    static void Draw(GraphicsDevice device, SpriteBatch spriteBatch, Texture2D source, RenderTarget2D target, Effect? effect) {
        device.SetRenderTarget(target);
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, effect);
        spriteBatch.Draw(source, new Rectangle(0, 0, target.Width, target.Height), Color.White);
        spriteBatch.End();
    }
}
