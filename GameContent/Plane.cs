using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using tainicom.Aether.Physics2D.Dynamics;
using TanksRebirth.GameContent.ID;
using TanksRebirth.Graphics;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent; 

// TODO: plane sounds, explosive (bomb, missile?) 
// potentially... plane can crash? into each other? into the ground?
public class Plane {
    public const int MAX_PLANES = 5;
    public static Plane[] AllPlanes = new Plane[MAX_PLANES];

    public readonly int Id;
    public float LifeSpan;
    public float LifeTime;
    public Vector3 Position { get; set; }
    public Vector3 Velocity { get; set; }
    public EulerAngles Rotation;

    public Matrix World;
    public Matrix View;
    public Matrix Projection;

    public Texture2D BodyTexture;
    public Texture2D WingTexture;
    public Texture2D InteriorTexture;
    public Model Model { get; set; }
    public ModelBone TrapDoorL { get; }
    public ModelBone TrapDoorR { get; }
    public ModelBone PropellerL { get; }
    public ModelBone PropellerR { get; }

    private Matrix[] _boneTransforms;
    public Plane(Vector3 position, Vector3 velocity, EulerAngles rotation, float lifeSpan) {
        Position = position;
        Velocity = velocity;
        Rotation = rotation;

        LifeSpan = lifeSpan;

        Model = GameResources.GetGameResource<Model>("Assets/plane");
        _boneTransforms = new Matrix[Model.Bones.Count];
        TrapDoorL = Model.Bones["Door1"];
        TrapDoorR = Model.Bones["Door2"];
        PropellerL = Model.Bones["Prop1"];
        PropellerR = Model.Bones["Prop2"];

        BodyTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/plane/body");
        WingTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/plane/wings");
        InteriorTexture = TankGame.BlackPixel;

        Id = Array.FindIndex(AllPlanes, plane => plane is null);

        AllPlanes[Id] = this;
    }

    public void Remove() {
        AllPlanes[Id] = null;
    }

    public void Update() {
        LifeTime += TankGame.DeltaTime;

        Model.CopyAbsoluteBoneTransformsTo(_boneTransforms);

        var bones = Model.Bones;
        var meshes = Model.Meshes;

        Position += Velocity;

        // TODO: once bk fixes mesh/bone problem, make this work. find mesh via Meshes["Name"] and then transform mesh.ParentBone.Transform
        PropellerR.Transform = Matrix.CreateRotationY(LifeTime); 
        PropellerL.Transform = PropellerR.Transform;
        Model!.Root.Transform = World;

        if (LifeTime > LifeSpan) Remove();

        View = TankGame.GameView;
        Projection = TankGame.GameProjection;
    }
    public void Render() {
        if (!MapRenderer.ShouldRenderAll)
            return;
        World = Matrix.CreateScale(0.6f) * Matrix.CreateFromYawPitchRoll(Rotation.Yaw, Rotation.Pitch, Rotation.Roll) * Matrix.CreateTranslation(Position);    
        Projection = TankGame.GameProjection;
        View = TankGame.GameView;

        for (int i = 0; i < (Lighting.AccurateShadows ? 2 : 1); i++) {
            foreach (var mesh in Model.Meshes) {
                foreach (BasicEffect effect in mesh.Effects) {
                    effect.View = View;
                    effect.World = i == 0 ? _boneTransforms[mesh.ParentBone.Index] : _boneTransforms[mesh.ParentBone.Index] * Matrix.CreateShadow(Lighting.AccurateLightingDirection, new(Vector3.UnitY, 0))
                        * Matrix.CreateTranslation(0, 0.2f, 0);
                    effect.Projection = Projection;

                    effect.TextureEnabled = true;

                    switch (mesh.Name) {
                        // chassis/body exterior
                        case "Plane_Wood1":
                            effect.Texture = BodyTexture;
                            break;
                        // wings/propellers
                        case "Plane_Wood2":
                            effect.Texture = WingTexture;
                            break;
                        // interior... duh.
                        case "Plane_Interior":
                            effect.Texture = InteriorTexture;
                            break;
                    }


                    effect.SetDefaultGameLighting_IngameEntities(2f);

                    effect.Alpha = 1f;
                }
                mesh.Draw();
            }
        }
    }
}
