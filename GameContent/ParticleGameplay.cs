using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using tainicom.Aether.Physics2D.Fluids;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Net;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Graphics;
using TanksRebirth.GameContent.Globals.Assets;
using TanksRebirth.GameContent.Systems.ParticleSystem;

namespace TanksRebirth.GameContent;

/// <summary>Each of these particles are server-shared.</summary>
public static class ParticleGameplay
{
    // TODO: track smokes as objects, so ai can't shoot through?
    public static void CreateSmokeGrenade(ParticleManager manager, Vector3 position, Vector3 velocity) {
        var p = manager.MakeParticle(position, ModelGlobals.SmokeGrenade.Asset, GameResources.GetGameResource<Texture2D>("Assets/textures/smoke/smokenade"));
        bool exploded = false;

        float gravity = 0.03f;
        // for player
        //Vector2 velXZ = Vector2.UnitY.RotatedByRadians(pl.TurretRotation) / 4;
        //Vector3 initialVelocity = new(-velXZ.X, 1 + pl.Velocity.Length() / 3, velXZ.Y);

        int maxHits = 3;
        int hits = maxHits;
        float timer = 0f;
        bool startTimer = false;
        bool isSmokeDestroyed = false;
        Vector3 oldPosition = p.Position;
        float shadowPos = 0f;

        p.UniqueBehavior = (a) => {
            shadowPos = 0.1f;
            p.Scale = new(125);
            p.IsIn2DSpace = false;

            p.Position += velocity;
            velocity.Y -= gravity;

            if (hits > 0) {
                p.Roll += 0.07f * velocity.Length() * RuntimeData.DeltaTime;
                p.Pitch += 0.07f * velocity.Length() * RuntimeData.DeltaTime;
            }

            // bounce off walls
            if (p.Position.Y <= 80) {
                if ((p.Position.X <= GameScene.MIN_X && p.Position.X >= GameScene.MIN_X - 6) || (p.Position.X >= GameScene.MAX_X && p.Position.X <= GameScene.MAX_X + 6)) {
                    velocity.X = -velocity.X * 0.5f;
                }
                if ((p.Position.Z <= GameScene.MIN_Z && p.Position.Z >= GameScene.MIN_Z - 6) || (p.Position.Z >= GameScene.MAX_Z && p.Position.Z <= GameScene.MAX_Z + 6)) {
                    velocity.Z = -velocity.Z * 0.5f;
                }
            }
            // block collision
            for (int i = 0; i < Block.AllBlocks.Length; i++) {
                var block = Block.AllBlocks[i];
                if (block is null) continue;
                if (block.Hitbox.Contains(p.Position.FlattenZ())) {
                    shadowPos = block.HeightFromGround;
                    if (p.Position.Y < block.HeightFromGround) {
                        if (oldPosition.X > block.Hitbox.X + block.Hitbox.Width
                        || oldPosition.X < block.Hitbox.X)
                            velocity.X = -velocity.X * 0.75f;
                        else if (oldPosition.Z > block.Hitbox.Y + block.Hitbox.Height
                        || oldPosition.Z < block.Hitbox.Y)
                            velocity.Z = -velocity.Z * 0.75f;
                        if (oldPosition.Y >= block.HeightFromGround) {
                            // less bounces on blocks!
                            if (hits <= 1) {
                                velocity = Vector3.Zero;
                                p.Position.Y = block.HeightFromGround;
                                startTimer = true;
                            }
                            hits--;
                            velocity.Y = -velocity.Y * hits / maxHits;
                        }
                    }
                }
            }

            if (p.Position.Y < 7) {
                if (hits > 0) {
                    hits--;
                    velocity.Y = -velocity.Y * hits / maxHits;
                    p.Position.Y = 7;
                }
                else if (hits <= 0) {
                    velocity = Vector3.Zero;
                    p.Position.Y = 7;
                    startTimer = true;
                }
            }

            if (startTimer) timer += RuntimeData.DeltaTime;

            if (timer > 60 && !exploded) {
                exploded = true;
                SoundPlayer.PlaySoundInstance("Assets/sounds/smoke_hiss.ogg", SoundContext.Effect, 0.3f);
                for (int i = 0; i < 8; i++) {
                    var c = manager.MakeParticle(p.Position,
                        ModelGlobals.Smoke.Asset,
                        GameResources.GetGameResource<Texture2D>("Assets/textures/smoke/smoke"));
                    var randDir = new Vector3(Server.ServerRandom.NextFloat(-35, 35), 0, Server.ServerRandom.NextFloat(-35, 35));
                    c.Position += randDir;
                    var randSize = Server.ServerRandom.NextFloat(5, 10);
                    c.Scale.X = randSize;
                    c.Scale.Z = randSize;
                    c.UniqueBehavior = (b) => {
                        c.Pitch += 0.005f * RuntimeData.DeltaTime;
                        if (c.Scale.Y < randSize && c.LifeTime < 600)
                            c.Scale.Y += 0.1f * RuntimeData.DeltaTime;
                        if (c.LifeTime >= 600) {
                            c.Scale.Y -= 0.06f * RuntimeData.DeltaTime;
                            c.Alpha -= 0.06f / randSize * RuntimeData.DeltaTime;

                            if (c.Scale.Y <= 0) {
                                c.Destroy();
                            }
                        }
                    };
                }
                isSmokeDestroyed = true;
                p.Destroy();
            }
            oldPosition = p.Position;
        };

        var shadow = manager.MakeParticle(position, GameResources.GetGameResource<Texture2D>("Assets/textures/mine/mine_shadow"));
        shadow.Scale = new(0.8f);
        shadow.Color = Color.Black;
        shadow.HasAdditiveBlending = false;
        // shadow.Roll = -MathHelper.PiOver2;
        shadow.Pitch = MathHelper.PiOver2;

        shadow.UniqueBehavior = (a) => {
            if (isSmokeDestroyed) {
                shadow.Destroy();
            }
            shadow.Position.Y = shadowPos;
            shadow.Position.X = p.Position.X;
            shadow.Position.Z = p.Position.Z;

            shadow.Alpha = MathUtils.InverseLerp(150, 7, p.Position.Y, true);
        };
    }
}