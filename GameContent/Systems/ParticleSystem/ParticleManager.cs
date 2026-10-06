using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Tanks;
using TanksRebirth.Graphics;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.Systems.ParticleSystem;

public class ParticleManager(Func<Matrix> view, Func<Matrix> proj) {
    // Particles live in one flat array. Removal is an O(1) swap-with-last (each particle stores its own index).
    Particle[] _items = new Particle[256];
    int _count;

    /// <summary>The number of live particles.</summary>
    public int Count => _count;

    public Matrix SystemView => _viewFunc.Invoke();
    public Matrix SystemProjection => _projFunc.Invoke();

    readonly Func<Matrix> _viewFunc = view;
    readonly Func<Matrix> _projFunc = proj;

    /// <summary>Whether this system also draws the batched tank tracks (<see cref="TankFootprint"/>). Only the in-game system should.</summary>
    public bool DrawsFootprints { get; init; }

    // render scratch (reused every frame; no per-frame allocation)
    readonly SpriteParticleBatcher _sprites = new();
    readonly List<Particle> _text3D = [];
    readonly List<Particle> _screenSpace = [];
    readonly List<Particle> _customDraw = [];

    internal int Register(Particle particle) {
        if (_count == _items.Length)
            Array.Resize(ref _items, _items.Length * 2);
        _items[_count] = particle;
        return _count++;
    }

    internal void Unregister(Particle particle) {
        int index = particle.Id;
        if (index < 0 || index >= _count || !ReferenceEquals(_items[index], particle))
            return;

        int last = --_count;
        if (index != last) {
            var moved = _items[last];
            _items[index] = moved;
            moved.Id = index;
        }
        _items[last] = null;
    }

    public void Empty() {
        for (int i = 0; i < _count; i++) {
            var p = _items[i];
            if (p is null) continue;
            p.UniqueBehavior = null;
            p.UniqueDraw = null;
            p.Destroyed = true;
            _items[i] = null;
        }
        _count = 0;
    }

    public void UpdateParticles() {
        int i = 0;
        while (i < _count) {
            var p = _items[i];
            p.Update();
            // if it destroyed itself, the last particle was swapped into slot i and still needs its update this frame
            if (i < _count && ReferenceEquals(_items[i], p))
                i++;
        }
    }

    /// <summary>Draws all sprite particles. Total cost is one draw call per distinct (texture, blend mode) pair
    /// instead of several state changes and a draw call per particle.</summary>
    public void RenderParticles(SpriteBatch spriteBatch) {
        var device = TankGame.Instance.GraphicsDevice;
        bool spriteBatchEnded = false;

        void EndSpriteBatch() {
            if (spriteBatchEnded) return;
            spriteBatch.End();
            spriteBatchEnded = true;
        }

        var view = SystemView;
        var projection = SystemProjection;

        // tank tracks go first: they're decals on the floor, everything else draws on top
        if (DrawsFootprints && TankFootprint.Count > 0) {
            EndSpriteBatch();
            TankFootprint.Render(device, view, projection);
        }

        // ---- sort this frame's particles into draw lists ----
        _sprites.Begin();
        _text3D.Clear();
        _screenSpace.Clear();
        _customDraw.Clear();

        for (int i = 0; i < _count; i++) {
            var p = _items[i];
            if (p.Model is not null) continue; // model particles are drawn by RenderModelParticles

            if (p.UniqueDraw is not null)
                _customDraw.Add(p);

            if (p.IsIn2DSpace) {
                _screenSpace.Add(p);
            }
            else if (p.IsText) {
                _text3D.Add(p);
            }
            else if (p.Texture is not null && p.Alpha > 0f) {
                _sprites.Add(p);
            }
        }

        // world space particles
        if (_sprites.HasSprites) {
            EndSpriteBatch();
            _sprites.Draw(device, view, projection);
        }

        // world space text
        if (_text3D.Count > 0) {
            EndSpriteBatch();
            Particle.EffectHandle.TextureEnabled = true;
            Particle.EffectHandle.View = view;
            Particle.EffectHandle.Projection = projection;
            Particle.EffectHandle.FogEnabled = false;
            for (int i = 0; i < _text3D.Count; i++)
                _text3D[i].DrawText3D(spriteBatch);
        }

        // screen space particles. should probably differentiate via two structs (Particle2D / Particle3D)
        if (_screenSpace.Count > 0) {
            EndSpriteBatch();
            DrawScreenSpace(spriteBatch, additive: false);
            DrawScreenSpace(spriteBatch, additive: true);
        }

        if (spriteBatchEnded)
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        // custom draw hooks run with the same SpriteBatch state they always did
        for (int i = 0; i < _customDraw.Count; i++)
            _customDraw[i].UniqueDraw?.Invoke(_customDraw[i]);
    }

    void DrawScreenSpace(SpriteBatch spriteBatch, bool additive) {
        bool begun = false;
        for (int i = 0; i < _screenSpace.Count; i++) {
            var p = _screenSpace[i];
            if (p.HasAdditiveBlending != additive) continue;

            if (!begun) {
                spriteBatch.Begin(SpriteSortMode.FrontToBack, additive ? BlendState.Additive : BlendState.NonPremultiplied, rasterizerState: RenderGlobals.DefaultRasterizer);
                begun = true;
            }
            p.Draw2D(spriteBatch);
        }
        if (begun)
            spriteBatch.End();
    }

    public void RenderModelParticles() {
        for (int i = 0; i < _count; i++) {
            var particle = _items[i];
            if (particle.Model is null) continue;

            particle.DrawModel();
        }
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
            spark.FaceTowardsMe = CameraGlobals.IsUsingFirstPersonCamera;

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

            smoke.FaceTowardsMe = CameraGlobals.IsUsingFirstPersonCamera;

            var velocity = Vector2.UnitY.RotatedBy(MathHelper.ToRadians(360f / numClouds * i)).ExpandZ() / 2;

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
        p.FaceTowardsMe = CameraGlobals.IsUsingFirstPersonCamera;
        p.UniqueBehavior = (part) => {
            GeometryUtils.Add(ref p.Scale, -0.0175f * RuntimeData.DeltaTime);

            p.Alpha -= 0.025f * RuntimeData.DeltaTime;

            if (p.Alpha <= 0f || p.Scale.X <= 0f)
                p.Destroy();
        };
    }
}
