using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.Graphics;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Framework.Collections;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;
using static TanksRebirth.GameContent.RebirthUtils.DebugManager;

namespace TanksRebirth.GameContent.Systems.ParticleSystem;

public class ParticleManager
{
    // maybe rendertarget for lvl edit particles?
    public SwapBackArray<Particle> CurrentParticles;

    public Matrix SystemView => _viewFunc.Invoke();
    public Matrix SystemProjection => _projFunc.Invoke();

    private readonly Func<Matrix> _viewFunc;
    private readonly Func<Matrix> _projFunc;

    public ParticleManager(int maxParticles, Func<Matrix> view, Func<Matrix> proj) {
        CurrentParticles = [];
        _viewFunc = view;
        _projFunc = proj;
        CurrentParticles.OnSwapBack += OnSwapBack;
    }
    void OnSwapBack(int index, Particle particle) {
        //Console.WriteLine($"remove: {particle.Id} ---> {index}");
        particle.Id = index;
    }
    public void Empty() {
        CurrentParticles.Clear();
        for (int i = 0; i < CurrentParticles.Count; i++) {
            CurrentParticles[i]?.Destroy();
        }
    }

    public void RenderParticles(bool renderInReverseOrder = false) {
        if (!GameScene.ShouldRenderAll)
            return;

        //if (Internals.Common.InputUtils.KeyJustPressed(Microsoft.Xna.Framework.Input.Keys.U))
            //Console.Clear();

        //Console.SetCursorPosition(0, 0);
        //var s = Stopwatch.StartNew();

        Particle.EffectHandle.TextureEnabled = true;
        Particle.EffectHandle.View = SystemView;
        Particle.EffectHandle.Projection = SystemProjection;
        Particle.EffectHandle.FogEnabled = false;

        if (renderInReverseOrder) {
            for (int i = CurrentParticles.Count - 1; i >= 0; i--)
                CurrentParticles[i]?.Render();
        }
        else {
            for (int i = 0; i < CurrentParticles.Count; i++)
                CurrentParticles[i]?.Render();
        }
        //double ms = s.ElapsedTicks * 1_000_000.0 / Stopwatch.Frequency / 1000;
        //Console.WriteLine($"Particle Render: {ms:0.000}ms");
    }
    public void RenderModelParticles(bool renderInReverseOrder = false) {
        if (!GameScene.ShouldRenderAll)
            return;

        if (renderInReverseOrder) {
            for (int i = CurrentParticles.Count - 1; i >= 0; i--) {
                var particle = CurrentParticles[i];
                if (particle is not null)
                    if (particle.Model != null)
                        particle.RenderModels();
            }
        }
        else {
            for (int i = 0; i < CurrentParticles.Count; i++) {
                var particle = CurrentParticles[i];
                if (particle is not null)
                    if (particle.Model != null)
                        particle.RenderModels();
            }
        }
    }
    public void UpdateParticles() {
        if (!GameScene.ShouldRenderAll)
            return;

        //var s = Stopwatch.StartNew();

        for (int i = 0; i < CurrentParticles.Count; i++) {
            CurrentParticles[i].Update();
        }
        
        //double ms = s.ElapsedTicks * 1_000_000.0 / Stopwatch.Frequency / 1000;
        //Console.WriteLine($"Particle Logic: {ms:0.000}ms");
    }
    /// <summary>Creates a particle.</summary>
    /// <param name="position">The initial position of this particle.</param>
    /// <param name="texture">The texture used for the particle.</param>
    /// <returns>The particle created.</returns>
    public Particle MakeParticle(Vector3 position, Texture2D texture) {
        return new(position, this) {
            Texture = texture
        };
    }
    public Particle MakeParticle(Vector3 position, Model model, Texture2D texture) {
        return new(position, this) {
            Model = model,
            Texture = texture,
        };
    }
    public Particle MakeParticle(Vector3 position, string text) {
        return new(position, this) {
            IsText = true,
            Text = text
        };
    }
    public Particle MakeExplosionFlameParticle(Vector3 position, out Action<Particle> ourAction, float lingerMultiplier = 1f, float particleScaleMultiplier = 1f) {
        var t = GameResources.GetGameResource<Texture2D>("Assets/textures/mine/explosion");
        var p = MakeParticle(position, t);

        int frame = 0;
        int frameHeight = 66;

        p.Scale = new(particleScaleMultiplier);
        p.IsIn2DSpace = false;
        p.HasAdditiveBlending = false;
        p.Alpha = 1;
        p.Origin2D = new Vector2(t.Width / 2, frameHeight / 2);
        void act(Particle b) {
            b.TextureCrop = new Rectangle(0, frame * frameHeight, t.Width, frameHeight);

            if (frame < 50) {
                if (b.LifeTime % 0.8f <= RuntimeData.DeltaTime)
                    frame++;
            }
            else {
                if (b.LifeTime % (0.8f * lingerMultiplier) <= RuntimeData.DeltaTime)
                    frame++;
            }

            if (frame * frameHeight >= t.Height)
                p.Destroy();
        }
        ourAction = act;
        return p;
    }
    public void MakeSmallExplosion(Vector3 position, int numClouds, int numSparks, float shineScale, int movementFactor) {
        MakeSmokeCloud(position, movementFactor, numClouds);
        MakeSparkEmission(position, numSparks);
        MakeShineSpot(position, Color.Orange, shineScale);
    }
    public void MakeSparkEmission(Vector3 position, int numSparks) {
        for (int i = 0; i < numSparks; i++) {
            var texture = GameResources.GetGameResource<Texture2D>("Assets/textures/misc/particle_line");

            var spark = MakeParticle(position, texture);

            var vel = new Vector3(Client.ClientRandom.NextFloat(-0.25f, 0.25f), Client.ClientRandom.NextFloat(0, 0.75f), Client.ClientRandom.NextFloat(-0.25f, 0.25f)) * 2;

            spark.Roll = -CameraGlobals.DEFAULT_ORTHOGRAPHIC_ANGLE;

            var angles = GeometryUtils.AsEulerAngles(new Quaternion(new Vector3(spark.Roll, spark.Pitch, spark.Yaw), 0f));

            spark.Roll = angles.Roll;
            spark.Pitch = angles.Pitch;
            spark.Yaw = angles.Yaw;
            spark.Alpha = 1f;
            spark.Scale = new(Client.ClientRandom.NextFloat(0.4f, 0.6f));
            spark.FaceTowardsMe = CameraGlobals.IsUsingFirstPresonCamera;

            spark.Color = Color.Yellow;

            spark.UniqueBehavior = (part) => {
                part.Position += vel * RuntimeData.DeltaTime;
                part.Alpha -= 0.025f * RuntimeData.DeltaTime;

                if (part.Alpha <= 0f)
                    part.Destroy();
            };
        }
    }
    public void MakeSmokeCloud(Vector3 position, int timeMovingSideways, int numClouds) {
        for (int i = 0; i < numClouds; i++) {
            var texture = GameResources.GetGameResource<Texture2D>("Assets/textures/misc/tank_smoke");

            var smoke = MakeParticle(position, texture);

            smoke.HasAdditiveBlending = true;

            smoke.Pitch = -CameraGlobals.DEFAULT_ORTHOGRAPHIC_ANGLE;

            smoke.Scale = new(0.8f);

            smoke.FaceTowardsMe = CameraGlobals.IsUsingFirstPresonCamera;

            var velocity = Vector2.UnitY.Rotate(MathHelper.ToRadians(360f / numClouds * i)).ExpandZ() / 2;

            smoke.Position.Y += 5f + Client.ClientRandom.NextFloat(0f, 8f);

            var smokeInitColor = Color.DarkOrange;
            float fullLerpTime = 50f;

            smoke.UniqueBehavior = (p) => {
                smoke.Position += velocity;
                smoke.Scale -= new Vector3(0.01f) * RuntimeData.DeltaTime;
                var time = MathF.Min(smoke.LifeTime, fullLerpTime);
                smoke.Color = Color.Lerp(smokeInitColor, new Color(40, 40, 40), time / fullLerpTime);

                if (smoke.Scale.X <= 0f)
                    smoke.Destroy();

                if (smoke.LifeTime > timeMovingSideways) {
                    smoke.Alpha -= 0.02f * RuntimeData.DeltaTime;
                    smoke.Position.Y += Client.ClientRandom.NextFloat(0.1f, 0.25f) * RuntimeData.DeltaTime;
                    velocity.X *= 0.9f * RuntimeData.DeltaTime;
                    velocity.Z *= 0.9f * RuntimeData.DeltaTime;
                }
            };
        }
    }
    public void MakeShineSpot(Vector3 position, Color color, float scale) {
        var p = MakeParticle(position, GameResources.GetGameResource<Texture2D>("Assets/textures/misc/light_star"));
        p.Scale = new(scale);
        p.Color = color;
        p.FaceTowardsMe = CameraGlobals.IsUsingFirstPresonCamera;
        p.UniqueBehavior = (part) => {
            GeometryUtils.Add(ref p.Scale, -0.0175f * RuntimeData.DeltaTime);

            p.Alpha -= 0.025f * RuntimeData.DeltaTime;

            if (p.Alpha <= 0f || p.Scale.X <= 0f)
                p.Destroy();
        };
    }
}