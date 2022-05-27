using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace TanksRebirth.GameContent.Cosmetics
{
    public struct Cosmetic2D : ICosmetic
    {
        /// <summary>The position of the cosmetic, relative to the tank's position.</summary>
        public Vector3 RelativePosition { get; set; }
        /// <summary>The texture of this <see cref="Cosmetic2D"/>.</summary>
        public Texture2D Texture { get; set; }
        /// <summary>The name of this <see cref="Cosmetic2D"/>.</summary>
        public string Name { get; set; }
        /// <summary>Whether or not this <see cref="Cosmetic2D"/> rotates with the tank's turret.</summary>
        public bool SnapToTurretAngle { get; set; }
        /// <summary>The rotation of this <see cref="Cosmetic2D"/></summary>
        public Vector3 Rotation { get; set; }
        /// <summary>Change the properties of this <see cref="Cosmetic2D"/> every game tick.</summary>
        public Action<ICosmetic> UniqueBehavior { get; set; } = null;

        public Cosmetic2D(string name, Texture2D texture, Vector3 position, Vector3 rotation, bool snap)
        {
            Name = name;
            Texture = texture;
            RelativePosition = position;
            Rotation = rotation;
            SnapToTurretAngle = snap;
        }
    }
}
