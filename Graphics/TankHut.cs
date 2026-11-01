using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using TanksRebirth.GameContent;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Globals.Assets;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Graphics.DynamicLighting;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.Graphics; 
public class TankHut {
    public Model HutScene;
    public ModelMesh Black;
    public Vector3 Position;

    float _minDist, _maxDist;
    Vector3 LightOffset => new Vector3(0, 85, 0) * Scale;
    Vector3 LightPosition => Position + LightOffset;
    public float Scale = 1f;
    public EulerAngles Rotation; // should lowkey convert everything to eulerangles
    public Matrix World, View, Proj;

    readonly OggAudio _lightAudio;
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

        _lightAudio = new OggAudio("Content/Assets/sounds/ambient/light_loop.ogg");
        _lightAudio.Instance.IsLooped = true;

        _lightAudio.MaxVolume = 1f;
        _minDist = _lightAudio.MinDistance = 50;
        _maxDist = _lightAudio.MaxDistance = 70;
        _lightAudio.OrthographicFade = true;

        _localBounds = HutScene.Meshes[0].BoundingSphere.Transform(_boneTransforms[HutScene.Meshes[0].ParentBone.Index]);
        foreach (var mesh in HutScene.Meshes)
            _localBounds = BoundingSphere.CreateMerged(_localBounds, mesh.BoundingSphere.Transform(_boneTransforms[mesh.ParentBone.Index]));

        LightingSystem.CollectLights += PlaceLights;
        SetTextures();
    }
    ~TankHut() {
        LightingSystem.CollectLights -= PlaceLights;
        _lightAudio?.Stop();
    }

    // add sum lights (but remove them when they can't be seen)
    void PlaceLights() {
        if (!_visible) return;

        var dir = -Vector3.UnitY;

        var baseIntensity = 10f;
        var baseColor = Color.Goldenrod;

        var spot1 = new SpotLight(LightPosition, dir, baseColor, baseIntensity, 200f,
            40f, 60f,
            true) {
            ShadowBias = 0.75f
        };
        var t = RuntimeData.RunTime;
        spot1.Intensity = baseIntensity + MathF.Sin(t * 0.5f) * 0.05f * baseIntensity;

        LightingSystem.AddFrameLight(spot1);
    }

    void SetTextures() {
        foreach (var mesh in HutScene.Meshes)
            _textures[mesh] = mesh == Black ? TextureGlobals.Pixels[Color.Black] : GameResources.GetGameResource<Texture2D>(GetMeshTexture(mesh));
    }

    public void Update() {
        _lightAudio.Position = Position + LightOffset * 0.25f;
        _lightAudio.Volume = TankGame.Settings.Audio.AmbientVolume;

        // only keep a voice playing while the buzz could actually be heard
        var audible = SoundUtils.GetSpatialParams(_lightAudio.Position.Value, _lightAudio.MinDistance * Scale, _lightAudio.MaxDistance * Scale, true).Volume > 0f;
        _lightAudio.MinDistance = _minDist * Scale;
        _lightAudio.MaxDistance = _maxDist * Scale;

        if (audible && !_lightAudio.IsPlaying())
            _lightAudio.Play();
        else if (!audible && _lightAudio.IsPlaying())
            _lightAudio.Stop();
    }

    public void Render() {
        World = Matrix.CreateScale(Scale)
            * Matrix.CreateFromYawPitchRoll(Rotation.Yaw, Rotation.Pitch, Rotation.Roll)
            * Matrix.CreateTranslation(Position);

        _visible = CameraGlobals.ViewFrustum.Intersects(_localBounds.Transform(World));
        if (!_visible) return;

        // DrawUtils.DrawBoundingSphere(new(_lightAudio.Position.Value, _lightAudio.MinDistance), Color.Red * 0.25f, View, Proj);
        // DrawUtils.DrawBoundingSphere(new(_lightAudio.Position.Value, _lightAudio.MaxDistance), Color.Lime * 0.25f, View, Proj);

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
