using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace TanksRebirth.GameContent.Cosmetics
{
    public struct Cosmetic3D : ICosmetic
    {
        /// <summary>The position of the cosmetic, relative to the tank's position.</summary>
        public Vector3 RelativePosition { get; set; }
        /// <summary>The model of this <see cref="Cosmetic3D"/>.</summary>
        public Model Model { get; set; }
        /// <summary>The name of this <see cref="Cosmetic3D"/>.</summary>
        public string Name { get; set; }
        /// <summary>Whether or not this <see cref="Cosmetic3D"/> rotates with the tank's turret.</summary>
        public CosmeticLockOptions LockOptions { get; set; }
        /// <summary>The rotation of this <see cref="Cosmetic3D"/></summary>
        public Vector3 Rotation { get; set; }
        /// <summary>The texture applied to the model.</summary>
        public Texture2D ModelTexture { get; set; }
        /// <summary>Change the properties of this <see cref="Cosmetic3D"/> every game tick.</summary>
        public Action<ICosmetic, Tank> UniqueBehavior { get; set; } = null;
        /// <summary>The rotation of this <see cref="Cosmetic3D"/>.</summary>
        public Vector3 Scale { get; set; }

        public string[] IgnoreMeshesByName;

        public Cosmetic3D(string name, Model model, Texture2D texture, Vector3 position, CosmeticLockOptions lockOptions)
        {
            Name = name;
            Model = model;
            ModelTexture = texture;
            RelativePosition = position;
            Rotation = Vector3.Zero;
            Scale = Vector3.One;
            LockOptions = lockOptions;
            IgnoreMeshesByName = Array.Empty<string>();
        }
    }
}
