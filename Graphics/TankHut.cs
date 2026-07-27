using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using tainicom.Aether.Physics2D.Dynamics;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Globals.Assets;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.Graphics; 
public class TankHut {
    public Model HutScene;
    public ModelMesh Black;

    public Vector3 Position;
    public float Scale;
    public EulerAngles Rotation;

    public Matrix View, Proj;

    static readonly string roof = PathGlobals.TEXTURES_PATH + "ingame/block_other_c";

    static readonly Dictionary<string, string> MeshToTexture = new() {
        // special treatment, Pixels[Black]
        // ["Black"] = "",
        ["Coat Rack"] = PathGlobals.SKYBOX_PATH + "textures/metal",
        ["Doorway"] = roof,
        ["Floor"] = PathGlobals.TEXTURES_PATH + "plane/wings",
        ["Frame"] = PathGlobals.SCENE_PATH + "hut/frame",
        ["Painting"] = PathGlobals.SCENE_PATH + "hut/p2", // could turn into random (golden freddy jumpscare
        ["Roof"] = roof,
        ["Rug"] = PathGlobals.SKYBOX_PATH + "textures/curtains",
        ["Shelves"] = roof,
        ["Walls"] = PathGlobals.TEXTURES_PATH + "ingame/block_other_b_test",
        ["Window"] = PathGlobals.SCENE_PATH + "hut/frame",
    };

    public TankHut() {
        HutScene = ModelGlobals.Hut.Asset;
        Black = HutScene.Meshes["Black"];
        SetTextures();
    }

    void SetTextures() {
        // HutScene.
    }

    public void Draw() {

    }

    public static string GetMeshTexture(ModelMesh mesh) {
        return MeshToTexture.TryGetValue(mesh.Name, out var textureName)
            ? textureName
            : "metal"; // fallback default
    }

    // no!!!
    void MeshEffectSet(ModelMesh mesh, BasicEffect effect) {
        effect.World = Matrix.CreateScale(Scale)
            * Matrix.CreateFromYawPitchRoll(Rotation.Yaw, Rotation.Pitch, Rotation.Roll)
            * Matrix.CreateTranslation(Position);
        effect.View = View;
        effect.Projection = Proj;

        effect.TextureEnabled = true;
        if (!mesh.Name.Equals("Black"))
            effect.Texture = GameResources.GetGameResource<Texture2D>("Assets/models/scene/skybox/textures/" + GetMeshTexture(mesh));
        else
            effect.Texture = TextureGlobals.Pixels[Color.Black];
        effect.SetDefaultGameLighting();
        effect.SpecularPower = 32f;
    }
}
