using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using WiiPlayTanksRemake.Internals.Common.Utilities;
using WiiPlayTanksRemake.Internals;
using Microsoft.Xna.Framework.Audio;
using WiiPlayTanksRemake.Internals.Common.Framework.Audio;
using WiiPlayTanksRemake.Graphics;

namespace WiiPlayTanksRemake.GameContent
{
    public class CrateDrop
    {
        public const int MAX_CRATES = 50;

        public static CrateDrop[] crates = new CrateDrop[MAX_CRATES];

        public Vector3 position;

        public Vector3 velocity;

        public float gravity;

        public float dropSpeedAccel = 0.05f;

        public float scale = 1f;

        public Model Model;

        public Matrix[] faceWorlds = new Matrix[6];

        public bool IsOpening { get; private set; }

        public int id;

        public Tank TankToSpawn;


        public float fadeScale = 0.05f;

        private int _bounceCount;

        private int _maxBounces = 2;

        private CrateDrop() 
        {
            Model = GameResources.GetGameResource<Model>("Assets/BoxFace");

            int index = Array.IndexOf(crates, crates.First(c => c is null));

            id = index;

            crates[index] = this;
        }

        public static CrateDrop SpawnCrate(Vector3 pos, float gravity)
        {
            var spawnSfx = GameResources.GetGameResource<SoundEffect>("Assets/sounds/crate/CrateSpawn");

            SoundPlayer.PlaySoundInstance(spawnSfx, SoundContext.Effect, 0.2f);

            return new()
            {
                position = pos,
                gravity = gravity,
            };
        }

        public void Render()
        {
            // face order: right, left, front, back, top, bottom


            var cubeOffset = 9.6f * scale;

            var rotationMtxX = Matrix.CreateRotationX(MathHelper.PiOver2);
            var rotationMtxZ = Matrix.CreateRotationZ(MathHelper.PiOver4);

            var scaleMtx = Matrix.CreateScale(scale, 11 * scale, 0.1f);

            faceWorlds[0] = scaleMtx * rotationMtxZ * rotationMtxX * Matrix.CreateRotationZ(MathHelper.PiOver2) * Matrix.CreateTranslation(position.X + cubeOffset, position.Y, position.Z);
            faceWorlds[1] = scaleMtx * rotationMtxZ * rotationMtxX * Matrix.CreateRotationZ(MathHelper.PiOver2) * Matrix.CreateTranslation(position.X - cubeOffset, position.Y, position.Z);

            faceWorlds[2] = scaleMtx * rotationMtxZ * Matrix.CreateTranslation(position.X, position.Y, position.Z - cubeOffset);
            faceWorlds[3] = scaleMtx * rotationMtxZ * Matrix.CreateTranslation(position.X, position.Y, position.Z + cubeOffset);

            faceWorlds[4] = scaleMtx * rotationMtxZ * Matrix.CreateRotationX(MathHelper.PiOver2) * Matrix.CreateTranslation(position.X, position.Y + cubeOffset, position.Z);
            faceWorlds[5] = scaleMtx * rotationMtxZ * Matrix.CreateRotationX(MathHelper.PiOver2) * Matrix.CreateTranslation(position.X, position.Y - cubeOffset, position.Z);

            for (int i = 0; i < faceWorlds.Length; i++)
            {
                foreach (ModelMesh mesh in Model.Meshes)
                {
                    foreach (BasicEffect effect in mesh.Effects)
                    {
                        effect.World = faceWorlds[i];
                        effect.View = TankGame.GameView;
                        effect.Projection = TankGame.GameProjection;

                        effect.SetDefaultGameLighting_IngameEntities();

                        effect.TextureEnabled = true;

                        effect.Texture = GameResources.GetGameResource<Texture2D>("Assets/Textures/ingame/block_other_c");

                        //if (IsOpening)
                            //effect.Alpha -= fadeScale;
                    }

                    mesh.Draw();
                }
            }
        }

        public void Update()
        {
            if (!IsOpening)
            {
                velocity.Y -= gravity * 0.05f;

                // dropSpeed += dropSpeedAccel;

                position += velocity;

                if (position.Y <= (9.6f * scale))
                {
                    if (velocity.Y <= -1f)
                    {
                        var spawnSfx = GameResources.GetGameResource<SoundEffect>("Assets/sounds/crate/CrateImpact");

                        SoundPlayer.PlaySoundInstance(spawnSfx, SoundContext.Effect, 0.2f);

                        velocity.Y = -velocity.Y * 0.5f;

                        _bounceCount++;
                    }

                    if (_bounceCount > _maxBounces)
                        Open();
                }
            }

            else
            {
                scale -= fadeScale;

                if (scale <= 0)
                    crates[id] = null;
            }
            if (position.Y < 0)
                position.Y = 0;

        }

        public void Open()
        {
            IsOpening = true;

            if (TankToSpawn is not null)
            {
                TankToSpawn.Dead = false;
                TankToSpawn.position = new(position.X, 0, position.Z);
            }
        }
    }
}
