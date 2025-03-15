using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using TanksRebirth.GameContent.Globals.Assets;
using TanksRebirth.Graphics;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.UI;

public class RebirthLogoModel {
    // normal human being stuff...
    public Vector3 Position;
    public Vector3 Rotation;
    public Vector3 Rotation_Tanks;
    public Vector3 Rotation_Rebirth;

    public float Scale;

    private int _animNum;

    public Matrix World { get; private set; }
    public Matrix View { get; set; }
    public Matrix Projection { get; set; }

    #region Model Parts/Textures

    // '__t' indicates "top" for a top layer
    // '__m' indicates "middle" for a middle layer
    // '__b' indicates "bottom" for a bottom layer

    // semantics:
    // word_letter (i.e: Tanks_T)
    // word_letter_layer (i.e: Rebirth_R__t)

    private readonly Model _model;
    private readonly ModelMesh Tanks_T;
    private readonly ModelMesh Tanks_a;
    private readonly ModelMesh Tanks_n;
    private readonly ModelMesh Tanks_k;
    private readonly ModelMesh Tanks_s;

    private readonly ModelMesh Graphic__t;
    private readonly ModelMesh Graphic__m;
    private readonly ModelMesh Graphic__b;

    private readonly ModelMesh Rebirth_R__t;
    private readonly ModelMesh Rebirth_R__m;
    private readonly ModelMesh Rebirth_R__b;

    private readonly ModelMesh Rebirth_e__t;
    private readonly ModelMesh Rebirth_e__m;
    private readonly ModelMesh Rebirth_e__b;

    private readonly ModelMesh Rebirth_b__t;
    private readonly ModelMesh Rebirth_b__m;
    private readonly ModelMesh Rebirth_b__b;

    private readonly ModelMesh Rebirth_i__t;
    private readonly ModelMesh Rebirth_i__m;
    private readonly ModelMesh Rebirth_i__b;

    private readonly ModelMesh Rebirth_r__t;
    private readonly ModelMesh Rebirth_r__m;
    private readonly ModelMesh Rebirth_r__b;

    private readonly ModelMesh Rebirth_t__t;
    private readonly ModelMesh Rebirth_t__m;
    private readonly ModelMesh Rebirth_t__b;

    private readonly ModelMesh Rebirth_h__t;
    private readonly ModelMesh Rebirth_h__m;
    private readonly ModelMesh Rebirth_h__b;

    // we modify this one.
    private Texture2D _gradient_t;
    private readonly Texture2D _gradient_mb;
    private readonly Texture2D _texture_tanks;

    private readonly Texture2D _texture_tnk_t;
    private readonly Texture2D _texture_tnk_mb;

    private readonly Matrix[] _boneTransforms;
    #endregion

    public RebirthLogoModel() {
        // mesh.ParentBone.Parent would be the word bone for a mesh 

        _model = ModelResources.Logo.Asset;
        Tanks_T = _model.Meshes["TANKS1"];
        Tanks_a = _model.Meshes["TANKS2"];
        Tanks_n = _model.Meshes["TANKS3"];
        Tanks_k = _model.Meshes["TANKS4"];
        Tanks_s = _model.Meshes["TANKS5"];

        Graphic__t = _model.Meshes["ICON_t"];
        Graphic__m = _model.Meshes["ICON_m"];
        Graphic__b = _model.Meshes["ICON_b"];

        Rebirth_R__t = _model.Meshes["REBIRTH1_t"];
        Rebirth_R__m = _model.Meshes["REBIRTH1_m"];
        Rebirth_R__b = _model.Meshes["REBIRTH1_b"];

        Rebirth_e__t = _model.Meshes["REBIRTH2_t"];
        Rebirth_e__m = _model.Meshes["REBIRTH2_m"];
        Rebirth_e__b = _model.Meshes["REBIRTH2_b"];

        Rebirth_b__t = _model.Meshes["REBIRTH3_t"];
        Rebirth_b__m = _model.Meshes["REBIRTH3_m"];
        Rebirth_b__b = _model.Meshes["REBIRTH3_b"];

        Rebirth_i__t = _model.Meshes["REBIRTH4_t"];
        Rebirth_i__m = _model.Meshes["REBIRTH4_m"];
        Rebirth_i__b = _model.Meshes["REBIRTH4_b"];

        Rebirth_r__t = _model.Meshes["REBIRTH5_t"];
        Rebirth_r__m = _model.Meshes["REBIRTH5_m"];
        Rebirth_r__b = _model.Meshes["REBIRTH5_b"];

        Rebirth_t__t = _model.Meshes["REBIRTH6_t"];
        Rebirth_t__m = _model.Meshes["REBIRTH6_m"];
        Rebirth_t__b = _model.Meshes["REBIRTH6_b"];

        Rebirth_h__t = _model.Meshes["REBIRTH7_t"];
        Rebirth_h__m = _model.Meshes["REBIRTH7_m"];
        Rebirth_h__b = _model.Meshes["REBIRTH7_b"];

        _gradient_t = GameResources.GetGameResource<Texture2D>("Assets/models/logo/grad1");
        _gradient_mb = GameResources.GetGameResource<Texture2D>("Assets/models/logo/grad2");

        _texture_tanks = GameResources.GetGameResource<Texture2D>("Assets/models/logo/tanks");

        _texture_tnk_t = GameResources.GetGameResource<Texture2D>("Assets/models/logo/grad3");
        _texture_tnk_mb = GameResources.GetGameResource<Texture2D>("Assets/models/logo/tank");

        _boneTransforms = new Matrix[_model.Bones.Count];
    }

    public void Render() {
        if (TankGame.RunTime % 2 <= TankGame.DeltaTime) {
            _gradient_t = GameResources.GetGameResource<Texture2D>($"Assets/models/logo/anim/anim_{_animNum:0000}");
            _animNum++;
            if (_animNum > 159) _animNum = 0;
        }

        World = Matrix.CreateScale(Scale)
            * Matrix.CreateFromYawPitchRoll(Rotation.Z, Rotation.Y, Rotation.X)
            * Matrix.CreateTranslation(Position);

        //Graphic__t.ParentBone.Parent.Transform = Matrix.CreateFromYawPitchRoll(Rotation.Z, Rotation.Y, Rotation.X) * Matrix.CreateFromYawPitchRoll(Rotation_Tanks.Z, Rotation_Tanks.Y, Rotation_Tanks.X);
        //Rebirth_R__t.ParentBone.Parent.Transform = Matrix.CreateFromYawPitchRoll(Rotation.Z, Rotation.Y, Rotation.X) * Matrix.CreateFromYawPitchRoll(Rotation_Tanks.Z, Rotation_Tanks.Y, Rotation_Tanks.X);

        _model.CopyAbsoluteBoneTransformsTo(_boneTransforms);
        _model.Root.Transform = World;

        var lDir = Vector3.Forward;

        foreach (var mesh in _model.Meshes) {
            foreach (BasicEffect effect in mesh.Effects) {
                effect.World = _boneTransforms[mesh.ParentBone.Index];
                effect.View = View;
                effect.Projection = Projection;

                effect.TextureEnabled = true;

                if (!mesh.Name.Contains("ICON")) {
                    switch (mesh.Name) {
                        case var _ when mesh.Name.EndsWith("_t"):
                            effect.Texture = _gradient_t;
                            effect.SetDefaultGameLighting_IngameEntities(1f, lightDir: lDir);
                            break;
                        case var _ when mesh.Name.EndsWith("_m"):
                            effect.Texture = _gradient_mb;
                            effect.SetDefaultGameLighting_IngameEntities(0.6f, lightDir: lDir);
                            break;
                        case var _ when mesh.Name.EndsWith("_b"):
                            effect.Texture = _gradient_mb;
                            effect.SetDefaultGameLighting_IngameEntities(0.8f, lightDir: lDir);
                            break;
                    }
                } else {
                    switch (mesh.Name) {
                        case "ICON_t":
                            effect.Texture = _texture_tnk_t;
                            effect.SetDefaultGameLighting_IngameEntities(1f, lightDir: lDir);
                            break;
                        case "ICON_m":
                            effect.Texture = _texture_tnk_mb;
                            effect.SetDefaultGameLighting_IngameEntities(0.8f, lightDir: lDir);
                            break;
                        case "ICON_b":
                            effect.Texture = _texture_tnk_mb;
                            effect.SetDefaultGameLighting_IngameEntities(0.6f, lightDir: lDir);
                            break;
                    }
                }
                if (mesh.Name.Contains("TANKS")) {
                    effect.Texture = _texture_tanks;
                    effect.SetDefaultGameLighting_IngameEntities(1f, lightDir: lDir);
                }
                effect.AmbientLightColor = Color.White.ToVector3() * 0.25f;
                effect.Alpha = 1f;
            }
            mesh.Draw();
        }
    }
}
