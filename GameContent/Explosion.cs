using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.Systems.AI;
using TanksRebirth.Graphics;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent;

public class Explosion : IAITankDanger {
    public delegate void PostUpdateDelegate(Explosion explosion);
    public static event PostUpdateDelegate? OnPostUpdate;
    public delegate void PostRenderDelegate(Explosion explosion);
    public static event PostRenderDelegate? OnPostRender;

    /// <summary>The "owner" of this explosion.</summary>
    public Tank? Source;

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

    public Matrix View;
    public Matrix Projection;
    public Matrix World;

    public Model Model;

    private static Texture2D? _maskingTex;

    /// <summary>Only merlin himself could decode why I use this... Use this number with any explosion-based calculations.</summary>
    public const float MAGIC_EXPLOSION_NUMBER = 9f;

    public float Scale;

    public float MaxScale;

    public float ExpanseRate = 1f;
    public float ShrinkRate = 1f;

    public float ShrinkDelay = 40;

    private bool _maxAchieved;

    private int _id;

    public float Rotation;

    public float RotationSpeed;

    public Explosion(Vector2 pos, float scaleMax, Tank? owner = null, float rotationSpeed = 1f, float soundPitch = -0.3f) {
        RotationSpeed = rotationSpeed;
        Position = pos;
        MaxScale = scaleMax;
        Source = owner;
        _maskingTex = GameResources.GetGameResource<Texture2D>("Assets/textures/misc/tank_smoke_ami");

        AITank.Dangers.Add(this);
        IsPlayerSourced = owner is not null && owner is PlayerTank;

        Model = GameResources.GetGameResource<Model>("Assets/mineexplosion");

        int index = Array.IndexOf(Explosions, null);

        var destroysound = "Assets/sounds/mine_explode.ogg";

        int vertLayers = 5;
        int horizLayers = 15;

        // my brain hurts help pls
        // employ 3d rotation tactics
        // the spherically displayed particles.
        for (int i = 0; i <= vertLayers; i++) {
            for (int j = 0; j <= horizLayers; j++) {
                var rotX = MathHelper.Pi / horizLayers * j;
                var rotZ = MathHelper.Pi / vertLayers * i;

                float rotation = 0f;

                var explScalar = 45f;

                var position = Vector3.Transform(Vector3.UnitX * explScalar, Matrix.CreateFromYawPitchRoll(rotZ, 0, rotX) * Matrix.CreateTranslation(Position3D));
                // this will become a model.
                var particle = GameHandler.Particles.MakeParticle(position, GameResources.GetGameResource<Texture2D>("Assets/textures/tnk_tank_env"));
                // TODO: make particles face center of explosion

                particle.Scale = new(0.5f);
                particle.Alpha = 1f;
                particle.HasAddativeBlending = true;
                //particle.Yaw = rotZ.ToRotation();
                //particle.Roll = rotX.ToRotation();

                particle.UniqueBehavior = (a) => {
                    rotation += 0.025f * TankGame.DeltaTime;
                    position = Vector3.Transform(Vector3.UnitX * explScalar, Matrix.CreateFromYawPitchRoll(rotZ + rotation, 0, rotX) * Matrix.CreateTranslation(Position3D));
                    particle.Position = position;
                    //var dir = MathUtils.DirectionOf(position.FlattenZ(), Position3D.FlattenZ()).ToRotation();

                    particle.Pitch = (position.FlattenZ() - Position3D.FlattenZ()).ToRotation() + MathHelper.PiOver2;
                    particle.Roll = (position.Flatten() - Position3D.Flatten()).ToRotation() + MathHelper.PiOver2;
                    if (particle.LifeTime > 20) {
                        particle.Scale -= Vector3.One * 0.01f * TankGame.DeltaTime;
                    }
                    if (particle.Scale.X < 0)
                        particle.Destroy();
                };
            }
        }

        SoundPlayer.PlaySoundInstance(destroysound, SoundContext.Effect, 1f, 0f, soundPitch, gameplaySound: true);

        _id = index;

        Explosions[index] = this;
    }

    public void Update() {
        if (!MapRenderer.ShouldRenderAll)
            return;
        if (!_maxAchieved) {
            if (Scale < MaxScale)
                Scale += ExpanseRate * TankGame.DeltaTime;

            if (Scale > MaxScale)
                Scale = MaxScale;

            if (Scale >= MaxScale)
                _maxAchieved = true;
        }
        else if (ShrinkDelay <= 0)
            Scale -= ShrinkRate * TankGame.DeltaTime;

        if (!IntermissionSystem.IsAwaitingNewMission) {
            foreach (var mine in Mine.AllMines) {
                if (mine is not null && Vector2.Distance(mine.Position, Position) <= Scale * MAGIC_EXPLOSION_NUMBER) // magick
                    mine.Detonate();
            }
            foreach (var block in Block.AllBlocks) {
                if (block is not null && Vector2.Distance(block.Position, Position) <= Scale * MAGIC_EXPLOSION_NUMBER && block.IsDestructible)
                    block.Destroy();
            }
            foreach (var shell in Shell.AllShells) {
                if (shell is not null && Vector2.Distance(shell.Position, Position) < Scale * MAGIC_EXPLOSION_NUMBER)
                    shell.Destroy(Shell.DestructionContext.WithExplosion);
            }
            foreach (var tank in GameHandler.AllTanks) {
                if (tank is null || Vector2.Distance(tank.Position, Position) > Scale * MAGIC_EXPLOSION_NUMBER
                    || tank.Dead || HasHit[tank.WorldId] || !tank.Properties.VulnerableToMines)
                    continue;
                HasHit[tank.WorldId] = true;
                if (Source is null)
                    tank.Damage(new TankHurtContextOther(TankHurtContextOther.HurtContext.FromIngame));
                else if (Source is not null) {
                    tank.Damage(new TankHurtContextMine(Source is not AITank, this));
                }
            }
        }

        if (_maxAchieved)
            ShrinkDelay -= TankGame.DeltaTime;

        if (Scale <= 0)
            Remove();

        Rotation += RotationSpeed * TankGame.DeltaTime;

        OnPostUpdate?.Invoke(this);
    }

    public void Remove() {
        AITank.Dangers.Remove(this);
        Explosions[_id] = null;
    }

    public void Render() {
        if (!MapRenderer.ShouldRenderAll)
            return;
        World = Matrix.CreateScale(Scale) * Matrix.CreateRotationY(Rotation) * Matrix.CreateTranslation(Position3D);
        View = TankGame.GameView;
        Projection = TankGame.GameProjection;

        foreach (ModelMesh mesh in Model.Meshes) {
            foreach (BasicEffect effect in mesh.Effects) {
                effect.World = World;
                effect.View = View;
                effect.Projection = Projection;
                effect.TextureEnabled = true;

                effect.Texture = _maskingTex;

                effect.SetDefaultGameLighting_IngameEntities();

                effect.AmbientLightColor = Color.Orange.ToVector3();
                effect.DiffuseColor = Color.Orange.ToVector3();
                effect.EmissiveColor = Color.Orange.ToVector3();
                effect.FogColor = Color.Orange.ToVector3();

                if (ShrinkDelay <= 0)
                    effect.Alpha -= 0.05f * TankGame.DeltaTime;
                else
                    effect.Alpha = 1f;
            }
            mesh.Draw();
        }
        OnPostRender?.Invoke(this);
    }
}
