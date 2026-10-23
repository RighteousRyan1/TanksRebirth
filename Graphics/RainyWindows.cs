using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.Graphics;

// RainShader?

/// <summary>
/// Draws registered glass meshes as rainy windows, with beads, running drips and condensation that refract the
/// scene behind the glass. Rain fades in while thunder mode is on, unless <see cref="IntensityOverride"/> is set.
/// </summary>
public static class RainyWindows {
    /// <summary>When set, the rain uses this intensity (0 to 1) instead of following thunder mode.</summary>
    public static float? IntensityOverride;
    /// <summary>How long the rain takes to fully fade in or out, in seconds.</summary>
    public static float FadeSeconds = 8f;
    /// <summary>How much slow gusts lower the intensity, from 0 (steady rain) to 1.</summary>
    public static float GustStrength = 0.3f;

    /// <summary>World size of one bead cell. Bigger cells mean bigger drops.</summary>
    public static float CellSize = 40f;
    /// <summary>Share of bead cells holding a bead in full rain.</summary>
    public static float BeadDensity = 0.7f;
    /// <summary>Share of drip columns running in full rain.</summary>
    public static float DripDensity = 0.5f;
    /// <summary>How fast drips run down the glass.</summary>
    public static float DripSpeed = 1f;
    /// <summary>How strongly the water bends the view behind it.</summary>
    public static float Refraction = 1f;
    /// <summary>How fogged the glass gets in full rain, from 0 (clear) to 1.</summary>
    public static float FogAmount = 0.6f;
    public static Color FogColor = new(140, 153, 166);
    public static Color GlassTint = new(230, 235, 242);
    /// <summary>How much of <see cref="GlassTint"/> is mixed over the glass.</summary>
    public static float GlassTintAmount = 0.12f;

    /// <summary>The current rain intensity, from 0 (dry) to 1.</summary>
    public static float Intensity { get; private set; }
    /// <summary>Whether rainy meshes are currently drawn as rainy.</summary>
    public static bool IsActive => Intensity > 0.001f && _effect is not null;

    static readonly HashSet<ModelMesh> _meshes = [];
    static Effect? _effect;
    static bool _loadFailed;
    static RenderTarget2D? _scene, _half, _quarter, _fog;
    static SpriteBatch? _copyBatch;
    static float _time;
    static float _stormIntensity;

    public static void Add(ModelMesh mesh) => _meshes.Add(mesh);
    /// <summary>Makes every mesh of <paramref name="model"/> named <paramref name="meshName"/> rainy.</summary>
    public static void Add(Model model, string meshName) {
        foreach (var mesh in model.Meshes)
            if (mesh.Name == meshName)
                _meshes.Add(mesh);
    }
    public static void Remove(ModelMesh mesh) => _meshes.Remove(mesh);
    public static bool IsRainy(ModelMesh mesh) => _meshes.Contains(mesh);

    /// <summary>Advances the rain and follows thunder mode. Called once per update.</summary>
    public static void Update() {
        var dt = RuntimeData.DeltaTime;
        _time += dt;

        var target = Modifiers.IsOn(Modifiers.THUNDER) ? 1f : 0f;
        _stormIntensity = MathUtils.RoughStep(_stormIntensity, target, dt / MathF.Max(FadeSeconds, 0.001f));

        // two slow waves multiplied so the gusts don't repeat on an obvious beat
        var gust = 0.5f + 0.5f * MathF.Sin(_time * 0.21f) * MathF.Sin(_time * 0.067f + 1.3f);
        Intensity = MathHelper.Clamp(IntensityOverride ?? _stormIntensity * (1f - GustStrength * gust), 0f, 1f);

        if (Intensity > 0.001f)
            LoadEffect();
    }

    static void LoadEffect() {
        if (_effect is not null || _loadFailed)
            return;
        try {
            _effect = GameResources.GetGameResource<Effect>("Assets/shaders/rainy_window");
        }
        catch (Exception e) {
            _loadFailed = true;
            TankGame.ClientLog.Write($"rainy_window.fx could not be loaded, windows stay dry: {e.Message}", RebirthLogLevel.Warn);
        }
    }

    internal static void CaptureScene() {
        var source = TankGame.GameFrameBuffer;
        if (!IsActive || source is null)
            return;

        var device = TankGame.Instance.GraphicsDevice;
        EnsureTargets(device, source.Width, source.Height);

        var blend = device.BlendState;
        var depth = device.DepthStencilState;
        var rasterizer = device.RasterizerState;
        var sampler = device.SamplerStates[0];

        // the fog copy is halved a few times so it blurs smoothly with bilinear filtering
        Copy(device, source, _scene!);
        Copy(device, _scene!, _half!);
        Copy(device, _half!, _quarter!);
        Copy(device, _quarter!, _fog!);
        device.SetRenderTarget(source);

        device.BlendState = blend;
        device.DepthStencilState = depth;
        device.RasterizerState = rasterizer;
        device.SamplerStates[0] = sampler;
    }

    /// <summary>Draws <paramref name="mesh"/> as rainy glass.</summary>
    /// <returns>False without drawing anything when it isn't raining or the mesh isn't rainy.</returns>
    internal static bool Draw(ModelMesh mesh, Matrix world, Matrix view, Matrix projection) {
        if (!IsActive || _scene is null || !_meshes.Contains(mesh))
            return false;

        var p = _effect!.Parameters;
        p["World"]?.SetValue(world);
        p["ViewProjection"]?.SetValue(view * projection);
        p["Time"]?.SetValue(_time);
        p["Intensity"]?.SetValue(Intensity);
        p["CellSize"]?.SetValue(MathF.Max(CellSize, 0.01f));
        p["BeadDensity"]?.SetValue(BeadDensity);
        p["DripDensity"]?.SetValue(DripDensity);
        p["DripSpeed"]?.SetValue(DripSpeed);
        p["Refraction"]?.SetValue(Refraction);
        p["FogAmount"]?.SetValue(FogAmount);
        p["FogColor"]?.SetValue(FogColor.ToVector3());
        p["GlassTint"]?.SetValue(new Vector4(GlassTint.ToVector3(), GlassTintAmount));
        p["SceneTexelSize"]?.SetValue(new Vector2(1f / _scene.Width, 1f / _scene.Height));
        p["SceneTexture"]?.SetValue(_scene);
        p["FogTexture"]?.SetValue(_fog);

        var device = TankGame.Instance.GraphicsDevice;
        foreach (var part in mesh.MeshParts) {
            if (part.PrimitiveCount <= 0)
                continue;
            device.SetVertexBuffer(part.VertexBuffer);
            device.Indices = part.IndexBuffer;
            _effect.CurrentTechnique.Passes[0].Apply();
            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, part.VertexOffset, part.StartIndex, part.PrimitiveCount);
        }
        return true;
    }

    static void Copy(GraphicsDevice device, Texture2D from, RenderTarget2D to) {
        _copyBatch ??= new SpriteBatch(device);
        device.SetRenderTarget(to);
        _copyBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
        _copyBatch.Draw(from, to.Bounds, Color.White);
        _copyBatch.End();
    }

    static void EnsureTargets(GraphicsDevice device, int width, int height) {
        if (_scene is not null && _scene.Width == width && _scene.Height == height)
            return;

        _scene?.Dispose();
        _half?.Dispose();
        _quarter?.Dispose();
        _fog?.Dispose();
        _scene = new RenderTarget2D(device, width, height);
        _half = new RenderTarget2D(device, Math.Max(1, width / 2), Math.Max(1, height / 2));
        _quarter = new RenderTarget2D(device, Math.Max(1, width / 4), Math.Max(1, height / 4));
        _fog = new RenderTarget2D(device, Math.Max(1, width / 8), Math.Max(1, height / 8));
    }
}
