using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Graphics;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent
{
    public class Explosion
    {
        // model, blah blah blah

        public delegate void PostUpdateDelegate(Explosion explosion);
        public static event PostUpdateDelegate OnPostUpdate;
        public delegate void PostRenderDelegate(Explosion explosion);
        public static event PostRenderDelegate OnPostRender;

        public Tank Source;

        public const int MINE_EXPLOSIONS_MAX = 500;

        public static Explosion[] Explosions = new Explosion[MINE_EXPLOSIONS_MAX];

        public Vector2 Position;

        public bool[] HasHit = new bool[GameHandler.AllTanks.Length];

        public Vector3 Position3D => Position.ExpandZ();

        public Matrix View;
        public Matrix Projection;
        public Matrix World;

        public Model Model;

        private static Texture2D _maskingTex;

        public float Scale;

        public float MaxScale;

        public float ExpanseRate = 1f;
        public float ShrinkRate = 1f;

        public float ShrinkDelay = 40;

        private bool _maxAchieved;

        private int _id;

        public float Rotation;

        public float RotationSpeed;

        public Explosion(Vector2 pos, float scaleMax, Tank owner = null, float rotationSpeed = 1f)
        {
            RotationSpeed = rotationSpeed;
            Position = pos;
            MaxScale = scaleMax;
            Source = owner;
            _maskingTex = GameResources.GetGameResource<Texture2D>("Assets/textures/misc/tank_smoke_ami");

            Model = GameResources.GetGameResource<Model>("Assets/mineexplosion");

            int index = Array.IndexOf(Explosions, Explosions.First(t => t is null));

            var destroysound = "Assets/sounds/tnk_destroy";

            SoundPlayer.PlaySoundInstance(destroysound, SoundContext.Effect, 0.4f, 0f, gameplaySound: true);
            SoundPlayer.PlaySoundInstance(destroysound, SoundContext.Effect, 0.4f, 0f, -0.2f, gameplaySound: true);

            _id = index;

            Explosions[index] = this;
        }

        public void Update() {
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
                    if (mine is not null && Vector2.Distance(mine.Position, Position) <= Scale * 9) // magick
                        mine.Detonate();
                }
                foreach (var block in Block.AllBlocks) {
                    if (block is not null && Vector2.Distance(block.Position, Position) <= Scale * 9 && block.IsDestructible)
                        block.Destroy();
                }
                foreach (var shell in Shell.AllShells) {
                    if (shell is not null && Vector2.Distance(shell.Position2D, Position) < Scale * 9)
                        shell.Destroy(Shell.DestructionContext.WithExplosion);
                }
                foreach (var tank in GameHandler.AllTanks) {
                    if (tank is not null && Vector2.Distance(tank.Position, Position) < Scale * 9)
                        if (!tank.Dead)
                            if (!HasHit[tank.WorldId])
                                if (tank.Properties.VulnerableToMines) {
                                    HasHit[tank.WorldId] = true;
                                    if (Source is null)
                                        tank.Damage(new TankHurtContext_Other(TankHurtContext_Other.HurtContext.FromIngame));
                                    else if (Source is not null) {
                                        if (Source is AITank)
                                            tank.Damage(new TankHurtContext_Mine(false, Source.WorldId));
                                        else
                                            tank.Damage(new TankHurtContext_Mine(true, Source.WorldId));
                                    }
                                }
                }
            }

            if (_maxAchieved)
                ShrinkDelay -= TankGame.DeltaTime;

            if (Scale <= 0)
                Remove();

            Rotation += RotationSpeed * TankGame.DeltaTime;

            World = Matrix.CreateScale(Scale) * Matrix.CreateRotationY(Rotation) * Matrix.CreateTranslation(Position3D);
            View = TankGame.GameView;
            Projection = TankGame.GameProjection;

            OnPostUpdate?.Invoke(this);
        }

        public void Remove()
        {
            Explosions[_id] = null;
        }

        public void Render() {
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
}
