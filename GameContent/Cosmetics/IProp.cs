using System;
using Microsoft.Xna.Framework;
using TanksRebirth.GameContent.Systems.TankSystem;

namespace TanksRebirth.GameContent.Cosmetics;

public delegate void CosmeticUpdateCallback(IProp prop, Tank tank);
public enum PropLockOptions {
    None,
    /// <summary>Will rotate with the tank.</summary>
    ToTank,
    /// <summary>Will rotate with the turret.</summary>
    ToTurret,
    /// <summary>Will rotate around turret instead of rotating with turret.</summary>
    AroundTurret
    // AroundTank too?
};
public interface IProp : ICloneable {
    Vector3 RelativePosition { get; set; }
    Vector3 Rotation { get; set; }
    string Name { get; set; }
    PropLockOptions LockOptions { get; set; }
    CosmeticUpdateCallback UniqueBehavior { get; set; }
    Vector3 Scale { get; set; }
}
