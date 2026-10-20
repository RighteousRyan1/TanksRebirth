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
        if (DrawsFootprints)
            ShellTrail.Clear();
    }

    public void UpdateParticles() {
        if (DrawsFootprints)
            ShellTrail.UpdateAll(RuntimeData.DeltaTime);

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

        // tracks before everything else
        if (DrawsFootprints && TankFootprint.Count > 0) {
            EndSpriteBatch();
            TankFootprint.Render(device, view, projection);
        }

        // rocket smoke: before the sprites, so the flames draw over it
        if (DrawsFootprints && ShellTrail.All.Count > 0) {
            EndSpriteBatch();
            ShellTrail.RenderAll(device, view, projection, TanksRebirth.GameContent.RebirthUtils.SceneManager.GameLight.Brightness / 0.75f);
        }

        // put each particles into their own draw lists for optimizations
        _sprites.Begin();
        _text3D.Clear();
        _screenSpace.Clear();
        _customDraw.Clear();

        for (int i = 0; i < _count; i++) {
            var p = _items[i];
            if (p.Model is not null) continue; // RenderModelParticles

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
            if (particle.Model is null || particle.Alpha <= 0f) continue;

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
    /// <summary>How long an explosion's flames last in ticks, before the linger multiplier.</summary>
    public static float ExplosionFlameDuration = 110f;
    /// <summary>How the flames move through the explosion texture. Below 1 spends less time on the white flash at the start.</summary>
    public static float ExplosionFlameCurve = 0.6f;
    public Particle MakeExplosionFlameParticle(Vector3 position, out Action<Particle> ourAction, float lingerMultiplier = 1f, float particleScaleMultiplier = 1f) {
        var t = GameResources.GetGameResource<Texture2D>("Assets/textures/mine/explosion");
        var p = MakeParticle(position, t);

        int frameHeight = 66;
        int frameCount = t.Height / frameHeight;
        // in ticks. about 1.2 seconds for a regular explosion
        float duration = ExplosionFlameDuration * lingerMultiplier;
        float elapsed = 0f;

        p.Scale = new(particleScaleMultiplier);
        p.IsIn2DSpace = false;
        p.HasAdditiveBlending = false;
        p.Alpha = 1;
        p.Origin2D = new Vector2(t.Width / 2, frameHeight / 2);
        p.TextureCrop = new Rectangle(0, 0, t.Width, frameHeight);
        void act(Particle b) {
            elapsed += RuntimeData.DeltaTime;
            var progress = elapsed / duration;
            if (progress >= 1f) {
                p.Destroy();
                return;
            }

            // the first frames are a white flash. the curve keeps it short and leaves more time for the fire
            var frame = Math.Min((int)(MathF.Pow(progress, ExplosionFlameCurve) * frameCount), frameCount - 1);
            b.TextureCrop = new Rectangle(0, frame * frameHeight, t.Width, frameHeight);
        }
        ourAction = act;
        return p;
    }
    public void MakeSmallExplosion(Vector3 position, int numClouds, int numSparks, float shineScale, int movementFactor) {
        MakeFireball(position, numClouds, movementFactor);
        MakeSparkEmission(position, numSparks);
        MakeEmbers(position, numSparks, movementFactor);
        MakeShineSpot(position, Color.Orange, shineScale);
    }

    // colors by age in ticks. similar to the breff/breft... something like this
    static readonly (float Age, Color Color)[] _fireRamp = [
        (0f, new Color(255, 240, 190)),
        (4f, new Color(255, 200, 80)),
        (12f, new Color(255, 130, 50)),
        (20f, new Color(225, 80, 45)),
        (28f, new Color(145, 105, 95)),
        (36f, new Color(130, 126, 126)),
    ];
    static readonly (float Age, Color Color)[] _fireGlowRamp = [
        (0f, new Color(255, 250, 220)),
        (5f, new Color(255, 225, 110)),
        (14f, new Color(255, 150, 50)),
        (24f, new Color(120, 40, 10)),
        (30f, Color.Black),
    ];

    // lerps between the ramps
    static Color SampleRamp((float Age, Color Color)[] ramp, float age) {
        if (age <= ramp[0].Age)
            return ramp[0].Color;
        for (int i = 1; i < ramp.Length; i++) {
            if (age > ramp[i].Age) continue;
            var (a, ca) = ramp[i - 1];
            var (b, cb) = ramp[i];
            return Color.Lerp(ca, cb, (age - a) / (b - a));
        }
        return ramp[^1].Color;
    }

    /// <summary>A puff of smoke that starts orangeish and "cools" over time.</summary>
    public void MakeFireball(Vector3 position, int numPuffs, float spread) {
        var puffTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/misc/tank_smoke");
        var glowTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/misc/light_particle");
        var size = spread / 15f;
        // small fireballs pack their puffs closer together so they still read as one ball
        var packing = size * size;
        var puffScale = MathF.Sqrt(size);

        var halo = MakeParticle(position + new Vector3(0f, 0.5f, 0f), glowTexture);
        halo.HasAdditiveBlending = true;
        halo.ApplyGameLight = false;
        halo.Pitch = MathHelper.PiOver2;
        halo.Color = new Color(255, 220, 120);
        halo.Alpha = 0.55f;
        halo.Scale = new(2.2f * size);
        halo.UniqueBehavior = (p) => {
            GeometryUtils.Add(ref p.Scale, 0.05f * size * RuntimeData.DeltaTime);
            p.Alpha -= 0.03f * RuntimeData.DeltaTime;
            if (p.Alpha <= 0f)
                p.Destroy();
        };

        var random = Client.ClientRandom;

        for (int i = 0; i < numPuffs; i++) {
            var angle = random.NextFloat(0f, MathHelper.TwoPi);
            var distance = MathF.Sqrt(random.NextFloat(0f, 1f)) * 30f * packing;
            var start = position + new Vector3(MathF.Cos(angle) * distance, random.NextFloat(10f, 40f) * packing, MathF.Sin(angle) * distance * 0.8f);
            var velocity = new Vector3(MathF.Cos(angle), 0f, MathF.Sin(angle)) * random.NextFloat(0.4f, 1f) * packing;
            var life = random.NextFloat(46f, 58f);
            var puffSize = Client.ClientRandom.NextFloat(0.65f, 0.85f) * puffScale;
            var rise = random.NextFloat(0.35f, 0.6f);
            // puffs don't all appear at once
            var age = -random.NextFloat(0f, 4f);

            var puff = MakeParticle(start, puffTexture);
            puff.HasAdditiveBlending = false;
            puff.ApplyGameLight = false;
            puff.Pitch = -CameraGlobals.DEFAULT_ORTHOGRAPHIC_ANGLE;
            // the texture is shaded darker at the bottom, and tilting the quad toward the camera flips it upside down
            puff.Rotation2D = MathHelper.Pi;
            puff.Billboard = CameraGlobals.IsUsingFirstPersonCamera;
            puff.Alpha = 0f;

            // makes the fire glow while it's hot
            var glow = MakeParticle(start, glowTexture);
            glow.HasAdditiveBlending = true;
            glow.ApplyGameLight = false;
            glow.Pitch = puff.Pitch;
            glow.Billboard = puff.Billboard;
            glow.Alpha = 0f;

            puff.UniqueBehavior = (p) => {
                var dt = RuntimeData.DeltaTime;
                age += dt;
                if (age < 0f)
                    return;
                if (age >= life) {
                    p.Destroy();
                    glow.Destroy();
                    return;
                }

                p.Position += velocity * dt;
                velocity *= MathF.Pow(0.9f, dt);
                if (age > 14f)
                    p.Position.Y += rise * dt;

                var scale = puffSize * (0.7f + 0.3f * MathHelper.Clamp(age / 14f, 0f, 1f) + 0.004f * age);
                p.Scale = new(scale);
                p.Color = SampleRamp(_fireRamp, age);

                // solid while burning, thinner once it's smoke, then gone
                var alpha = 0.95f * MathHelper.Clamp(age / 2f, 0f, 1f);
                alpha *= 1f - 0.45f * MathHelper.Clamp((age - 18f) / 12f, 0f, 1f);
                alpha *= 1f - MathHelper.Clamp((age - 30f) / (life - 30f), 0f, 1f);
                p.Alpha = alpha;

                if (age < 30f) {
                    glow.Position = p.Position;
                    glow.Scale = new(scale * 1.2f);
                    glow.Color = SampleRamp(_fireGlowRamp, age);
                    glow.Alpha = 0.5f;
                }
                else
                    glow.Destroy();
            };
        }
    }

    /// <summary>Small glowing bits that fly out of an explosion and burn out.</summary>
    /// <param name="spread">How far they fly. A destroyed tank uses 15.</param>
    public void MakeEmbers(Vector3 position, int count, float spread) {
        var texture = GameResources.GetGameResource<Texture2D>("Assets/textures/misc/light_particle");
        var size = spread / 15f;

        for (int i = 0; i < count; i++) {
            var angle = Client.ClientRandom.NextFloat(0f, MathHelper.TwoPi);
            var speed = Client.ClientRandom.NextFloat(1f, 2.6f) * size;
            var velocity = new Vector3(MathF.Cos(angle) * speed, Client.ClientRandom.NextFloat(0.1f, 0.5f), MathF.Sin(angle) * speed * 0.8f);
            var life = Client.ClientRandom.NextFloat(30f, 60f);
            var phase = Client.ClientRandom.NextFloat(0f, MathHelper.TwoPi);
            float age = 0f;

            var ember = MakeParticle(position + new Vector3(0f, Client.ClientRandom.NextFloat(6f, 20f), 0f), texture);
            ember.HasAdditiveBlending = true;
            ember.ApplyGameLight = false;
            ember.Pitch = -CameraGlobals.DEFAULT_ORTHOGRAPHIC_ANGLE;
            ember.Billboard = CameraGlobals.IsUsingFirstPersonCamera;
            ember.Color = Client.ClientRandom.Next(0, 2) == 0 ? new Color(255, 150, 130) : new Color(255, 215, 140);
            ember.Scale = new(Client.ClientRandom.NextFloat(0.2f, 0.3f));

            ember.UniqueBehavior = (p) => {
                var dt = RuntimeData.DeltaTime;
                age += dt;
                if (age >= life) {
                    p.Destroy();
                    return;
                }

                p.Position += velocity * dt;
                var damping = MathF.Pow(0.94f, dt);
                velocity.X *= damping;
                velocity.Z *= damping;
                velocity.Y = velocity.Y * damping + 0.01f * dt;

                // twinkles while it burns out
                p.Alpha = (0.6f + 0.4f * MathF.Sin(age * 0.6f + phase)) * (1f - age / life);
            };
        }
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
            spark.Billboard = CameraGlobals.IsUsingFirstPersonCamera;

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

            smoke.Billboard = CameraGlobals.IsUsingFirstPersonCamera;

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
        p.Billboard = CameraGlobals.IsUsingFirstPersonCamera;
        p.UniqueBehavior = (part) => {
            GeometryUtils.Add(ref p.Scale, -0.0175f * RuntimeData.DeltaTime);

            p.Alpha -= 0.025f * RuntimeData.DeltaTime;

            if (p.Alpha <= 0f || p.Scale.X <= 0f)
                p.Destroy();
        };
    }
}
