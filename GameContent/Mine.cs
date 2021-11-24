using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using WiiPlayTanksRemake.Internals;

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

        public Mine()
        {
            int index = Array.IndexOf(AllMines, AllMines.First(bullet => bullet is null));

            worldId = index;

            AllMines[index] = this;
        }

        public void Detonate()
        {
            var destroysound = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_destroy");
            AllMines[worldId] = null;
        }

        internal void Update()
        {
            
        }

        internal void Draw()
        {
        
        }
    }
}