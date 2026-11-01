using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Globals.Assets;
using TanksRebirth.GameContent.Systems.ParticleSystem;
using TanksRebirth.Graphics;
using TanksRebirth.Graphics.DynamicLighting;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

// this probably won't be used anymore cuz bk made pumpkins part of the actual scene.
namespace TanksRebirth.GameContent.Seasonal; 
public class JackOLantern {
    static Model? _jol;
    static Texture2D? _tex;
    readonly Particle? _p;
    public Color BaseColor = Color.Goldenrod;
    public Vector3 Position;
    public float Rotation, Scale;
    public PointLight Light;

    JackOLanternSettings _settings;

    JackOLantern(Vector3 position, float rotation, float scale, bool lightOnly = false) {
        _settings = new();
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

        // uses a frame light
        LightingSystem.CollectLights += AddLight;

        if (lightOnly) return;

        _jol ??= ModelGlobals.JackOLantern.Asset;
        _tex ??= GameResources.GetGameResource<Texture2D>(PathGlobals.DECO_PATH + "pumpkin_tex.png", false);
        _p = GameHandler.Particles.MakeParticle(Position, _jol, _tex);
        _p.Alpha = 1f;
        _p.Scale = new Vector3(scale);

        // handles our update loop
        _p.UpdateCallback = (p) => {
            Update();
        };
    }

    void AddLight() {
        Light.Range = 110f * (Scale * 0.075f);
        Light.Position = Position + Vector3.UnitY * 0.6f * Scale;
        LightFunctions.JackOLanternFlicker(Light, TankGame.LastGameTime, ref _settings);
        LightingSystem.AddFrameLight(Light);
    }

    public static JackOLantern Create(Vector3 position, float rotation, float scale) {
        return new(position, rotation, scale);
    }

    public static JackOLantern CreateLight(Vector3 position, float scale, int priority = 0) {
        var lantern = new JackOLantern(position, 0f, scale, true);
        lantern.Light.Priority = priority;
        lantern.Light.CastsShadows = false;
        return lantern;
    }

    public void Update() {
        if (_p is null) return;

        _p.Yaw = Rotation;
        _p.Scale = new Vector3(Scale);
    }

    public void Remove() {
        LightingSystem.CollectLights -= AddLight;
        _p?.Destroy();
    }
}
internal static class SceneJackOLanterns {
    // the model space is in m while game reads in cm... i think this is just fbx being the stupidest format OAT.
    // todo: switch to gltf or some other good format (.obj?)
    const float MODEL_TO_LOCAL = 100f;
    // the pumpkin model JackOLantern uses is this tall at a scale of 1
    const float PUMPKIN_HEIGHT = 1.8f;

    // i might convert these to world coordinates straight up
    static readonly (Vector3 Center, float Height)[] _pumpkins = [
        (new(-0.982f, 0.18f, -3.348f), 0.373f),
        (new(-4.199f, 0.33f, 3.368f), 0.684f),
        (new(4.016f, 0.33f, -3.381f), 0.684f),
        (new(-1.311f, 0.128f, -3.528f), 0.263f),
        (new(-0.668f, 0.09f, -3.373f), 0.183f)
    ];

    // i tried turning this into Particles with PointLight data so it wouldnt have
    // to allocate a bunch of Jackolanterns. did not go well
    static readonly List<JackOLantern> _lanterns = [];

    public static void Spawn() {
        Clear();
        foreach (var (center, height) in _pumpkins) {
            var position = center * MODEL_TO_LOCAL * GameScene.Scale + GameScene.Center;
            var scale = height * MODEL_TO_LOCAL * GameScene.Scale / PUMPKIN_HEIGHT;
            _lanterns.Add(JackOLantern.CreateLight(position, scale));
        }
    }

    public static void Clear() {
        foreach (var lantern in _lanterns)
            lantern.Remove();
        _lanterns.Clear();
    }
}
