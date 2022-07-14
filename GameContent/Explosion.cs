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

        public int ShrinkDelay = 40;

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
            _maskingTex = GameResources.GetGameResource<Texture2D>(/*"Assets/textures/mine/explosion_mask"*/"Assets/textures/misc/tank_smoke_ami");

            Model = GameResources.GetGameResource<Model>("Assets/mineexplosion");

            int index = Array.IndexOf(Explosions, Explosions.First(t => t is null));

            var destroysound = "Assets/sounds/tnk_destroy";

            SoundPlayer.PlaySoundInstance(destroysound, SoundContext.Effect, 0.4f);

            _id = index;

            Explosions[index] = this;
        }

        public void Update()
        {
            if (!_maxAchieved)
            {
                if (Scale < MaxScale)
                    Scale += ExpanseRate;

                if (Scale > MaxScale)
                    Scale = MaxScale;

                if (Scale >= MaxScale)
                    _maxAchieved = true;
            }
            else if (ShrinkDelay <= 0)
                Scale -= ShrinkRate;

            if (!IntermissionSystem.IsAwaitingNewMission)
            {
                foreach (var mine in Mine.AllMines)
                {
                    if (mine is not null && Vector2.Distance(mine.Position, Position) <= Scale * 9) // magick
                        mine.Detonate();
                }
                foreach (var cube in Block.AllBlocks)
                {
                    if (cube is not null && Vector2.Distance(cube.Position, Position) <= Scale * 9 && cube.IsDestructible)
                        cube.Destroy();
                }
                foreach (var shell in Shell.AllShells)
                {
                    if (shell is not null && Vector2.Distance(shell.Position2D, Position) < Scale * 9)
                        shell.Destroy();
                }
                foreach (var tank in GameHandler.AllTanks)
                {
                    if (tank is not null && Vector2.Distance(tank.Position, Position) < Scale * 9)
                        if (!tank.Dead)
                            if (!HasHit[tank.WorldId])
                                if (tank.Properties.VulnerableToMines)
                                {
                                    HasHit[tank.WorldId] = true;
                                    if (Source is null)
                                        tank.Damage(new TankHurtContext_Other(TankHurtContext_Other.HurtContext.FromIngame));
                                    else if (Source is not null)
                                    {
                                        if (Source is AITank)
                                            tank.Damage(new TankHurtContext_Mine(false, Source.WorldId));
                                        else
                                            tank.Damage(new TankHurtContext_Mine(true, Source.WorldId));
                                    }
                                }
                }
            }

            if (_maxAchieved)
                ShrinkDelay--;

            if (Scale <= 0)
                Remove();

            Rotation += RotationSpeed;

            World = Matrix.CreateScale(Scale) * Matrix.CreateRotationY(Rotation) * Matrix.CreateTranslation(Position3D);
            View = TankGame.GameView;
            Projection = TankGame.GameProjection;
        }

        public void Remove()
        {
            Explosions[_id] = null;
        }

        public void Render()
        {
            foreach (ModelMesh mesh in Model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
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
                        effect.Alpha -= 0.05f;
                    else
                        effect.Alpha = 1f;
                }
                mesh.Draw();
            }
        }
    }
}
