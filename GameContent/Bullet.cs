using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace WiiPlayTanksRemake.GameContent
{
    public class Bullet
    {
        public static List<Bullet> AllBullets { get; } = new();
        public Vector3 position;
        public Vector3 velocity;
        public int ricochets;

        internal void Update()
        {
            position += velocity;
        }

        internal void Draw()
        {
            if (velocity.Length() > 20)
            {
                //draw as flaming bullet. optionally we could have
                //a property defining whether or not it draws as a flaming bullet
            }
        }
    }
}
