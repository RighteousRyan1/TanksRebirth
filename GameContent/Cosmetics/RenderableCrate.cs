using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TanksRebirth.Internals;

namespace TanksRebirth.GameContent.Cosmetics
{
    public class RenderableCrate
    {
        public readonly Model Model;
        public readonly Texture2D Texture;
        public Vector3 ChestPosition;
        public Vector3 LidPosition;

        private Matrix _world;
        public Matrix View;
        public Matrix Projection;

        public Vector3 Rotation;

        public float Scale = 80f;

        public Vector3 LidRotation;

        public BoundingBox BoundingBox;

        public RenderableCrate(Vector3 position, Matrix view, Matrix proj)
        {
            Model = GameResources.GetGameResource<Model>("Assets/chest");
            Texture = GameResources.GetGameResource<Texture2D>("Assets/textures/chest/chest");

            _world = Matrix.CreateTranslation(position);

            ChestPosition = position;
            LidPosition = position;
            View = view;
            Projection = proj;
        }

        public void Render()
        {
            foreach (var mesh in Model.Meshes)
            {
                var rotmtx = mesh.Name == "Chest" ? Matrix.CreateFromYawPitchRoll(Rotation.Z, Rotation.Y, Rotation.X) : Matrix.CreateFromYawPitchRoll(LidRotation.Z, LidRotation.Y, LidRotation.X);
                _world = Matrix.CreateScale(Scale) * rotmtx * Matrix.CreateTranslation(mesh.Name == "Chest" ? ChestPosition : LidPosition);
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = _world;
                    effect.View = View;
                    effect.Projection = Projection;

                    effect.TextureEnabled = true;
                    effect.Texture = Texture;
                }
                mesh.Draw();
            }
        }
    }
}
