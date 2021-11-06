using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WiiPlayTanksRemake.Enums;
using WiiPlayTanksRemake.Internals.Core.Interfaces;

namespace WiiPlayTanksRemake.GameContent
{
    public abstract class Tank
    {
        public Model Model { get; set; }
        public Matrix World { get; set; }
        public Matrix View { get; set; }
        public Matrix Projection { get; set; }

        public bool Dead { get; set; }
        public float Speed { get; set; } = 1f;
        public float BulletShootSpeed { get; set; }
        public float BarrelRotation { get; set; }
        public float TankRotation { get; set; }
        public float TreadPitch { get; set; }
        public float ShootPitch { get; set; }
        public BulletType BulletType { get; set; } = BulletType.Regular;
        public int MaxLayableMines { get; set; }

        public Vector3 position;
        public Vector3 oldPosition;
        public Vector3 approachVelocity;
        public Vector3 velocity;
    }
}