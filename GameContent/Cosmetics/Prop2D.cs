using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace TanksRebirth.GameContent.Cosmetics;

public class Prop2D : IProp
{
    /// <summary>The position of the cosmetic, relative to the tank's position.</summary>
    public Vector3 RelativePosition { get; set; }
    /// <summary>The texture of this <see cref="Prop2D"/>.</summary>
    public Texture2D Texture { get; set; }
    /// <summary>The name of this <see cref="Prop2D"/>.</summary>
    public string Name { get; set; }
    /// <summary>Whether or not this <see cref="Prop2D"/> rotates with the tank's turret.</summary>
    public PropLockOptions LockOptions { get; set; }
    /// <summary>The rotation of this <see cref="Prop2D"/></summary>
    public Vector3 Rotation { get; set; }
    /// <summary>Change the properties of this <see cref="Prop2D"/> every game tick.</summary>
    public Action<IProp, Tank> UniqueBehavior { get; set; } = null;
    /// <summary>The rotation of this <see cref="Prop3D"/>.</summary>
    public Vector3 Scale { get; set; } 
    public Prop2D(string name, Texture2D texture, Vector3 position, PropLockOptions lockOptions)
    {
        Name = name;
        Texture = texture;
        RelativePosition = position;
        Rotation = Vector3.Zero;
        Scale = Vector3.One;
        LockOptions = lockOptions;
    }
}
