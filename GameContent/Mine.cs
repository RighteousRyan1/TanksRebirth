using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace WiiPlayTanksRemake.GameContent
{
    public sealed class Mine
    {
        public static List<Mine> AllMines { get; } = new();
        public Vector3 position;
        public int detonationTimer;
        public float explosionRadius;
        public PlayerTank owner;

        internal void Update()
        {
            
        }

        internal void Draw()
        {
        
        }
    }
}