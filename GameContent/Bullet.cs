using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace WiiPlayTanksRemake.GameContent
{
    public class Bullet
    {
        public static List<Bullet> AllBullets { get; } = new();
        public Vector3 position;
        public Vector3 velocity;
        public int ricochets;
        public PlayerTank owner;

        public Matrix View;
        public Matrix Projection;
        public Matrix World;

        public Model BulletModel;

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
