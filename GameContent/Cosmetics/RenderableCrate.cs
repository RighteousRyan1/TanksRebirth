using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TanksRebirth.Internals;

namespace TanksRebirth.GameContent.Cosmetics;

public class RenderableCrate
{
    public readonly Model Model;
    public readonly ModelMesh LidMesh;
    public readonly Texture2D Texture;
    public Vector3 ChestPosition;
    public Vector3 LidPosition;

    public Matrix World;
    public Matrix View;
    public Matrix Projection;

    public Vector3 Rotation;

    public float Scale = 1f; // instead of 80...?

    public Vector3 LidRotation;

    public BoundingBox BoundingBox;

    private Matrix[] _boneTransforms;

    public RenderableCrate(Vector3 position, Matrix view, Matrix proj)
    {
        Model = GameResources.GetGameResource<Model>("Assets/models/chest");
        Texture = GameResources.GetGameResource<Texture2D>("Assets/textures/chest/chest");

        LidMesh = Model.Meshes["Lid"];

        _boneTransforms = new Matrix[Model.Bones.Count];

        ChestPosition = position;
        LidPosition = position;
        View = view;
        Projection = proj;
    }

    public void Render()
    {
        /* Remember: mesh origins (+translations)
         * 
         */
        World = Matrix.CreateScale(Scale)
            * Matrix.CreateFromYawPitchRoll(Rotation.Z, Rotation.Y, Rotation.X)
            * Matrix.CreateTranslation(ChestPosition - new Vector3(0, 0, /*15.2424f*/0));
        LidMesh.ParentBone.Transform = Matrix.CreateScale(Scale)
            * Matrix.CreateFromYawPitchRoll(LidRotation.Z, LidRotation.Y, LidRotation.X)
            * Matrix.CreateTranslation(LidPosition);

        Model.CopyAbsoluteBoneTransformsTo(_boneTransforms);
        Model!.Root.Transform = World;

        /*for (int i = 0; i < Model.Bones.Count; i++) {
            var bone = Model.Bones[i];
            var parentTransform = bone.Parent != null ? _boneTransforms[bone.Parent.Index] : World;
            _boneTransforms[i] = _boneTransforms[bone.Index] * bone.Transform * parentTransform;
            // Console.WriteLine(bone.Transform.Translation);
        }*/
        foreach (var mesh in Model.Meshes) {
            foreach (BasicEffect effect in mesh.Effects) {
                effect.World = _boneTransforms[mesh.ParentBone.Index];
                effect.View = View;
                effect.Projection = Projection;

                effect.TextureEnabled = true;
                effect.Texture = Texture;
            }
            mesh.Draw();
        }
    }
}
