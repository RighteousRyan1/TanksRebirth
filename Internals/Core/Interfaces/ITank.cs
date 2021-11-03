using WiiPlayTanksRemake.Enums;

namespace WiiPlayTanksRemake.Internals.Core.Interfaces
{
    public interface ITank
    {
        bool Dead { get; set; }
        float Speed { get; set; }
        float BulletShootSpeed { get; set; }
        float BarrelRotation { get; set; }
        float TankRotation { get; set; }
        float TreadPitch { get; set; }
        float ShootPitch { get; set; }
        BulletType BulletType { get; set; }
        int MaxLayableMines { get; set; }

        void Destroy();
        void Shoot(float radians, float bulletSpeed);
    }
}