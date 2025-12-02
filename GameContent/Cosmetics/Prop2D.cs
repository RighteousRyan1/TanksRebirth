using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using TanksRebirth.GameContent.Systems.TankSystem;

namespace TanksRebirth.GameContent.Cosmetics;

public class Prop2D(string name, Texture2D texture, Vector3 position, PropLockOptions lockOptions) : IProp
{
    /// <summary>The position of the cosmetic, relative to the tank's position.</summary>
    public Vector3 RelativePosition { get; set; } = position;
    /// <summary>The texture of this <see cref="Prop2D"/>.</summary>
    public Texture2D Texture { get; set; } = texture;
    /// <summary>The name of this <see cref="Prop2D"/>.</summary>
    public string Name { get; set; } = name;
    /// <summary>Whether or not this <see cref="Prop2D"/> rotates with the tank's turret.</summary>
    public PropLockOptions LockOptions { get; set; } = lockOptions;
    /// <summary>The rotation of this <see cref="Prop2D"/></summary>
    public Vector3 Rotation { get; set; } = Vector3.Zero;
    /// <summary>Change the properties of this <see cref="Prop2D"/> every game tick.</summary>
    public Action<IProp, Tank> UniqueBehavior { get; set; } = null;
    /// <summary>The rotation of this <see cref="Prop3D"/>.</summary>
    public Vector3 Scale { get; set; } = Vector3.One;
}
