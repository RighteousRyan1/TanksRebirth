using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Globals.Assets;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.GameContent.UI;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.Graphics;

#pragma warning disable

// TODO: texture pack support
public static class RoomScene {
    public static readonly Vector3 TableScenePos = new(-350f, -61.7f, 330f);
    public static readonly Vector3 FloorScenePos = new(-155f, -0.15f, 210f);

    public static readonly float ClockRotation = MathHelper.PiOver4;


    // bro there is no construcor stop yapping stupid ide
    public static Model RoomSkyboxScene;

    public static ModelMesh HandHour;
    public static ModelMesh HandMinute;
    public static ModelMesh Pendulum;

    public static Dictionary<string, Texture2D> RoomSkyboxTextures = [];
    public static Dictionary<string, Color> BookColors = [];
    static List<ModelMesh> _transparentFaces = [];

    public static float Scale;
    public static Vector3 Rotation;
    public static Vector3 Position;
    public static Vector3 UsedPosition = TableScenePos;

    public static Matrix World;
    public static Matrix View;
    public static Matrix Projection;

    static Matrix[] _boneTransforms;

    static Matrix _baseMinuteTransform;
    static Matrix _baseHourTransform;
    static Matrix _basePendulumTransform;
    static RoomScene() {
        // .png breaks the UVs vs .jpg
        RoomSkyboxScene = ModelGlobals.Room.Asset;
        _boneTransforms = new Matrix[RoomSkyboxScene.Bones.Count];
        HandHour = RoomSkyboxScene.Meshes["Clock_Hand_Hour"];
        HandMinute = RoomSkyboxScene.Meshes["Clock_Hand_Minute"];
        Pendulum = RoomSkyboxScene.Meshes["Clock_Pendulum"];

        RoomSkyboxScene.CopyAbsoluteBoneTransformsTo(_boneTransforms);
        RoomSkyboxScene!.Root.Transform = World;

        _baseMinuteTransform = HandMinute.ParentBone.Transform;
        _baseHourTransform = HandHour.ParentBone.Transform;
        _basePendulumTransform = Pendulum.ParentBone.Transform;

        InitializeTextures();

        ChimeS1 = GetClockSfx("chime_seq1");
        ChimeS2 = GetClockSfx("chime_seq2");
        ChimeS3 = GetClockSfx("chime_seq3");
        ChimeS4 = GetClockSfx("chime_seq4");
        ChimeS5 = GetClockSfx("chime_seq5");
        ChimeHour = GetClockSfx("chime_hour");

        PopulateAlphaDict();
    }
    public static OggAudio GetClockSfx(string asset) => new($"Content/Assets/sounds/roomscene/{asset}.ogg");
    public static void InitializeTextures() {
        foreach (var file in Directory.GetFiles(Path.Combine("Content", "Assets", "models", "scene", "skybox", "textures"))) {
            var fileName = Path.GetFileNameWithoutExtension(file);
            RoomSkyboxTextures.Add(fileName, GameResources.GetGameResource<Texture2D>(file, false, false));
        }
        AssignBookColors();
    }
    // these will be random, for fun.
    public static void AssignBookColors() {
        foreach (var mesh in RoomSkyboxScene.Meshes) {
            if (!mesh.Name.Contains("Book")) continue;
            if (mesh.Name.Contains("Side")) continue;
            // X2CHECK: ServerRand should be fine to use since it's the same amount of randomization on each client.
            var pickedColor = ColorUtils.BrightColors[Client.ClientRandom.Next(ColorUtils.BrightColors.Length)];
            BookColors[mesh.Name] = pickedColor;
        }
    }

    static readonly Dictionary<string, string> MeshToTexture = new() {
        // ceiling
        ["Ceiling"] = "ceiling",

        // chair
        ["Chair_Leather"] = "leather_red_brown",
        ["Chair_Metal"] = "metal",

        // clock parts
        ["Clock_Brass"] = "brass",
        ["Clock_Face"] = "clock_face", // clock_face_0 is also supposed to be here...? not sure what to do about it until BigKitty does something.
        ["Clock_Glass"] = "glass",
        ["Clock_Hand_Hour"] = "black",
        ["Clock_Hand_Minute"] = "black",
        ["Clock_Pendulum"] = "brass",
        ["Clock_Wires"] = "brass_wire",
        ["Clock_Wood1"] = "wood_dark",
        ["Clock_Wood2"] = "wood_med",

        // room decor
        ["Curtains"] = "curtains",

        // desk books
        ["Desk_Book_Page1"] = "book1",
        ["Desk_Book_Page2"] = "book2",
        ["Desk_Book_Side"] = "book_side",

        // desk lamp
        ["Desk_Lamp"] = "brass",

        // closet door
        ["Door_Closet"] = "wood_white",
        ["Door_Closet_Knobs"] = "brass",

        // room door
        ["Door_Room_Handle_Brass"] = "brass",
        ["Door_Room_Handle_Metal"] = "metal",

        // floor & furniture
        ["Floor"] = "floor",
        ["Floor_Lamp"] = "brass",
        ["Floor_Lamp_Bowl"] = "lamps",

        // small lamp
        ["Lamp_Brass"] = "brass",
        ["Lamp_Shade"] = "lamps",
        ["Lamp_Wood"] = "wood_med",

        // trim/moulding
        ["Moulding"] = "wood_white",

        // plant
        ["Plant_Brown"] = "dirt", // Plant_Pot itself has its own 'color' or something? Check into that.
        ["Plant_Green"] = "plant",

        // misc
        ["Sphere"] = "pano",

        // table
        ["Table_Top"] = "wood_dark", // 'tabletop' will be 'wood_dark' for now, since 'tabletop' is mysteriously missing.
        ["Table_Metal"] = "metal",
        ["Table_Metal_Dark"] = "metal_dark",

        // walls & windows
        ["Walls"] = "walls",
        ["Windows_Glass"] = "glass",
        ["Windows_Sides"] = "window_sides",

        // shelf
        ["Shelf"] = "wood_dark"
    };

    static List<ModelMesh> GlassMeshes = [];
    static List<ModelMesh> OpaqueMeshes = [];

    public static string GetMeshTexture(ModelMesh mesh) {
        return MeshToTexture.TryGetValue(mesh.Name, out var textureName)
            ? textureName
            : "metal"; // fallback default
    }
    static void PopulateAlphaDict() {
        for (int i = 0; i < RoomSkyboxScene.Meshes.Count; i++) {
            var mesh = RoomSkyboxScene.Meshes[i];

            if (mesh.Name.Contains("glass", StringComparison.InvariantCultureIgnoreCase))
                GlassMeshes.Add(mesh);
            else
                OpaqueMeshes.Add(mesh);
        }
    }
    public static OggAudio ChimeS1;
    public static OggAudio ChimeS2;
    public static OggAudio ChimeS3;
    public static OggAudio ChimeS4;
    public static OggAudio ChimeS5;
    public static OggAudio ChimeHour;

    public static readonly Vector3 ClockAudioPosition = new(1663.5045f, 850.9713f, -65.44688f);

    public static float HourHandRotation;
    public static float MinuteHandRotation;
    public static float PendulumRotation;
    public static int Hour;
    public static int Minute;
    static int _oldHour;
    static int _oldMin;
    /* q1 = seq1
     * q2 = seq2, seq3
     * q3 = seq4, seq5, seq1
     * q4 = seq2, seq3, seq4, seq5 ---- chime * hour
     */
    public static void ChimeQuarterly(int quarter) {
        // quarter = 15 minutes * quarter.
        Task.Run(async () => {
            var timeBetweenSeqs = 3000;
            var vol = SoundUtils.GetVolumeFromCameraPosition(ClockAudioPosition, CameraGlobals.RebirthFreecam.Position, 2500);
            switch (quarter) {
                case 1:
                    SoundPlayer.PlaySoundInstance(ChimeS1, SoundContext.Effect, playNew: true, volume: vol);
                    break;
                case 2:
                    SoundPlayer.PlaySoundInstance(ChimeS2, SoundContext.Effect, playNew: true, volume: vol);
                    await Task.Delay(timeBetweenSeqs);
                    SoundPlayer.PlaySoundInstance(ChimeS3, SoundContext.Effect, playNew: true, volume: vol);
                    break;
                case 3:
                    SoundPlayer.PlaySoundInstance(ChimeS4, SoundContext.Effect, playNew: true, volume: vol);
                    await Task.Delay(timeBetweenSeqs);
                    SoundPlayer.PlaySoundInstance(ChimeS5, SoundContext.Effect, playNew: true, volume: vol);
                    await Task.Delay(timeBetweenSeqs);
                    SoundPlayer.PlaySoundInstance(ChimeS1, SoundContext.Effect, playNew: true, volume: vol);
                    break;
                default:
                    // do hourly bro.
                    break;
            }
        });
    }
    public static void ChimeHourly(int hour) {
        if (hour == 0) hour += 12;
        Task.Run(async () => {
            var timeBetweenSeqs = 3000;
            var vol = SoundUtils.GetVolumeFromCameraPosition(ClockAudioPosition, CameraGlobals.RebirthFreecam.Position, 2500);
            SoundPlayer.PlaySoundInstance(ChimeS2, SoundContext.Effect, playNew: true, volume: vol);
            await Task.Delay(timeBetweenSeqs);
            SoundPlayer.PlaySoundInstance(ChimeS3, SoundContext.Effect, playNew: true, volume: vol);
            await Task.Delay(timeBetweenSeqs);
            SoundPlayer.PlaySoundInstance(ChimeS4, SoundContext.Effect, playNew: true, volume: vol);
            await Task.Delay(timeBetweenSeqs);
            SoundPlayer.PlaySoundInstance(ChimeS5, SoundContext.Effect, playNew: true, volume: vol);
            for (int i = 0; i < hour; i++) {
                await Task.Delay(timeBetweenSeqs);
                vol = SoundUtils.GetVolumeFromCameraPosition(ClockAudioPosition, CameraGlobals.RebirthFreecam.Position, 2500);
                SoundPlayer.PlaySoundInstance(ChimeHour, SoundContext.Effect, playNew: true, volume: vol);
            }
        });
    }

    public static void Update() {
        UpdateRoom();
        UpdateClock();
    }
    public static void UpdateRoom() {
        // this is to line up the table with the game scene.
        // again, pretty magical values, but it is what it is.

        Rotation = new(0, 0, 0);
        //UsedPosition = TableScenePos; //new(/*-450f*/ -MouseUtils.Test.X * 1000, -200f, MouseUtils.Test.Y * 1000);
        Position = UsedPosition * Scale;
        View = CameraGlobals.GameView;
        Projection = CameraGlobals.GameProjection;
    }
    public static void UpdateClock() {
        // this is pretty magical, but it gives the room a good scale in comparison to the game scene.
        Scale = 10f;

        //var testX = MouseUtils.MousePosition.X / WindowUtils.WindowWidth;
        HourHandRotation = TimeUtils.InterpolateHourToDay(DateTime.Now);
        MinuteHandRotation = TimeUtils.InterpolateMinuteToHour(DateTime.Now);
        PendulumRotation = MathHelper.Pi / 32 * TimeUtils.SineForSecond(DateTime.Now, 0.5f);

        Hour = TimeUtils.GetHourFromCircle(HourHandRotation);
        Minute = TimeUtils.GetMinuteFromCircle(MinuteHandRotation);

        // hacky way to prevent chiming on game boot. 
        if (RuntimeData.RunTime > 60f) {
            if (Hour != _oldHour) {
                // Console.WriteLine($"Attempting chime at hour {Hour % 12}");
                ChimeHourly(Hour % 12);
            }
            // this will not run if the hour changes since this is an 'else if' branch.
            else if (Minute != _oldMin) {
                // prevent chimes that would have happened when unfocused
                if (Math.Abs(Minute - _oldMin) >= 3)
                    return;


                if (Minute % 15 == 0) {
                    // Console.WriteLine($"Attempting chime at quarter {Minute / 15}");
                    ChimeQuarterly(Minute / 15);
                }
            }
        }

        _oldHour = Hour;
        _oldMin = Minute;
    }

    // render

    public static void Render() {

        // don't cull backfaces
        TankGame.Instance.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
        World = Matrix.CreateScale(Scale)
            * Matrix.CreateFromYawPitchRoll(Rotation.Z, Rotation.Y, Rotation.X)
            * Matrix.CreateTranslation(Position);

        HandHour.ParentBone.Transform = Matrix.CreateRotationY(HourHandRotation * MathHelper.Tau) * _baseHourTransform;
        HandMinute.ParentBone.Transform = Matrix.CreateRotationY(MinuteHandRotation * MathHelper.Tau) * _baseMinuteTransform;
        Pendulum.ParentBone.Transform = Matrix.CreateRotationY(PendulumRotation) * _basePendulumTransform;

        RoomSkyboxScene.CopyAbsoluteBoneTransformsTo(_boneTransforms);
        RoomSkyboxScene!.Root.Transform = World;

        foreach (var mesh in OpaqueMeshes) {
            foreach (BasicEffect effect in mesh.Effects) {
                MeshEffectSet(mesh, effect);
                effect.Alpha = 1f;
            }
            mesh.Draw();
        }

        // particularly expensive i think
        //GlassMeshes.Sort((x, y) =>
        //Vector3.Distance(x.ParentBone.Transform.Translation, CameraGlobals.RebirthFreecam.Position)
        //.CompareTo(Vector3.Distance(y.ParentBone.Transform.Translation, CameraGlobals.RebirthFreecam.Position)));

        foreach (var mesh in GlassMeshes) {
            foreach (BasicEffect effect in mesh.Effects) {
                MeshEffectSet(mesh, effect);

                effect.Alpha = 0.15f;
            }
            mesh.Draw();
        }
    }
    static void MeshEffectSet(ModelMesh mesh, BasicEffect effect) {
        effect.World = _boneTransforms[mesh.ParentBone.Index];
        effect.View = View;
        effect.Projection = Projection;

        effect.TextureEnabled = true;
        if (!mesh.Name.Contains("Shelf_Book") || mesh.Name.Contains("Side"))
            effect.Texture = GameResources.GetGameResource<Texture2D>("Assets/models/scene/skybox/textures/" + GetMeshTexture(mesh));
        else
            effect.Texture = TextureGlobals.Pixels[BookColors[mesh.Name]];
        effect.SetDefaultGameLighting();
        effect.SpecularPower = 32f;
    }
}
