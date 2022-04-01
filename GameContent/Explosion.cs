using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using WiiPlayTanksRemake.Graphics;
using WiiPlayTanksRemake.Internals;
using WiiPlayTanksRemake.Internals.Common.Framework.Audio;
using WiiPlayTanksRemake.Internals.Common.Utilities;

namespace WiiPlayTanksRemake.GameContent
{
    public class Explosion
    {
        // model, blah blah blah

        public const int MINE_EXPLOSIONS_MAX = 500;

        public static Explosion[] explosions = new Explosion[MINE_EXPLOSIONS_MAX];

        public Vector2 Position;

        public bool[] HasHit = new bool[GameHandler.AllTanks.Length];

        public Vector3 Position3D => Position.ExpandZ();

        public Matrix View;
        public Matrix Projection;
        public Matrix World;

        public Model Model;

        public static Texture2D mask;

        public float scale;

        public float maxScale;

        public float expanseRate = 1f;
        public float shrinkRate = 1f;

        public int tickAtMax = 40;

        private bool hitMaxAlready;

        private int id;

        public float rotation;

        public float rotationSpeed;

        public Explosion(Vector2 pos, float scaleMax, float rotationSpeed = 1f)
        {
            this.rotationSpeed = rotationSpeed;
            Position = pos;
            maxScale = scaleMax;
            mask = GameResources.GetGameResource<Texture2D>(/*"Assets/textures/mine/explosion_mask"*/"Assets/textures/misc/tank_smoke_ami");

            Model = GameResources.GetGameResource<Model>("Assets/mineexplosion");

            int index = Array.IndexOf(explosions, explosions.First(t => t is null));

            var destroysound = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_destroy");

            SoundPlayer.PlaySoundInstance(destroysound, SoundContext.Effect, 0.4f);

            id = index;

            explosions[index] = this;
        }

        public void Update()
        {
            if (!hitMaxAlready)
            {
                if (scale < maxScale)
                    scale += expanseRate;

                if (scale > maxScale)
                    scale = maxScale;

                if (scale >= maxScale)
                    hitMaxAlready = true;
            }
            else if (tickAtMax <= 0)
                scale -= shrinkRate;

            if (!GameHandler.IsAwaitingNewMission)
            {
                foreach (var mine in Mine.AllMines)
                {
                    if (mine is not null && Vector2.Distance(mine.Position, Position) <= scale * 9) // magick
                        mine.Detonate();
                }
                foreach (var cube in Block.AllBlocks)
                {
                    if (cube is not null && Vector2.Distance(cube.Position, Position) <= scale * 9 && cube.IsDestructible)
                        cube.Destroy();
                }
                foreach (var shell in Shell.AllShells)
                {
                    if (shell is not null && Vector2.Distance(shell.Position2D, Position) < scale * 9)
                        shell.Destroy();
                }
                foreach (var tank in GameHandler.AllTanks)
                {
                    if (tank is not null && Vector2.Distance(tank.Position, Position) < scale * 9)
                        if (!tank.Dead)
                            if (!HasHit[tank.WorldId])
                                if (tank.VulnerableToMines)
                                {
                                    HasHit[tank.WorldId] = true;
                                    tank.Damage();
                                }
                }
            }

            if (hitMaxAlready)
                tickAtMax--;

            if (scale <= 0)
                Remove();

            rotation += rotationSpeed;

            World = Matrix.CreateScale(scale) * Matrix.CreateRotationY(rotation) * Matrix.CreateTranslation(Position3D);
            View = TankGame.GameView;
            Projection = TankGame.GameProjection;
        }

        public void Remove()
        {
            explosions[id] = null;
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

                    effect.Texture = mask;

                    effect.SetDefaultGameLighting_IngameEntities();

                    effect.AmbientLightColor = Color.Orange.ToVector3();
                    effect.DiffuseColor = Color.Orange.ToVector3();
                    effect.EmissiveColor = Color.Orange.ToVector3();
                    effect.FogColor = Color.Orange.ToVector3();

                    if (tickAtMax <= 0)
                        effect.Alpha -= 0.05f;
                    else
                        effect.Alpha = 1f;
                }
                mesh.Draw();
            }
        }
    }
}
