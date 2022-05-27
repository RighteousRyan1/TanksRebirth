using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;

namespace TanksRebirth.GameContent.Cosmetics
{
    public interface ICosmetic
    {
        Vector3 RelativePosition { get; set; }
        Vector3 Rotation { get; set; }
        string Name { get; set; }
        bool SnapToTurretAngle { get; set; }
        Action<ICosmetic, Tank> UniqueBehavior { get; set; }
        Vector3 Scale { get; set; }
    }
}
