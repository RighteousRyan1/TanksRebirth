using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using TanksRebirth.GameContent;

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

    /// <summary>Call once at the start of collecting a frame's gameplay lights (recycles the pooled lights).</summary>
    public static void BeginFrame() {
        _pointsUsed = 0;
        _spotsUsed = 0;
    }

    /// <summary>A tank headlight. <paramref name="forward"/> is the turret direction on the XZ plane.</summary>
    public static void AddHeadlight(Vector3 relativePosition, Vector2 forward, bool isLocalPlayer) {
        if (forward.LengthSquared() < 1e-6f)
            forward = new Vector2(0f, 1f);
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

    /// <summary>Small light travelling with a shell.</summary>
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

    /// <summary>Red blinking mine light; <paramref name="fuse"/> goes from 1 (just placed) to 0 (detonating).</summary>
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

    /// <summary>Shadow casting explosion flash. <paramref name="life"/> goes from 1 (new) to 0 (gone).</summary>
    public static void AddExplosionLight(Vector3 position, float radius, float life) {
        if (life <= 0f)
            return;
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

    static PointLight NextPoint() {
        if (_pointsUsed == _pointPool.Count)
            _pointPool.Add(new PointLight());
        var light = _pointPool[_pointsUsed++];
        light.Enabled = true;
        light.ShadowBias = 0.75f;
        return light;
    }

    static SpotLight NextSpot() {
        if (_spotsUsed == _spotPool.Count)
            _spotPool.Add(new SpotLight());
        var light = _spotPool[_spotsUsed++];
        light.Enabled = true;
        light.ShadowBias = 0.75f;
        return light;
    }
}
