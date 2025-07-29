using Microsoft.Win32;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Globals.Assets;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.Systems.AI;
using TanksRebirth.GameContent.Systems.TankSystem;
using TanksRebirth.Graphics;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent;

public class Explosion : IAITankDanger {
    public delegate void PostUpdateDelegate(Explosion explosion);
    public static event PostUpdateDelegate? OnPostUpdate;

    /// <summary>The "owner" of this explosion.</summary>
    public Tank? Owner;

    // 500 -> 80
    public const int MINE_EXPLOSIONS_MAX = 80;

    public static Explosion[] Explosions = new Explosion[MINE_EXPLOSIONS_MAX];
    public Vector2 Position { get; set; }
    public bool IsPlayerSourced { get; set; }
    /// <summary>
    /// An array representation of what tanks this explosion has already hit.
    /// If a tank's global ID is in this array, it has been damaged by this <see cref="Explosion"/>, and will not be damaged again by it.
    /// </summary>
    public bool[] HasHit = new bool[GameHandler.AllTanks.Length];
    public Vector3 Position3D => Position.ExpandZ();

    /// <summary>Only merlin himself could decode why I use this... Use this number with any explosion-based calculations.</summary>
    public const float MAGIC_EXPLOSION_NUMBER = 9f;
    public int Id { get; private set; }
    public int Team => Owner?.Team ?? TeamID.NoTeam;

    public float Scale;
    public float LingerDuration = 40f;
    public float Rotation;
    public float RotationSpeed;

    public float LifeTime;

    public Color ExplosionColor = Color.White;

    public Explosion(Vector2 pos, float scale, Tank? owner = null, float rotationSpeed = 1f, float soundPitch = 0f) {
        RotationSpeed = rotationSpeed;
        Position = pos;
        Scale = scale;
        Owner = owner;

        AITank.Dangers.Add(this);
        IsPlayerSourced = owner is not null && owner is PlayerTank;

        int index = Array.IndexOf(Explosions, null);

        var destroysound = "Assets/sounds/mine_explode.ogg";

        int horizLayers = (int)(scale * 1.5f) + 2; // 10
        int vertLayers = (int)(scale * 1.1f) + 2; // 8

        var ring = GameHandler.Particles.MakeParticle(Position3D + Vector3.UnitY,
            /*ModelResources.FlatFace.Asset, */
            GameResources.GetGameResource<Texture2D>("Assets/textures/misc/ring"));

        ring.Scale = new(1.3f);
        ring.Pitch = MathHelper.PiOver2;
        ring.HasAdditiveBlending = true;
        ring.Color = ExplosionColor == Color.White ? Color.Yellow : ExplosionColor;

        ring.UniqueBehavior = (a) => {
            ring.Alpha -= 0.08f * RuntimeData.DeltaTime;

            GeometryUtils.Add(ref ring.Scale, 0.04f * RuntimeData.DeltaTime);
            if (ring.Alpha <= 0)
                ring.Destroy();
        };

        for (int i = 0; i <= horizLayers; i++) {
            for (int j = 0; j <= vertLayers; j++) {
                var rotX = MathHelper.Pi / vertLayers * j;
                var rotZ = MathHelper.Pi / horizLayers * i;

                float rotation = 0f;
                float rotationSpeed1 = 0.06f * RotationSpeed;

                var explScalar = MAGIC_EXPLOSION_NUMBER * Scale;
                var lingerRandom = Client.ClientRandom.NextFloat(0.8f, 1.2f);
                var position = Vector3.UnitY * -5000f;
                var particle = GameHandler.Particles.MakeExplosionFlameParticle(position, out var act, LingerDuration / 60f * lingerRandom);

                // TODO: make particles face center of explosion
                particle.UniqueBehavior = (a) => {
                    act?.Invoke(particle);
                    particle.Color = ExplosionColor;
                    rotation += rotationSpeed1 * RuntimeData.DeltaTime;

                    // find why the rotation isnt rotating the right way. maybe needs a unit circle offset? piover2?
                    position = Vector3.Transform(Vector3.UnitX * explScalar, Matrix.CreateFromYawPitchRoll(rotZ + rotation, 0, rotX) * Matrix.CreateTranslation(Position3D));
                    particle.Position = position;

                    var rot = MathUtils.CalculateRotationToFaceCenter(particle.Position, Position3D);

                    particle.Roll = rot.X;
                    particle.Pitch = rot.Y;
                    particle.Yaw = rot.Z;
                };
            }
            horizLayers -= (int)MathF.Round((float)horizLayers / vertLayers);
        }

        var audio = SoundPlayer.PlaySoundInstance(destroysound, SoundContext.Effect, 1f, pitchOverride: soundPitch);
        //if (CameraGlobals.IsUsingFirstPresonCamera)
        //    SoundUtils.CreateSpatialSound(audio, Position3D, CameraGlobals.RebirthFreecam.Position, 1.25f);

        Id = index;

        Explosions[index] = this;
    }

    public void Update() {
        if (!IntermissionSystem.IsAwaitingNewMission) {
            foreach (var mine in Mine.AllMines) {
                if (mine is not null && Vector2.Distance(mine.Position, Position) <= Scale * MAGIC_EXPLOSION_NUMBER) // magick
                    mine.Detonate();
            }
            foreach (var block in Block.AllBlocks) {
                if (block is not null && Vector2.Distance(block.Position, Position) <= Scale * MAGIC_EXPLOSION_NUMBER && block.Properties.IsDestructible)
                    block.Destroy();
            }
            foreach (var shell in Shell.AllShells) {
                if (shell is not null && Vector2.Distance(shell.Position, Position) < Scale * MAGIC_EXPLOSION_NUMBER)
                    shell.Destroy(Shell.DestructionContext.WithExplosion);
            }
            foreach (var tank in GameHandler.AllTanks) {
                if (tank is null || Vector2.Distance(tank.Position, Position) > Scale * MAGIC_EXPLOSION_NUMBER
                    || tank.Dead || HasHit[tank.WorldId] || tank.Properties.InvulnerableToMines)
                    continue;
                HasHit[tank.WorldId] = true;
                if (Owner is null)
                    tank.Damage(new TankHurtContextOther(null, TankHurtContextOther.HurtContext.FromIngame, "Unowned Explosion"), true);
                else if (Owner is not null) {
                    tank.Damage(new TankHurtContextExplosion(this), true);
                }
            }
        }
        if (LifeTime > LingerDuration)
            Remove();

        Rotation += RotationSpeed * RuntimeData.DeltaTime;
        LifeTime += RuntimeData.DeltaTime;

        OnPostUpdate?.Invoke(this);
    }

    public void Remove() {
        AITank.Dangers.Remove(this);
        Explosions[Id] = null;
    }
}
