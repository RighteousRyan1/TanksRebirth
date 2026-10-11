using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Globals.Assets;
using TanksRebirth.GameContent.Systems.ParticleSystem;
using TanksRebirth.Graphics.DynamicLighting;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.Seasonal; 
public class JackOLantern {
    static Model? _jol;
    static Texture2D? _tex;
    readonly Particle _p;

    public Color BaseColor = Color.Goldenrod;
    public Vector3 Position;
    public float Rotation, Scale;
    public PointLight Light;

    readonly float _seed = Client.ClientRandom.NextFloat(1, 1000);
    float _flicker = 1f, _flickerTarget = 1f, _retarget, _dip;

    JackOLantern(Vector3 position, float rotation, float scale) {
        _jol ??= ModelGlobals.JackOLantern.Asset;
        _tex ??= GameResources.GetGameResource<Texture2D>(PathGlobals.DECO_PATH + "pumpkin.png", false);
        Position = position;
        Rotation = rotation;
        Scale = scale;
        Light = new PointLight() {
            CastsShadows = true,
            Color = BaseColor,
            Intensity = 2f,
            Position = position,
            Wrap = 0.8f,
            Priority = 1,
            Range = 400f,
        };
        _p = GameHandler.Particles.MakeParticle(Position, _jol, _tex);
        _p.Alpha = 1f;
        _p.Scale = new Vector3(scale);

        // uses a frame light
        LightingSystem.CollectLights += AddLight;

        // handles our update loop
        _p.UniqueBehavior = (p) => {
            Update();
        };
    }

    void AddLight() {
        var dt = (float)TankGame.LastGameTime.ElapsedGameTime.TotalSeconds; //RuntimeData.DeltaTime * 0.1f;
        // slow uneven sway, about -1 to 1
        var t = RuntimeData.RunTime + _seed;
        var sway = MathF.Sin(t * 1.7f) * 0.5f + MathF.Sin(t * 2.9f + 1.3f) * 0.3f + MathF.Sin(t * 5.3f + 4.1f) * 0.2f;

        // fast jitter, picks a new random target 10 to 20 times a second and eases toward it
        _retarget -= dt;
        if (_retarget <= 0f) {
            _flickerTarget = 0.82f + Client.ClientRandom.NextFloat() * 0.3f;
            _retarget = 0.05f + Client.ClientRandom.NextFloat() * 0.05f;
        }
        _flicker = MathHelper.Lerp(_flicker, _flickerTarget, 1f - MathF.Exp(-dt * 25f));

        // rare gutter, a quick dip down and back up
        if (_dip <= 0f && Client.ClientRandom.NextFloat() < dt * 0.15f)
            _dip = 1f;
        _dip = MathF.Max(0f, _dip - dt * 3f);
        var gutter = 1f - 0.45f * MathF.Sin(_dip * MathHelper.Pi);

        var strength = (1f + sway * 0.12f) * _flicker * gutter;

        // a wee bit hardcoded for yellow huh
        Light.Intensity = 2f * strength;
        // also deifnitely unhardcode this
        Light.Range = 110f * (Scale * 0.075f);
        Light.Color = Color.Lerp(new Color(255, 110, 25), BaseColor, MathHelper.Clamp((strength - 0.55f) / 0.5f, 0f, 1f));
        Light.Position = Position + Vector3.UnitY * 0.6f * Scale;
        LightingSystem.AddFrameLight(Light);
    }

    public static JackOLantern Create(Vector3 position, float rotation, float scale) {
        return new(position, rotation, scale);
    }

    /// <summary>Handles the lighting and other small things.</summary>
    public void Update() {
        _p.Yaw = Rotation;
        _p.Scale = new Vector3(Scale);
    }

    /// <summary>Removes the particle and the light.</summary>
    public void Remove() {
        LightingSystem.CollectLights -= AddLight;
        _p?.Destroy();
    }
}
