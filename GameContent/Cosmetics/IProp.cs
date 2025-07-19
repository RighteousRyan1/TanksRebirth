using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using TanksRebirth.GameContent.Systems.TankSystem;

namespace TanksRebirth.GameContent.Cosmetics;
public enum PropLockOptions {
    None,
    ToTank,
    ToTurret,
    /// <summary>Will Rotate around turret instead of rotating with turret.</summary>
    ToTurretCentered
};
public interface IProp {
    Vector3 RelativePosition { get; set; }
    Vector3 Rotation { get; set; }
    string Name { get; set; }
    PropLockOptions LockOptions { get; set; }
    Action<IProp, Tank> UniqueBehavior { get; set; }
    Vector3 Scale { get; set; }
}
