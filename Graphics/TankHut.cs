using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Globals.Assets;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Graphics.DynamicLighting;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.Graphics; 
public class TankHut {
    public Model HutScene;
    public ModelMesh Black;

    public Vector3 Position;
    public float Scale = 1f;
    public EulerAngles Rotation; // should lowkey convert everything to eulerangles
    public Matrix World, View, Proj;

    bool _visible;
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

    readonly Matrix[] _boneTransforms;
    readonly Dictionary<ModelMesh, Texture2D> _textures = [];
    readonly BoundingSphere _localBounds;

    public TankHut() {
        HutScene = ModelGlobals.Hut.Asset;
        Black = HutScene.Meshes["Black"];

        _boneTransforms = new Matrix[HutScene.Bones.Count];
        HutScene.CopyAbsoluteBoneTransformsTo(_boneTransforms);

        _localBounds = HutScene.Meshes[0].BoundingSphere.Transform(_boneTransforms[HutScene.Meshes[0].ParentBone.Index]);
        foreach (var mesh in HutScene.Meshes)
            _localBounds = BoundingSphere.CreateMerged(_localBounds, mesh.BoundingSphere.Transform(_boneTransforms[mesh.ParentBone.Index]));

        LightingSystem.CollectLights += PlaceLights;
        SetTextures();
    }
    ~TankHut() {
        LightingSystem.CollectLights -= PlaceLights;
    }

    // add sum lights (but remove them when they can't be seen)
    void PlaceLights() {
        if (!_visible) return;

        var s1p = Position + new Vector3(0, 85, 0) * Scale;
        var s1d = -Vector3.UnitY;

        var baseIntensity = 10f;
        var baseColor = Color.Goldenrod;

        var spot1 = new SpotLight(s1p, s1d, baseColor, baseIntensity, 200f,
            40f, 60f,
            true) {
            ShadowBias = 1f
        };
        var t = RuntimeData.RunTime;
        spot1.Intensity = baseIntensity + MathF.Sin(t * 0.5f) * 0.05f * baseIntensity;

        LightingSystem.AddFrameLight(spot1);
    }

    void SetTextures() {
        foreach (var mesh in HutScene.Meshes)
            _textures[mesh] = mesh == Black ? TextureGlobals.Pixels[Color.Black] : GameResources.GetGameResource<Texture2D>(GetMeshTexture(mesh));
    }

    public void Render() {
        World = Matrix.CreateScale(Scale)
            * Matrix.CreateFromYawPitchRoll(Rotation.Yaw, Rotation.Pitch, Rotation.Roll)
            * Matrix.CreateTranslation(Position);

        _visible = CameraGlobals.ViewFrustum.Intersects(_localBounds.Transform(World));
        if (!_visible) return;

        TankGame.Instance.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
        foreach (var mesh in HutScene.Meshes) {
            foreach (BasicEffect effect in mesh.Effects)
                MeshEffectSet(mesh, effect, World);

            if (mesh != Black)
                mesh.Draw();
        }
    }

    public static string GetMeshTexture(ModelMesh mesh) {
        return MeshToTexture.TryGetValue(mesh.Name, out var textureName)
            ? textureName
            : PathGlobals.SKYBOX_PATH + "textures/metal"; // fallback default
    }

    // no!!!
    void MeshEffectSet(ModelMesh mesh, BasicEffect effect, Matrix world) {
        effect.World = _boneTransforms[mesh.ParentBone.Index] * world;
        effect.View = View;
        effect.Projection = Proj;

        effect.TextureEnabled = true;
        effect.Texture = _textures[mesh];
        effect.SetDefaultGameLighting();
        effect.SpecularPower = 32f;
    }
}
