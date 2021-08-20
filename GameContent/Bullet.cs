using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace WiiPlayTanksRemake.GameContent
{
    public class Bullet
    {
        public static List<Bullet> AllBullets { get; } = new();
        public Vector2 position;
        public Vector2 velocity;
        public int ricochets;
        public Tank owner;

        public bool Flaming { get; set; }

        internal void Update()
        {
            position += velocity;
        }

        internal void Draw()
        {
            
        }
    }
}
