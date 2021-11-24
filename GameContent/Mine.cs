using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using WiiPlayTanksRemake.Internals;
using WiiPlayTanksRemake.Internals.Common.Utilities;

namespace WiiPlayTanksRemake.GameContent
{
    public sealed class Mine
    {
        private static int maxMines = 200;
        public static Mine[] AllMines { get; } = new Mine[maxMines];

        public Tank owner;

        public Vector3 position;

        public Matrix View;
        public Matrix Projection;
        public Matrix World;

        public Model Model;

        public static Texture2D _mineTexture;

        private int worldId;

        public Mine(Vector3 pos)
        {
            position = pos;

            // _mineTexture

            int index = Array.IndexOf(AllMines, AllMines.First(bullet => bullet is null));

            worldId = index;

            AllMines[index] = this;
        }

        public void Detonate()
        {
            var destroysound = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_destroy");

            SoundPlayer.PlaySoundInstance(destroysound, SoundContext.Sound, 0.4f);

            AllMines[worldId] = null;
        }

        internal void Update()
        {
            World = Matrix.CreateTranslation(position);
            View = TankGame.GameView;
            Projection = TankGame.GameProjection;


        }

        internal void Draw()
        {
            foreach (ModelMesh mesh in Model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = World;
                    effect.View = View;
                    effect.Projection = Projection;
                    effect.TextureEnabled = true;

                    effect.Texture = _mineTexture;

                    effect.EnableDefaultLighting();
                }
                mesh.Draw();
            }
        }
    }
}