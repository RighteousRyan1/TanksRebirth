using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WiiPlayTanksRemake.Enums;
using WiiPlayTanksRemake.Internals.Core.Interfaces;

namespace WiiPlayTanksRemake.GameContent
{
    public abstract class Tank
    {
        /// <summary>This <see cref="Tank"/>'s model.</summary>
        public Model Model { get; set; }
        /// <summary>This <see cref="Tank"/>'s world position. Used to change the actual location of the model relative to the <see cref="View"/> and <see cref="Projection"/>.</summary>
        public Matrix World { get; set; }
        /// <summary>How the <see cref="Model"/> is viewed through the <see cref="Projection"/>.</summary>
        public Matrix View { get; set; }
        /// <summary>The projection from the screen to the <see cref="Model"/>.</summary>
        public Matrix Projection { get; set; }
        /// <summary>Whether the tank has been destroyed or not.</summary>
        public bool Dead { get; set; }
        /// <summary>Whether or not the tank should become invisible at mission start.</summary>
        public bool Invisible { get; set; }
        /// <summary>How fast the tank should accelerate towards it's <see cref="MaxSpeed"/>.</summary>
        public float Acceleration { get; set; } = 1f;
        /// <summary>The maximum speed this tank can achieve.</summary>
        public float MaxSpeed { get; set; } = 1f;
        /// <summary>How fast the bullets this <see cref="Tank"/> shoot are.</summary>
        public float BulletShootSpeed { get; set; } = 1f;
        /// <summary>The rotation of this <see cref="Tank"/>'s barrel. Generally should not be modified in a player context.</summary>
        public float BarrelRotation { get; set; }
        /// <summary>The rotation of this <see cref="Tank"/>.</summary>
        public float TankRotation { get; set; }
        /// <summary>The pitch of the footprint placement sounds.</summary>
        public float TreadPitch { get; set; }
        /// <summary>The pitch of the shoot sound.</summary>
        public float ShootPitch { get; set; }
        /// <summary>The type of bullet this <see cref="Tank"/> shoots.</summary>
        public BulletType BulletType { get; set; } = BulletType.Regular;
        /// <summary>The maximum amount of mines this <see cref="Tank"/> can place.</summary>
        public int MineLimit { get; set; }

        public Vector3 position, oldPosition, velocity;

        public int shootStun, mineLayStun, maxBullets, ricochetCount; // ticks!


        public float maxTurnUntilPivot; // radians!!!!
        public float pivotSpeed = 1f; // radians per tick!!!
    }

    public class TankFootprint
    {
        public const int MAX_FOOTPRINTS = 100000;

        public static TankFootprint[] footprints = new TankFootprint[MAX_FOOTPRINTS];

        public Vector3 location;
        public float rotation;

        private static int total_treads_placed;

        public void Render()
        {
            total_treads_placed++;
        }
    }
}