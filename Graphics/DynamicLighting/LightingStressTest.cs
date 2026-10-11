using Microsoft.Xna.Framework;
using System;

namespace TanksRebirth.Graphics.DynamicLighting;

/// <summary>Puts the most shadowed point and spot lights allowed on the board and moves them around, to test lighting performance.</summary>
/// <remarks>The quality setting still caps how many get shadows, so use Ultra to test all of them.</remarks>
public static class LightingStressTest {
    // roughly the board's half size in world units
    const float HALF_WIDTH = 200f;
    const float HALF_DEPTH = 130f;

    static readonly PointLight[] _points = new PointLight[LightingSystem.MAX_SHADOW_SLOTS];
    static readonly SpotLight[] _spots = new SpotLight[LightingSystem.MAX_SHADOW_SLOTS];
    static float _time;

    public static bool IsRunning { get; private set; }

    public static void Start() {
        if (IsRunning)
            return;
        IsRunning = true;
        _time = 0f;
        for (int i = 0; i < _points.Length; i++) {
            _points[i] ??= new PointLight {
                CastsShadows = true,
                Intensity = 1.2f,
                Range = 140f,
                Priority = 20,
            };
            _points[i].Color = Hue(i / (float)_points.Length);
        }
        for (int i = 0; i < _spots.Length; i++) {
            _spots[i] ??= new SpotLight {
                CastsShadows = true,
                Intensity = 1.4f,
                Range = 350f,
                InnerAngle = MathHelper.ToRadians(12f),
                OuterAngle = MathHelper.ToRadians(24f),
                Priority = 20,
            };
            _spots[i].Color = Hue((i + 0.5f) / _spots.Length);
        }
        LightingSystem.CollectLights += AddLights;
    }

    public static void Stop() {
        if (!IsRunning)
            return;
        IsRunning = false;
        LightingSystem.CollectLights -= AddLights;
        // puts back the preset's own shadow requests
        LightManager.Apply(LightingPresets.Current);
    }

    public static void Toggle() {
        if (IsRunning) Stop();
        else Start();
    }

    static void AddLights() {
        // presets lower these, so ask for every slot each frame
        LightingSystem.MaxShadowedPointLights = LightingSystem.MAX_SHADOW_SLOTS;
        LightingSystem.MaxShadowedSpotLights = LightingSystem.MAX_SHADOW_SLOTS;
        _time += TankGame.LastGameTime is null ? 0f : (float)TankGame.LastGameTime.ElapsedGameTime.TotalSeconds;

        // point lights circle the board at different speeds and distances, bobbing up and down
        for (int i = 0; i < _points.Length; i++) {
            var count = (float)_points.Length;
            var direction = i % 2 == 0 ? 1f : -1f;
            var angle = i / count * MathHelper.TwoPi + _time * (0.5f + i % 3 * 0.25f) * direction;
            var radius = 0.3f + 0.65f * (i * 7 % _points.Length) / count;
            var light = _points[i];
            light.Position = new Vector3(MathF.Cos(angle) * HALF_WIDTH * radius, 28f + 10f * MathF.Sin(_time * 2f + i), MathF.Sin(angle) * HALF_DEPTH * radius);
            LightingSystem.AddFrameLight(light);
        }

        // spot lights hang above the board edges and sweep their beams across it
        for (int i = 0; i < _spots.Length; i++) {
            var count = (float)_spots.Length;
            var angle = i / count * MathHelper.TwoPi + _time * 0.2f;
            var sweep = -angle * 1.5f + _time * (0.7f + i % 4 * 0.15f);
            var light = _spots[i];
            light.Position = new Vector3(MathF.Cos(angle) * HALF_WIDTH * 1.1f, 110f, MathF.Sin(angle) * HALF_DEPTH * 1.1f);
            light.LookAt(new Vector3(MathF.Cos(sweep) * HALF_WIDTH * 0.6f, 0f, MathF.Sin(sweep) * HALF_DEPTH * 0.6f));
            LightingSystem.AddFrameLight(light);
        }
    }

    static Color Hue(float hue) {
        var r = MathHelper.Clamp(MathF.Abs(hue * 6f - 3f) - 1f, 0f, 1f);
        var g = MathHelper.Clamp(2f - MathF.Abs(hue * 6f - 2f), 0f, 1f);
        var b = MathHelper.Clamp(2f - MathF.Abs(hue * 6f - 4f), 0f, 1f);
        return new Color(r, g, b);
    }
}
