using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using tainicom.Aether.Physics2D.Dynamics;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Globals.Assets;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.UI;
using TanksRebirth.Graphics;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent; 

// TODO: plane sounds (done), explosive (bomb, missile?) 
// potentially... plane can crash? into each other? into the ground?
public class Airplane {
    public const int MAX_PLANES = 30;
    public static Airplane[] AllPlanes = new Airplane[MAX_PLANES];

    // for modders.
    public delegate void TrapDoorsFullyOpened(Airplane plane);
    public delegate void TrapDoorsFullyClosed(Airplane plane);
    public delegate void TrapDoorsOpened(Airplane plane);
    public delegate void TrapDoorsClosed(Airplane plane);
    public static event TrapDoorsFullyOpened? OnTrapDoorsFullyOpenedEvent;
    public static event TrapDoorsFullyClosed? OnTrapDoorsFullyClosedEvent;
    public static event TrapDoorsOpened? WhileTrapDoorsOpenedEvent;
    public static event TrapDoorsClosed? WhileTrapDoorsClosedEvent;

    public event TrapDoorsFullyOpened? OnTrapDoorsFullyOpenedInstance;
    public event TrapDoorsFullyClosed? OnTrapDoorsFullyClosedInstance;
    public event TrapDoorsOpened? WhileTrapDoorsOpenedInstance;
    public event TrapDoorsClosed? WhileTrapDoorsClosedInstance;

    public readonly int Id;
    public float LifeSpan;
    public float LifeTime;
    public float Rotation; // previously EulerAngles
    public bool RotateTowardsVelocity = true;

    public Vector3 Position;
    public Vector2 Velocity;

    public Matrix World;
    public Matrix View;
    public Matrix Projection;

    public Texture2D BodyTexture;
    public Texture2D WingTexture;
    public Texture2D InteriorTexture;
    public Model Model { get; set; }
    public ModelMesh TrapDoorL { get; }
    public ModelMesh TrapDoorR { get; }
    public ModelMesh PropellerL { get; }
    public ModelMesh PropellerR { get; }

    public OggAudio PlaneLoop { get; set; }

    private Matrix[] _boneTransforms;
    public Airplane(Vector3 position, Vector2 velocity, float lifeSpan) {
        Position = position;
        Velocity = velocity;

        LifeSpan = lifeSpan;

        PlaneLoop = new OggAudio("Content/Assets/sounds/plane/plane_loop.ogg");

        Model = ModelResources.Plane.Duplicate();
        _boneTransforms = new Matrix[Model.Bones.Count];
        Model.CopyAbsoluteBoneTransformsTo(_boneTransforms);
        Model!.Root.Transform = World;

        TrapDoorL = Model.Meshes["Plane_Door1"];
        TrapDoorR = Model.Meshes["Plane_Door2"];
        PropellerL = Model.Meshes["Plane_Prop1"];
        PropellerR = Model.Meshes["Plane_Prop2"];

        BodyTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/plane/body");
        WingTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/plane/wings");
        InteriorTexture = TextureGlobals.Pixels[Color.Black];

        PlaneLoop.Volume = 0f;
        PlaneLoop.MaxVolume = 0.25f;
        PlaneLoop.Instance.IsLooped = true;
        PlaneLoop.Play();

        GameUI.Pause.OnPress += PauseSounds;
        TankGame.OnFocusLost += PauseSoundsWindow;
        TankGame.OnFocusRegained += ResumeSounds;

        Id = Array.FindIndex(AllPlanes, plane => plane is null);

        AllPlanes[Id] = this;
    }

    private void ResumeSounds(object? sender, nint e) {
        if (!GameUI.Paused)
            PlaneLoop.Play();
    }
    private void PauseSoundsWindow(object? sender, nint e) { 
        if (!GameUI.Paused)
            PlaneLoop.Pause(); 
    }
    private void PauseSounds() {
        if (!GameUI.Paused)
            PlaneLoop.Pause();
        else
            PlaneLoop.Play();
    }

    private bool _wereDoorsFullyOpen;
    public bool AreDoorsFullyOpen { get; private set; }
    // TrapDoor specific private fields
    private float _openPercent;
    private bool _isOpening;
    private float _doorLRotation;
    private float _doorRRotation;

    public void OpenTrapDoors() => _isOpening = true;
    public void CloseTrapDoors() => _isOpening = false;
    /// <summary>Returns a random position on the outskirts of the playing field. Pass Server.ServerRandom if doing server-sided stuff.
    /// Use Client.ClientRandom if not.</summary>
    public static Vector2 ChooseRandomXZPosition(Random random) {
        var spawnZAxis = random.Next(2) == 0;

        // if spawnZAxis, spawn on top if false, bottom if true
        // if !spawnZAxis, spawn on left if false, right if true
        var spawnOtherSide = random.Next(2) == 0;

        var randomPos = new Vector2();

        if (spawnZAxis) {
            if (spawnOtherSide) randomPos.Y = GameScene.MAX_Z + 150;
            else randomPos.Y = GameScene.MIN_Z - 150;

            randomPos.X = random.NextFloat(GameScene.MIN_X, GameScene.MAX_X);
        } else {
            if (spawnOtherSide) randomPos.X = GameScene.MAX_X + 150;
            else randomPos.X = GameScene.MIN_X - 150;

            randomPos.Y = random.NextFloat(GameScene.MIN_Z, GameScene.MAX_Z);
        }
        return randomPos;
    }
    /// <summary>Returns a random direction for a plane to use, normalized.</summary>
    /// <param name="random">Pass Server.ServerRandom if doing server-sided stuff. Use Client.ClientRandom if not.</param>
    /// <param name="posXZ">The position of the plane, initially.</param>
    /// <param name="xPotential">What percent of the map's x-axis, centering from the center of the map, the plane can take flight towards.</param>
    /// <param name="zPotential">What percent of the map's z-axis, centering from the center of the map, the plane can take flight towards.</param>
    public static Vector2 ChooseRandomFlightTarget(Random random, Vector2 posXZ, float speedMult = 1f, float xPotential = 1f, float zPotential = 1f) {
        // magic numbers used reduce bias towards negative Z from the origin
        var zOff = 132;
        var randomPosition = new Vector2 {
            X = random.NextFloat(GameScene.MIN_X * xPotential, GameScene.MAX_X * xPotential),
            Y = random.NextFloat((GameScene.MIN_Z - zOff) * zPotential, (GameScene.MAX_Z - zOff) * zPotential)
        };
        randomPosition.Y += zOff;
        

        // return normalized so the user can modify its magnitude
        return Vector2.Normalize(randomPosition - posXZ) * speedMult;
    }
    public void Remove() {
        GameUI.Pause.OnPress -= PauseSounds;
        TankGame.OnFocusLost -= PauseSoundsWindow;
        TankGame.OnFocusRegained -= ResumeSounds;

        PlaneLoop.Stop();
        //PlaneLoop.Dispose();
        Model = null;
        AllPlanes[Id] = null;
    }
    public void Update() {
        LifeTime += RuntimeData.DeltaTime;

        Position.X += Velocity.X * RuntimeData.DeltaTime;
        Position.Z += Velocity.Y * RuntimeData.DeltaTime;

        // TODO: unhardcode this.
        if (LifeTime > 60)
            OpenTrapDoors();
        if (LifeTime > 180)
            CloseTrapDoors();

        AreDoorsFullyOpen = false;
        // opening/closing w/hatch speed
        var openSpeed = 0.03f;
        if (_isOpening) {
            _openPercent += openSpeed * RuntimeData.DeltaTime;
            if (_openPercent > 1) {
                AreDoorsFullyOpen = true;

                if (AreDoorsFullyOpen && !_wereDoorsFullyOpen) {
                    OnTrapDoorsFullyOpenedEvent?.Invoke(this);
                    OnTrapDoorsFullyOpenedInstance?.Invoke(this);
                }
                _openPercent = 1; 
            }

            WhileTrapDoorsOpenedEvent?.Invoke(this);
            WhileTrapDoorsOpenedInstance?.Invoke(this);

            _doorLRotation = _doorRRotation = Easings.GetEasingBehavior(EasingFunction.OutBack, _openPercent) * MathHelper.PiOver2;
        }
        else { 
            _openPercent -= openSpeed * RuntimeData.DeltaTime;
            if (_openPercent < 0) { 
                _openPercent = 0;

                if (!AreDoorsFullyOpen && _wereDoorsFullyOpen) {
                    OnTrapDoorsFullyClosedEvent?.Invoke(this);
                    OnTrapDoorsFullyClosedInstance?.Invoke(this);
                }
            }

            WhileTrapDoorsClosedEvent?.Invoke(this);
            WhileTrapDoorsClosedInstance?.Invoke(this);

            _doorLRotation = _doorRRotation = Easings.GetEasingBehavior(EasingFunction.OutCubic, _openPercent) * MathHelper.PiOver2;
        }

        // why the fuck do i have to do "-rotation + Pi/2"
        // is this a fucked-up translation fault?
        if (RotateTowardsVelocity) Rotation = -Velocity.ToRotation() + MathHelper.PiOver2;

        var screenPos = MatrixUtils.ConvertWorldToScreen(Vector3.Zero, World, View, Projection);
        if (CameraGlobals.IsUsingFirstPresonCamera) {
            PlaneLoop.Volume = SoundUtils.GetVolumeFromCameraPosition(Position, CameraGlobals.RebirthFreecam.Position);
        }
        else PlaneLoop.Volume = SoundUtils.GetVolumeFromScreenPosition(screenPos);
        //PlaneLoop.Instance.Pan = SoundUtils.GetPanFromScreenPosition(screenPos.X) - 1;// * 0.8f;
        // TODO: once bk fixes mesh/bone problem, make this work. find mesh via Meshes["Name"] and then transform mesh.ParentBone.Transform
        PropellerR.ParentBone.Transform = Matrix.CreateScale(100) * Matrix.CreateRotationY(LifeTime / 5f) * Matrix.CreateRotationX(-MathHelper.PiOver2)
            * Matrix.CreateTranslation(new Vector3(-36.501f, 0, 22.2054f));
        PropellerL.ParentBone.Transform = Matrix.CreateScale(100) * Matrix.CreateRotationY(-LifeTime / 5f) * Matrix.CreateRotationX(-MathHelper.PiOver2)
            * Matrix.CreateTranslation(new Vector3(36.501f, 0, 22.2054f));

        TrapDoorL.ParentBone.Transform = Matrix.CreateScale(100) * Matrix.CreateRotationX(-MathHelper.PiOver2) * Matrix.CreateRotationZ(_doorLRotation)
            * Matrix.CreateTranslation(11.565f, -4.4863f, -0.000009f);
        // negative rotation is applied since we want the two trap doors to appear to rotate in opposite directions to "open the hatch"
        TrapDoorR.ParentBone.Transform = Matrix.CreateScale(100) * Matrix.CreateRotationX(-MathHelper.PiOver2) * Matrix.CreateRotationZ(-_doorRRotation)
            * Matrix.CreateTranslation(-11.565f, -4.4863f, -0.000009f);
        // x and z values grabbed from blender. is hacky but it works i guess

        if (LifeTime > LifeSpan) Remove();

        View = CameraGlobals.GameView;
        Projection = CameraGlobals.GameProjection;

        _wereDoorsFullyOpen = AreDoorsFullyOpen;
    }
    public static void SpawnPlaneWithSmokeGrenades(Vector2? spawnSet = null, Vector2? flightTarget = null, bool wasSentFromNet = false) {
        // TODO: sync the plane over the net... soon!
        Vector2 pos;
        Vector2 vel;

        pos = spawnSet != null ? spawnSet.Value : ChooseRandomXZPosition(Client.ClientRandom);
        vel = flightTarget != null ? flightTarget.Value : ChooseRandomFlightTarget(Client.ClientRandom, pos, 2.5f, 0.5f, 0.5f);

        var pos3d = new Vector3(pos.X, 100, pos.Y);

        if (!wasSentFromNet)
            Client.SendAirplaneSpawn(pos, vel);

        var plane = new Airplane(pos3d, vel, 400f);
        plane.WhileTrapDoorsOpenedInstance = (t) => {
            if (RuntimeData.RunTime % 30 <= RuntimeData.DeltaTime) {
                ParticleGameplay.CreateSmokeGrenade(GameHandler.Particles, plane.Position, Vector3.Down + new Vector3(plane.Velocity.X, 0, plane.Velocity.Y) * 0.25f);
            }
        };
    }
    public void Render() {
        if (!GameScene.ShouldRenderAll)
            return;
        World = Matrix.CreateScale(0.6f)
            * Matrix.CreateRotationY(Rotation) 
            * Matrix.CreateTranslation(Position);    
        Projection = CameraGlobals.GameProjection;
        View = CameraGlobals.GameView;

        Model.CopyAbsoluteBoneTransformsTo(_boneTransforms);
        Model!.Root.Transform = World;

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
                        case "Plane_Door1":
                        case "Plane_Door2":
                        case "Plane_Prop1":
                        case "Plane_Prop2":
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
