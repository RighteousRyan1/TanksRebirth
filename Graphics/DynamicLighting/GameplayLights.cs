using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using TanksRebirth.GameContent;
using TanksRebirth.GameContent.Systems.ParticleSystem;

namespace TanksRebirth.Graphics.DynamicLighting;

/// <summary>
/// The look of the lights that follow gameplay objects (headlights, shells, mines, explosions). Takes plain
/// positions so it doesn't depend on game types; <see cref="LightManager"/> feeds it the actual tanks and shells.
/// Brightness follows the current preset through <see cref="LightingPresets.DynamicBrightness"/>.
/// </summary>
/// <remarks>Lights are pooled and reused every frame, so adding them doesn't allocate.</remarks>
public static class GameplayLights {
    static readonly List<PointLight> _pointPool = [];
    static readonly List<SpotLight> _spotPool = [];
    static int _pointsUsed, _spotsUsed;

    static float Brightness => LightingPresets.DynamicBrightness;

    /// <summary>Call once at the start of collecting a frame's gameplay lights.</summary>
    public static void BeginFrame() {
        _pointsUsed = 0;
        _spotsUsed = 0;
    }
    /// <summary>Creates a temporary light. If you wish to modify the particle, subscribe to <see cref="Particle.UniqueBehavior"/>.</summary>
    /// <param name="time">The amount of time, in ticks (60 = 1 second).</param>
    /// <returns>The particle created.</returns>
    public static Particle CreateTempLight(ParticleManager system, Vector3 position, Light light, float time) {
        var particle = system.MakeParticle(position);
        LightingSystem.Lights.Add(light);

        particle.UniqueBehavior += (a) => {
            if (particle.LifeTime > time) {
                LightingSystem.Lights.Remove(light);
                particle.Destroy();
            }
        };

        return particle;
    }

    public static void AddHeadlight(Vector3 relativePosition, Vector2 forward, bool isLocalPlayer) {
        forward.Normalize();
        var forward3 = new Vector3(forward.X, 0f, forward.Y);

        var head = NextSpot();
        head.Position = relativePosition;
        head.Direction = forward3 + new Vector3(0f, -0.32f, 0f);
        head.Color = new Color(255, 236, 196);
        head.Intensity = 1.7f * Brightness;
        head.Range = 250f;
        head.InnerAngle = MathHelper.ToRadians(10f);
        head.OuterAngle = MathHelper.ToRadians(40f);
        head.CastsShadows = true;
        head.Priority = isLocalPlayer ? 10 : 8;
        LightingSystem.AddFrameLight(head);
    }

    public static void AddShellLight(Vector3 position, Color color) {
        var light = NextPoint();
        light.Position = position;
        light.Color = color;
        light.Intensity = 0.9f * Brightness;
        light.Range = 70f;
        light.Wrap = 0.4f;
        light.CastsShadows = true;
        light.Priority = 1;
        LightingSystem.AddFrameLight(light);
    }

    public static void AddMineLight(Mine mine) {
        var light = NextPoint();
        light.Position = mine.Position3D + new Vector3(0f, 9f, 0f);
        light.Color = new Color(255, 40, 30);
        // light.Intensity = (0.25f + 1.1f * blink) * Brightness;
        light.Intensity = 1f;
        light.Range = 55f;
        light.Wrap = 0.5f;
        light.CastsShadows = false;
        light.Priority = 1;
        LightingSystem.AddFrameLight(light);
    }

    public static void AddExplosionLight(Vector3 position, float radius, float life) {
        if (life <= 0f) return;

        var light = NextPoint();
        light.Position = position + new Vector3(0f, 14f, 0f);
        light.Color = new Color(255, 168, 82);
        light.Intensity = 1.9f * MathF.Pow(MathHelper.Clamp(life, 0f, 1f), 1.5f) * MathF.Max(Brightness, 0.8f);
        light.Range = 80f + radius * 3f;
        light.Wrap = 0.2f;
        light.CastsShadows = true;
        light.Priority = 9;
        LightingSystem.AddFrameLight(light);
    }

    public static void AddLantern(Vector3 position, float time, bool isLocalPlayer) {
        // i keep having to ask ai how to make a good flickering effect :cry:
        var flicker = 0.9f + 0.06f * MathF.Sin(time * 2.1f) + 0.03f * MathF.Sin(time * 7.7f + 1.3f) + 0.02f * MathF.Sin(time * 17.3f + 4.1f);
        var light = NextPoint();
        light.Position = position;
        light.Color = new Color(255, 178, 76);
        light.Intensity = 1.6f * flicker;
        light.Range = 100f * (0.97f + 0.03f * flicker);
        light.Wrap = 0.3f;
        light.CastsShadows = true;
        light.Priority = isLocalPlayer ? 11 : 9;
        LightingSystem.AddFrameLight(light);
    }

    static PointLight NextPoint() {
        if (_pointsUsed == _pointPool.Count)
            _pointPool.Add(new PointLight());
        var light = _pointPool[_pointsUsed++];
        light.Enabled = true;
        light.ShadowBias = 0.75f;
        return light;
    }

    // idek the point of this or nextpoint
    static SpotLight NextSpot() {
        if (_spotsUsed == _spotPool.Count)
            _spotPool.Add(new SpotLight());
        var light = _spotPool[_spotsUsed++];
        light.Enabled = true;
        light.ShadowBias = 0.75f;
        return light;
    }
}
