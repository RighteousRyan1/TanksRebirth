using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TanksRebirth.GameContent.Globals.Assets;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.Graphics;
using TanksRebirth.Graphics.Drawing;
using TanksRebirth.Internals;

namespace TanksRebirth.GameContent.Cosmetics;

public class RenderableChest
{
    public readonly Model Model;
    public readonly ModelMesh LidMesh;
    public readonly Texture2D Texture;
    public Vector3 ChestPosition;
    public Vector3 LidPosition;

    public BasicDrawParams DrawParams = new();

    public Vector3 Rotation;

    public float Scale = 1f; // instead of 80...?

    public Vector3 LidRotation;

    public BoundingBox BoundingBox;

    readonly Matrix[] _boneTransforms;


    public Vector3 KeySlotPos { get; private set; }

    public RenderableChest(Vector3 position, Matrix view, Matrix proj)
    {
        Model = ModelGlobals.Chest.Asset;
        Texture = GameResources.GetGameResource<Texture2D>("Assets/textures/chest/chest");

        LidMesh = Model.Meshes["Lid"];

        _boneTransforms = new Matrix[Model.Bones.Count];

        ChestPosition = position;
        LidPosition = position;
        DrawParams.View = view;
        DrawParams.Projection = proj;
    }

    public void Render()
    {
        /* Remember: mesh origins (+translations)
         * 
         */
        var ypr = Matrix.CreateFromYawPitchRoll(Rotation.Z, Rotation.Y, Rotation.X);
        DrawParams.World = Matrix.CreateScale(Scale)
            * ypr
            * Matrix.CreateTranslation(ChestPosition - new Vector3(0, 0, /*15.2424f*/0));
        LidMesh.ParentBone.Transform = Matrix.CreateFromYawPitchRoll(LidRotation.Z, LidRotation.Y, LidRotation.X)
            * Matrix.CreateTranslation(LidPosition);

        Model.CopyAbsoluteBoneTransformsTo(_boneTransforms);
        Model!.Root.Transform = DrawParams.World;

        KeySlotPos = ChestPosition + Vector3.Transform(new Vector3(0, 30.6f, 20), ypr);

        /*for (int i = 0; i < Model.Bones.Count; i++) {
            var bone = Model.Bones[i];
            var parentTransform = bone.Parent != null ? _boneTransforms[bone.Parent.Index] : World;
            _boneTransforms[i] = _boneTransforms[bone.Index] * bone.Transform * parentTransform;
            // Console.WriteLine(bone.Transform.Translation);
        }*/
        foreach (var mesh in Model.Meshes) {
            foreach (BasicEffect effect in mesh.Effects) {
                effect.World = _boneTransforms[mesh.ParentBone.Index];
                effect.View = DrawParams.View;
                effect.Projection = DrawParams.Projection;

                effect.TextureEnabled = true;
                effect.Texture = Texture;

                effect.SetDefaultGameLighting();
            }
            mesh.Draw();
        }
    }
}
