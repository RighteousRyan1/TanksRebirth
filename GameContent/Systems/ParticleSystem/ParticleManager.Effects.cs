using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Runtime.CompilerServices;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.Graphics.DynamicLighting;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.Systems.ParticleSystem;

// Effects recreated from the effect files of Wii Play Tanks. Sizes are in game units and times are in ticks.
public partial class ParticleManager {
    static Random Rand => Client.ClientRandom;

    // shorthand cuz writing GetGameResource 20x times is annoying.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Texture2D Tex(string name) => GameResources.GetGameResource<Texture2D>("Assets/textures/" + name);

    /// <summary>An unlit sprite facing the camera, like most effect particles.</summary>
    public Particle MakeOrthoParticle(Vector3 position, Texture2D texture, bool additive) {
        var p = MakeParticle(position, texture);
        p.HasAdditiveBlending = additive;
        p.ApplyGameLight = false;
        p.Pitch = -CameraGlobals.DEFAULT_ORTHOGRAPHIC_ANGLE;
        p.Billboard = CameraGlobals.IsUsingFirstPersonCamera;
        return p;
    }

    // should also go into utils 
    /// <summary>Rolls the particle so the bottom of its texture points along <paramref name="direction"/>.</summary>
    public static void PointAlong(Particle p, Vector3 direction) {
        var orientation = Matrix.CreateFromYawPitchRoll(p.Yaw, p.Pitch, 0f);
        p.Roll = MathF.Atan2(-Vector3.Dot(direction, orientation.Right), Vector3.Dot(direction, orientation.Up));
    }

    /// <summary>Moves the particle toward the camera, so a big particle near the floor isn't cut off by it.</summary>
    public static void LiftTowardCamera(Particle p, float distance) => p.Position += Matrix.CreateFromYawPitchRoll(p.Yaw, p.Pitch, 0f).Backward * distance;

    // particularly useful for this particle facelift... some of these methods could totally go into a utils class
    /// <summary>Calls <paramref name="update"/> every frame with how far the particle is through its life, from 0 to 1, then destroys it.</summary>
    public static void Animate(Particle p, float life, Action<Particle, float> update) {
        // float parameter in the action seems kinda pointless..?
        update(p, 0f);
        p.UniqueBehavior = particle => {
            var t = particle.LifeTime / life;
            if (t >= 1f) {
                particle.Destroy();
                return;
            }
            update(particle, t);
        };
    }

    static float Sample((float At, float Value)[] curve, float t) {
        if (t <= curve[0].At)
            return curve[0].Value;
        for (int i = 1; i < curve.Length; i++) {
            if (t > curve[i].At) continue;
            var (a, va) = curve[i - 1];
            var (b, vb) = curve[i];
            return MathHelper.Lerp(va, vb, (t - a) / (b - a));
        }
        return curve[^1].Value;
    }

    // lerps between two colors at specific points of particle lifetime, kind of like how the wii does it
    static Color Sample((float At, Color Value)[] curve, float t) {
        if (t <= curve[0].At)
            return curve[0].Value;
        for (int i = 1; i < curve.Length; i++) {
            if (t > curve[i].At) continue;
            var (a, ca) = curve[i - 1];
            var (b, cb) = curve[i];
            return Color.Lerp(ca, cb, (t - a) / (b - a));
        }
        return curve[^1].Value;
    }

    // this stuff can be used for basically anything... but mainly are used for 

    static readonly (float, Color)[] _sparkColor = [(0f, Color.White), (0.14f, Color.White), (0.43f, new Color(255, 255, 0)), (0.57f, new Color(220, 0, 0))];
    static readonly (float, float)[] _sparkLength = [(0f, 1f), (0.28f, 1f), (1f, 0.3f)];

    void MakeSparks(Vector3 position, int count, Vector3 axis, float spread, float speed, float outward, float drag, float life) {
        var texture = Tex("misc/particle_line");
        for (int i = 0; i < count; i++) {
            var direction = MathUtils.RandomDirection(axis, spread);
            var velocity = (direction * speed + MathUtils.Outward(direction) * outward) * Rand.NextFloat(0.6f, 1.4f);
            var width = Rand.NextFloat(1.4f, 2.8f);
            var length = Rand.NextFloat(5f, 9f);

            var spark = MakeOrthoParticle(position, texture, true);
            // the bright end of the texture leads
            PointAlong(spark, -velocity);
            Animate(spark, life * Rand.NextFloat(0.75f, 1.25f), (p, t) => {
                p.Position += velocity * RuntimeData.DeltaTime;
                velocity *= MathF.Pow(drag, RuntimeData.DeltaTime);
                p.Color = Sample(_sparkColor, t);
                p.Scale = MathUtils.SizeToScale(texture, width, length * Sample(_sparkLength, t));
            });
        }
    }

    static readonly (float, Color)[] _ringColor = [(0f, new Color(255, 111, 108)), (0.15f, new Color(255, 45, 45))];

    void MakeImpactRing(Vector3 position, float size, float life, float alpha) {
        var texture = Tex("misc/ring");
        var ring = MakeOrthoParticle(position, texture, true);
        Animate(ring, life, (p, t) => {
            p.Color = Sample(_ringColor, t);
            p.Alpha = alpha * MathHelper.Clamp((1f - t) * 3f, 0f, 1f);
            p.Scale = MathUtils.SizeToScale(texture, size * MathHelper.Lerp(0.4f, 1f, MathF.Min(t / 0.85f, 1f)));
        });
    }

    static readonly (float, Color)[] _flashColor = [(0f, new Color(255, 255, 183)), (0.8f, new Color(255, 255, 48))];

    void MakeImpactFlash(Vector3 position, float size, float life) {
        var texture = Tex("misc/light_star");
        var flash = MakeOrthoParticle(position, texture, true);
        flash.Rotation2D = Rand.NextFloat(0f, MathHelper.TwoPi);
        Animate(flash, life, (p, t) => {
            p.Color = Sample(_flashColor, t);
            p.Alpha = 1f - t;
            p.Scale = MathUtils.SizeToScale(texture, size * MathHelper.Lerp(1f, 0.5f, MathF.Min(t / 0.8f, 1f)));
        });
    }

    static readonly (float, Color)[] _fireColor = [(0f, new Color(255, 100, 0)), (0.8f, Color.Black)];
    static readonly (float, float)[] _fireSize = [(0f, 4f), (0.04f, 7f), (0.18f, 7.7f), (0.41f, 6.8f), (1f, 3f)];
    static readonly (float, float)[] _fireAlpha = [(0f, 1f), (0.65f, 1f), (0.95f, 0f)];

    void MakeFirePuffs(Vector3 center, int count, float radius, float speed, float drag, float rise, float size, float life, Color smokeColor) {
        var texture = Tex("misc/tank_smoke_light");
        var smokeTexture = Tex("misc/tank_smoke");
        for (int i = 0; i < count; i++) {
            var direction = MathUtils.RandomDirection(Vector3.Up, MathHelper.PiOver2);
            var velocity = direction * speed;
            var puffSize = size * Rand.NextFloat(0.6f, 1.4f);
            var puffLife = life * Rand.NextFloat(0.5f, 1f);
            var start = center + direction * radius * Rand.NextFloat(0.84f, 1f);

            var fire = MakeOrthoParticle(start, texture, true);
            fire.Rotation2D = MathHelper.Pi;
            // above the smoke
            fire.Layer = 0.01f;
            Animate(fire, puffLife, (p, t) => {
                p.Position += velocity * RuntimeData.DeltaTime;
                velocity *= MathF.Pow(drag, RuntimeData.DeltaTime);
                velocity.Y += rise * RuntimeData.DeltaTime;
                p.Color = Sample(_fireColor, t);
                p.Alpha = Sample(_fireAlpha, t);
                p.Scale = MathUtils.SizeToScale(texture, puffSize * Sample(_fireSize, t));
            });

            var smoke = MakeOrthoParticle(start, smokeTexture, false);
            smoke.Rotation2D = MathHelper.Pi;
            smoke.Color = smokeColor;
            Animate(smoke, puffLife, (p, t) => {
                p.Position = fire.Position;
                p.Alpha = fire.Alpha;
                p.Scale = fire.Scale;
            });
        }
    }

    // newer effects for the facelift
    static readonly (float, float)[] _bigFlashSize = [(0f, 175f), (0.2f, 135f), (0.8f, 80f)];
    static readonly (float, float)[] _glintWidth = [(0f, 0.7f), (0.58f, 1f), (1f, 0.1f)];
    static readonly (float, float)[] _glintLength = [(0f, 2f), (0.58f, 1f), (1f, 0.1f)];
    static readonly (float, float)[] _debrisAlpha = [(0f, 1f), (0.67f, 1f), (0.94f, 0f)];

    // some of this stuff was read via the breft thru claude for easier porting. it would be a nightmare to write all of these numbers myself.
    public void MakeTankExplosion(Vector3 position, Color debrisColor) {
        var starTexture = Tex("misc/bot_hit");
        var star = MakeOrthoParticle(position, starTexture, true);
        star.Color = new Color(145, 136, 104);
        // drawn over the fire
        star.Layer = 1f;
        // it breaks the first person look to have this.
        // LiftTowardCamera(star, 50f);
        Animate(star, 15f, (p, t) => {
            p.Alpha = 1f - MathF.Min(t / 0.7f, 1f);
            p.Scale = MathUtils.SizeToScale(starTexture, Sample(_bigFlashSize, t));
        });

        var glowTexture = Tex("misc/light_particle");
        var glow = MakeOrthoParticle(star.Position, glowTexture, true);
        glow.Color = new Color(76, 52, 28);
        glow.Layer = 1f;
        Animate(glow, 15f, (p, t) => {
            p.Alpha = star.Alpha;
            p.Scale = MathUtils.SizeToScale(glowTexture, Sample(_bigFlashSize, t));
        });

        var waveTexture = Tex("misc/ring");
        var wave = MakeOrthoParticle(position, waveTexture, true);
        wave.Pitch = MathHelper.PiOver2;
        wave.Billboard = false;
        wave.Color = new Color(255, 255, 92);
        Animate(wave, 15f, (p, t) => {
            p.Alpha = 0.6f * (1f - t);
            p.Scale = MathUtils.SizeToScale(waveTexture, MathHelper.Lerp(17.5f, 140f, MathF.Min(t / 0.5f, 1f)));
        });

        var glintTexture = Tex("misc/light_particle");
        for (int i = 0; i < 30; i++) {
            var velocity = MathUtils.RandomDirection(Vector3.Up, MathHelper.PiOver2) * 8.5f * Rand.NextFloat(0.65f, 1f);
            var size = Rand.NextFloat(3f, 11f);
            var glint = MakeOrthoParticle(position, glintTexture, true);
            glint.Color = Rand.Next(0, 2) == 0 ? new Color(255, 180, 190) : new Color(255, 180, 255);
            glint.Alpha = 0.8f;
            Animate(glint, 60f * Rand.NextFloat(0.2f, 1f), (p, t) => {
                p.Position += velocity * RuntimeData.DeltaTime;
                velocity *= MathF.Pow(0.92f, RuntimeData.DeltaTime);
                velocity.Y -= 0.042f * RuntimeData.DeltaTime;
                PointAlong(p, velocity);
                p.Scale = MathUtils.SizeToScale(glintTexture, size * Sample(_glintWidth, t), size * Sample(_glintLength, t));
            });
        }

        MakeFirePuffs(position, 18, radius: 4.2f, speed: 3.3f, drag: 0.9f, rise: 0.056f, size: 4.3f, life: 50f, smokeColor: new Color(90, 90, 90));

        var debrisTexture = Tex("misc/tank_rock_2");
        for (int i = 0; i < 16; i++) {
            var angle = Rand.NextFloat(0f, MathHelper.TwoPi);
            var outward = new Vector3(MathF.Cos(angle), 0f, MathF.Sin(angle));
            var velocity = (outward * 2.2f + MathUtils.RandomDirection(Vector3.Up, 0.87f) * 5.6f) * Rand.NextFloat(0.6f, 1.4f);
            var spin = Rand.NextFloat(-0.12f, 0.12f);
            var piece = MakeOrthoParticle(position + outward * Rand.NextFloat(7.3f, 14f) + new Vector3(0f, 2f, 0f), debrisTexture, false);
            piece.Rotation2D = Rand.NextFloat(0f, MathHelper.TwoPi);
            piece.Color = Color.Lerp(debrisColor, debrisColor * 0.82f, Rand.NextFloat()) with { A = 255 };
            piece.Scale = MathUtils.SizeToScale(debrisTexture, Rand.NextFloat(2.5f, 7.5f));
            Animate(piece, 50f * Rand.NextFloat(0.7f, 1f), (p, t) => {
                p.Position += velocity * RuntimeData.DeltaTime;
                velocity *= MathF.Pow(0.96f, RuntimeData.DeltaTime);
                velocity.Y -= 0.28f * RuntimeData.DeltaTime;
                p.Rotation2D += spin * RuntimeData.DeltaTime;
                p.Alpha = Sample(_debrisAlpha, t);
            });
        }

        // this honestly might be shit...
        var light = new PointLight(position, Color.Orange, 20f, 100f, true) { Priority = 1 };
        float lifetime = 60f;
        var plight = GameplayLights.CreateTempLight(GameHandler.Particles, position, light, lifetime);
        // ParticleManager.Animate(plight, lifetime, )
        plight.UniqueBehavior += (a) => {
            light.Intensity -= 0.5f * RuntimeData.DeltaTime;
        };
    }

    public void MakeShellImpact(Vector3 position, bool addFlames = true) {
        if (addFlames) {
            MakeImpactFlash(position, 50f, 20f);
            MakeImpactRing(position, 35f, 10f, 0.78f);
            MakeSparks(position, 16, Vector3.Up, MathHelper.Pi, speed: 1.2f, outward: 2.8f, drag: 0.92f, life: 25f);
            MakeFirePuffs(position, 6, radius: 2.8f, speed: 2.1f, drag: 0.88f, rise: 0.07f, size: 2.2f, life: 43f, smokeColor: new Color(76, 76, 76));
            return;
        }
        MakeImpactFlash(position, 34f, 10f);
        MakeImpactRing(position, 17.5f, 8f, 0.7f);
        MakeSparks(position, 8, Vector3.Up, MathHelper.Pi, speed: 0.8f, outward: 2.8f, drag: 0.94f, life: 15f);
    }

    // ported from an earlier set of breff/breft color arrays seen from brawlcrate. same thing with the other tuple arrays.
    static readonly (float, Color)[] _muzzleStarColor = [(0f, new Color(255, 200, 150)), (0.33f, new Color(255, 100, 0)), (0.67f, new Color(255, 0, 0))];
    static readonly (float, float)[] _muzzleStarSize = [(0f, 22f), (0.33f, 34f), (0.67f, 17f)];
    static readonly (float, Color)[] _muzzleSmokeColor = [(0f, new Color(255, 240, 150)), (0.08f, new Color(250, 150, 140)), (0.17f, new Color(200, 140, 140)), (0.29f, new Color(140, 140, 140))];
    static readonly (float, float)[] _muzzleSmokeSize = [(0f, 1f), (0.55f, 2.5f), (1f, 3f)];
    static readonly (float, float)[] _muzzleSmokeAlpha = [(0f, 1f), (0.36f, 1f), (1f, 0f)];

    public void MakeMuzzleFlash(Vector3 muzzle, Vector3 direction) {
        direction.Normalize();
        MakeImpactRing(muzzle, 35f, 6f, 0.78f);

        var starTexture = Tex("misc/bot_hit");
        var star = MakeOrthoParticle(muzzle, starTexture, additive: true);
        star.Rotation2D = Rand.NextFloat(0f, MathHelper.TwoPi);
        Animate(star, 6f, (p, t) => {
            p.Color = Sample(_muzzleStarColor, t);
            p.Alpha = 1f - MathF.Min(t / 0.75f, 1f);
            p.Scale = MathUtils.SizeToScale(starTexture, Sample(_muzzleStarSize, t));
        });

        MakeSparks(muzzle, 6, direction, MathHelper.PiOver2, speed: 2.4f, outward: 0f, drag: 0.92f, life: 12f);

        var smokeTexture = Tex("misc/tank_smoke_light");
        var velocity = direction * 1.4f * Rand.NextFloat(0.6f, 1.4f);
        var size = Rand.NextFloat(4f, 10f);
        var smoke = MakeOrthoParticle(muzzle - direction * 4.2f, smokeTexture, additive: false);
        smoke.Rotation2D = MathHelper.Pi;
        Animate(smoke, 45f, (p, t) => {
            p.Position += velocity * RuntimeData.DeltaTime;
            velocity *= MathF.Pow(0.9f, RuntimeData.DeltaTime);
            p.Color = Sample(_muzzleSmokeColor, t);
            p.Alpha = Sample(_muzzleSmokeAlpha, t);
            p.Scale = MathUtils.SizeToScale(smokeTexture, size * Sample(_muzzleSmokeSize, t));
        });
    }

    static readonly (float, float)[] _rockAlpha = [(0f, 1f), (0.84f, 1f), (0.95f, 0f)];

    /// <summary>Rocks flying off a destroyed block. <paramref name="position"/> is on the ground under the block.</summary>
    public void MakeBlockBreak(Vector3 position) {
        // magical ahh numbers (part 500)
        var center = position + new Vector3(0f, 15.8f, 0f);
        var texture = Tex("misc/tank_rock_light");
        for (int i = 0; i < 15; i++) {
            // the first few are the big ones
            bool large = i < 6;
            var start = new Vector3(Rand.NextFloat(-1f, 1f), Rand.NextFloat(-1f, 1f), Rand.NextFloat(-1f, 1f));
            // pushed out to the surface of the block
            start /= MathF.Max(MathF.Abs(start.X), MathF.Max(MathF.Abs(start.Y), MathF.Abs(start.Z)));
            // the bigger ones go higher so they're more noticeable
            var velocity = MathUtils.Outward(start) * (large ? 0.8f : 2.2f) + MathUtils.RandomDirection(Vector3.Up, 0.35f) * (large ? 7f : 8.4f);
            var momentum = 1f - Rand.NextFloat(0f, large ? 0.18f : 0.35f);
            var size = large ? Rand.NextFloat(4.5f, 23.5f) : Rand.NextFloat(1.9f, 9.3f);
            var spin = Rand.NextFloat(-1f, 1f);

            var rock = MakeOrthoParticle(center + start * 10f, texture, false);
            rock.Color = new Color(235, 177, 136);
            rock.Scale = MathUtils.SizeToScale(texture, size);
            Animate(rock, 60f * Rand.NextFloat(0.75f, 1f), (p, t) => {
                p.Position += velocity * momentum * RuntimeData.DeltaTime;
                velocity.Y -= 0.31f * RuntimeData.DeltaTime;
                p.Rotation2D = 0.35f + spin * MathF.Min(t / 0.86f, 1f) * 1.5f;
                p.Alpha = Sample(_rockAlpha, t);
            });
        }
    }

    static readonly (float, Color)[] _trailSmokeColor = [(0f, new Color(220, 190, 190)), (0.1f, new Color(190, 190, 190)), (1f, new Color(120, 120, 120))];
    static readonly (float, Color)[] _rocketSmokeColor = [(0f, new Color(250, 200, 200)), (0.2f, new Color(220, 200, 200)), (0.3f, new Color(200, 200, 200))];

    public void MakeShellSmoke(Vector3 position, Vector3 direction, Color tint, bool rocket) {
        var texture = Tex("misc/tank_smoke_light");
        var velocity = MathUtils.RandomDirection() * 0.07f - (rocket ? direction * 1.4f * Rand.NextFloat(0.87f, 1.13f) : Vector3.Zero);
        var size = rocket ? Rand.NextFloat(2.5f, 5.9f) : Rand.NextFloat(2.1f, 7.7f);
        var colors = rocket ? _rocketSmokeColor : _trailSmokeColor;

        var smoke = MakeOrthoParticle(position, texture, false);
        smoke.Rotation2D = MathHelper.Pi;
        Animate(smoke, rocket ? 20f : 60f, (p, t) => {
            p.Position += velocity * RuntimeData.DeltaTime;
            p.Color = new Color(Sample(colors, t).ToVector3() * tint.ToVector3());
            p.Alpha = 1f - t;
            p.Scale = MathUtils.SizeToScale(texture, size * (rocket ? MathHelper.Lerp(1.25f, 3f, t) : MathHelper.Lerp(1f, 3f, t)));
        });
    }

    public void MakeShellFlame(Shell shell) {
        var texture = Tex("bullet/flame");
        var length = Rand.NextFloat(0.5f, 0.75f);
        var rollOffset = Rand.NextFloat(-0.25f, 0.25f);

        var flame = MakeParticle(shell.Position3D, texture);
        flame.HasAdditiveBlending = false;
        flame.Layer = 1f;
        flame.Color = shell.Properties.FlameColor;
        flame.Pitch = MathHelper.PiOver2;
        flame.Rotation2D = -MathHelper.PiOver2;
        flame.Origin2D = new(texture.Width / 2, texture.Height);
        Animate(flame, length / 0.06f, (p, t) => {
            p.Position = shell.Position3D;
            p.Roll = shell.Rotation + MathHelper.PiOver2 + rollOffset;
            // x is outward from the shell
            p.Scale = new(length * (1f - t), 0.165f, 0.4f);
        });
    }
}
