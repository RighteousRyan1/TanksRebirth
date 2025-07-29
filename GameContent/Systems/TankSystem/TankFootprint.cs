using HidSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Systems.ParticleSystem;
using TanksRebirth.Graphics;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.Systems.TankSystem;

// TODO: this can definitely become a rendertarget translated into 3d space.

#pragma warning disable
public class TankFootprint {
    public static bool ShouldTracksFade;

    public static TankFootprint[] AllFootprints = new TankFootprint[TankGame.Settings.TankFootprintLimit];

    public Vector3 Scale;
    public Vector3 Position;
    public float Rotation;

    public readonly Tank Owner;
    public Texture2D Texture;

    Particle _track;

    public readonly bool alternate;

    public long lifeTime;

    public int Id = 0;

    //public static DecalSystem DecalHandler; // = new(TankGame.SpriteRenderer, TankGame.Instance.GraphicsDevice);
    public static TankFootprint Place(Tank? owner, float rotation, bool alt = false) {
        if (owner == null) return null;
        var index = Array.IndexOf(AllFootprints, null);
        if (index < 0) {
            var oldest = AllFootprints.Select(x => x._track.LifeTime).Max();

            index = Array.FindIndex(AllFootprints, x => x._track.LifeTime == oldest);

            // remove oldest footprint
            AllFootprints[index].Remove();

            return new(owner, rotation, alt);
        }
        return new(owner, rotation, alt);
    }
    public TankFootprint(Tank owner, float rotation, bool alt = false) {
        Rotation = rotation;
        alternate = alt;
        Owner = owner;

        alternate = alt;
        Position = owner.Position3D;

        Id = Array.IndexOf(AllFootprints, null);

        Texture = GameResources.GetGameResource<Texture2D>(alt
            ? $"Assets/textures/tank_footprint_alt"
            : $"Assets/textures/tank_footprint");

        _track = GameHandler.Particles.MakeParticle(Position, Texture);

        _track.HasAdditiveBlending = false;

        _track.Pitch = MathHelper.PiOver2;

        Scale = owner.Scaling;

        var defScale = new Vector3(0.5f, 0.55f, 0.5f);
        _track.Scale = defScale;
        _track.Alpha = 0.35f;
        _track.Color = Color.White;
        _track.UniqueBehavior = track => {
            track.Position = Position;
            track.Yaw = rotation;
            track.Scale = Scale * defScale;

            if (ShouldTracksFade)
                track.Alpha -= 0.001f * RuntimeData.DeltaTime;

            if (track.Alpha <= 0) {
                Remove();
            }
        };

        AllFootprints[Id] = this;

        /*DecalHandler.AddDecal(texture, MatrixUtils.ConvertWorldToScreen(Vector3.Zero,
            Matrix.CreateTranslation(Position), TankGame.GameView,
            TankGame.GameProjection), null, Color.White, rotation, BlendState.Opaque);*/
    }

    public void Update() => lifeTime++;
    public void Remove() {
        _track?.Destroy();

    }
}
/*public class TankFootprint {
    public static bool ShouldTracksFade;

    public static TankFootprint[] AllFootprints = new TankFootprint[TankGame.Settings.TankFootprintLimit];

    public Vector2 Scale;
    public Vector2 Position;
    public float Rotation;

    public readonly Tank Owner;
    public Texture2D Texture;

    // Particle _track;

    public readonly bool alternate;

    public long lifeTime;

    public int Id = 0;

    public static RenderTarget2D FrameBuffer;

    static int indexOfOldest = -1;

    public static float Alpha = 0.7f;

    //public static DecalSystem DecalHandler; // = new(TankGame.SpriteRenderer, TankGame.Instance.GraphicsDevice);
    public static TankFootprint Create(Tank? owner, float rotation, Texture2D texture) {
        return new(owner, rotation, false);
    }

    public TankFootprint(Tank owner, float rotation, bool alt = false) {
        Rotation = rotation;
        alternate = alt;
        Owner = owner;

        alternate = alt;
        Id = Array.FindIndex(AllFootprints, x => x == null);
        Position = owner.Position;

        Texture = GameResources.GetGameResource<Texture2D>(alt
            ? $"Assets/textures/tank_footprint_alt"
            : $"Assets/textures/tank_footprint");

        AllFootprints[Id] = this;

        // Render();
    }

    public static void UpdateAll() {
        for (int i = 0; i < AllFootprints.Length; i++) {
            var footprint = AllFootprints[i];
            if (footprint == null) continue;
            footprint.lifeTime++;

        }
    }
    public static BasicEffect EffectHandle = new(TankGame.Instance.GraphicsDevice);
    public static void PrepareRT(GraphicsDevice device) {
        // could probably be better
        RenderGlobals.EnsureRenderTargetOK(ref FrameBuffer, device, WindowUtils.WindowWidth, WindowUtils.WindowHeight);

        device.SetRenderTarget(FrameBuffer);

        device.Clear(RenderGlobals.BackBufferColor);

        EffectHandle.View = CameraGlobals.GameView;
        EffectHandle.Projection = CameraGlobals.GameProjection;
        EffectHandle.Alpha = 1f;
        EffectHandle.TextureEnabled = true;
        EffectHandle.FogEnabled = false;
        EffectHandle.SetDefaultGameLighting_IngameEntities();

        for (int i = 0; i < AllFootprints.Length; i++) {
            var fp = AllFootprints[i];
            if (fp == null) continue;

            TankGame.SpriteRenderer.Draw(fp.Texture, fp.Position, null, Color.White, 0f, fp.Texture.Size() / 2, fp.Scale, default, 0f);
        }
        device.SetRenderTarget(TankGame.GameFrameBuffer);
    }
    public static void Draw() {
        EffectHandle.View = CameraGlobals.GameView;
        EffectHandle.Projection = CameraGlobals.GameProjection;
        EffectHandle.Alpha = 1f;
        EffectHandle.TextureEnabled = true;
        EffectHandle.FogEnabled = false;
        EffectHandle.SetDefaultGameLighting_IngameEntities();

        EffectHandle.World = Matrix.CreateScale(1) 
            * Matrix.CreateFromYawPitchRoll(0f, MathHelper.PiOver2, 0) 
            * Matrix.CreateTranslation(new Vector3(0, 0.15f, 0));

        TankGame.SpriteRenderer.End();
        TankGame.SpriteRenderer.Begin(effect: EffectHandle);

        var texture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank_shadow"); // FrameBuffer;
        TankGame.SpriteRenderer.Draw(texture, 
            new Vector2(GameScene.MIN_X, GameScene.MIN_Z), null, Color.White * Alpha, 0f, texture.Size() / 2, 1f, default, 0f);

        TankGame.SpriteRenderer.End();
        TankGame.SpriteRenderer.Begin();
    }
    public void Remove() {


        // _track?.Destroy();
    }
}*/

/*
 * public class TankFootprint {
    public static bool ShouldTracksFade;

    public static TankFootprint[] AllFootprints = new TankFootprint[TankGame.Settings.TankFootprintLimit];

    public Vector2 Scale;
    public Vector3 Position;
    public float Rotation;

    public readonly Tank Owner;
    public Texture2D Texture;

    // Particle _track;

    public readonly bool alternate;

    public long lifeTime;

    public int Id = 0;

    public static RenderTarget2D FrameBuffer;

    static int indexOfOldest = -1;

    public static float Alpha = 0.7f;

    //public static DecalSystem DecalHandler; // = new(TankGame.SpriteRenderer, TankGame.Instance.GraphicsDevice);
    public static TankFootprint Create(Tank? owner, float rotation, Texture2D texture) {
        return new(owner, rotation, false);
    }

    public TankFootprint(Tank owner, float rotation, bool alt = false) {
        Rotation = rotation;
        alternate = alt;
        Owner = owner;

        alternate = alt;
        Id = Array.FindIndex(AllFootprints, x => x == null);
        Position = owner.Position3D;

        Texture = GameResources.GetGameResource<Texture2D>(alt
            ? $"Assets/textures/tank_footprint_alt"
            : $"Assets/textures/tank_footprint");

        _track = GameHandler.Particles.MakeParticle(Position, Texture);

        _track.HasAdditiveBlending = false;

        _track.Pitch = MathHelper.PiOver2;

        var defScale = new Vector3(0.5f, 0.55f, 0.5f);
        _track.Scale = defScale;
        _track.Alpha = 0.7f;
        _track.Color = Color.White;
        _track.UniqueBehavior = track => {
            track.Position = Position;
            track.Yaw = rotation;
            track.Scale = Scale.ExpandZ() * defScale;

            if (ShouldTracksFade)
                track.Alpha -= 0.001f * RuntimeData.DeltaTime;

            if (track.Alpha <= 0) {
                Remove();
            }
        };

AllFootprints[Id] = this;

// Render();
}

public static void UpdateAll() {
    for (int i = 0; i < AllFootprints.Length; i++) {
        var footprint = AllFootprints[i];
        if (footprint == null) continue;
        footprint.lifeTime++;

    }
}
public static BasicEffect EffectHandle = new(TankGame.Instance.GraphicsDevice);
public static void PrepareRT(GraphicsDevice device) {
    // could probably be better
    RenderGlobals.EnsureRenderTargetOK(ref FrameBuffer, device, WindowUtils.WindowWidth, WindowUtils.WindowHeight);

    device.SetRenderTarget(FrameBuffer);

    device.Clear(RenderGlobals.BackBufferColor);

    EffectHandle.View = CameraGlobals.GameView;
    EffectHandle.Projection = CameraGlobals.GameProjection;
    EffectHandle.Alpha = 1f;
    EffectHandle.TextureEnabled = true;
    EffectHandle.FogEnabled = false;
    EffectHandle.SetDefaultGameLighting_IngameEntities();

    for (int i = 0; i < AllFootprints.Length; i++) {
        var fp = AllFootprints[i];
        if (fp == null) continue;

        EffectHandle.World = Matrix.CreateFromYawPitchRoll(0f, MathHelper.PiOver2, fp.Rotation) *
            Matrix.CreateTranslation(fp.Position);

        EffectHandle.Texture = fp.Texture;

        TankGame.SpriteRenderer.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied,
            rasterizerState: RenderGlobals.DefaultRasterizer, effect: EffectHandle);

        TankGame.SpriteRenderer.Draw(fp.Texture, Vector2.Zero, null, Color.White, 0f, fp.Texture.Size() / 2, fp.Scale * 0.25f, default, 0f);
        TankGame.SpriteRenderer.End();
    }
    device.SetRenderTarget(TankGame.GameFrameBuffer);
}
public static void Draw() {
    EffectHandle.View = CameraGlobals.GameView;
    EffectHandle.Projection = CameraGlobals.GameProjection;
    EffectHandle.Alpha = 1f;
    EffectHandle.TextureEnabled = true;
    EffectHandle.FogEnabled = false;
    EffectHandle.SetDefaultGameLighting_IngameEntities();


    TankGame.SpriteRenderer.Draw(FrameBuffer, Vector2.Zero, Color.White * Alpha * 0.5f);
}
public void Remove() {


    // _track?.Destroy();
}
}*/