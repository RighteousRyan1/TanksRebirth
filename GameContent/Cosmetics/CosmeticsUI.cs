using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Globals.Assets;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.Systems.ParticleSystem;
using TanksRebirth.GameContent.UI.MainMenu;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.Cosmetics;

#pragma warning disable

// ikik, not in the UI namespace but whatever
public static class CosmeticsUI {
    public static bool IsActive => MainMenuUI.MenuState == MainMenuUI.UIState.Cosmetics;

    static float _interp;
    static bool _switch;

    public static RenderableChest Chest;

    internal static Particle dispPart;

    static BoundingBox _clickSpot;

    public static void Initialize() {
        Chest = new(new(0, 0, 0), CameraGlobals.GameView, CameraGlobals.GameProjection);
        Chest.Rotation = new Vector3(0, 0, MathHelper.Pi + MathHelper.PiOver4);
    }

    public static void Update() {
        Chest.DrawParams.View = CameraGlobals.GameView;
        Chest.DrawParams.Projection = CameraGlobals.GameProjection;

        GameShaders.BlurFactor += (!IsActive ? 0.000075f : -0.000075f) * RuntimeData.DeltaTime;
        GameShaders.BlurFactor = MathHelper.Clamp(GameShaders.BlurFactor, 0f, 0.0075f);

        _interp += (_switch ? 0.015f : -0.015f) * RuntimeData.DeltaTime;

        _interp = MathHelper.Clamp(_interp, 0, 1);

        if (_interp == 1f)
            _switch = false;

        Chest.Scale = 0.3f;

        Chest.ChestPosition = new Vector3(-875f, 992.81537f, 2860f);

        var basePos = Chest.ChestPosition - new Vector3(15, 23, 15);

        float boxDimsXZ = 5f;
        float boxHeight = 9f;
        float yOff = 50f;

        _clickSpot = new(basePos - new Vector3(boxDimsXZ, -yOff, boxDimsXZ), 
            basePos + new Vector3(boxDimsXZ, yOff + boxHeight, boxDimsXZ));

        var ray = RayUtils.GetMouseToWorldRay();

        var inter = ray.Intersects(_clickSpot);

        if (inter.HasValue) {
            if (_interp == 0f)
                _switch = true;

            // GameHandler.Particles.MakeShineSpot(ray.Direction * inter.Value, Color.White, 0.5f);

            // ChatSystem.SendMessage(inter.Value, ColorUtils.DiscoPartyColor);
        }

        Chest.LidRotation = new Vector3(0, Easings.GetEasingBehavior
            (_switch ? EasingFunction.OutBounce : EasingFunction.OutSine, _interp) * (MathHelper.Pi + MathHelper.PiOver4 / 2), 
            0);
    }
    public static void EnterMenu() {
        var pos = Chest.ChestPosition;

        dispPart = GameHandler.Particles.MakeParticle(Vector3.Zero, 
            string.Format(TankGame.GameLanguage.KeysCount, TankGame.SaveFile.CollectedKeys));

        dispPart.IsIn2DSpace = true;
        dispPart.ToScreenSpace = true;

        dispPart.Color = Color.White;

        dispPart.HasAdditiveBlending = false;
        dispPart.Origin2D = FontGlobals.RebirthFont.MeasureString(dispPart.Text) / 2;
        dispPart.Scale = Vector3.One;
        dispPart.Alpha = 0;

        dispPart.UniqueDraw = particle => {
            particle.Position = Chest.ChestPosition + new Vector3(0, 150, 0);
            DrawUtils.DrawStringWithBorder(TankGame.SpriteRenderer, FontGlobals.RebirthFontLarge, particle.Text,
                MatrixUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(particle.Position),
                    CameraGlobals.GameView, CameraGlobals.GameProjection),
                particle.Color, Color.Black, new(particle.Scale.X, particle.Scale.Y), 0f, Anchor.Center);
        };

        SpawnKeys();
    }

    static void SpawnKeys() {
        var numKeys = TankGame.SaveFile.CollectedKeys;

        var tex = GameResources.GetGameResource<Texture2D>("Assets/textures/chest/key");

        // TextureGlobals.Pixels[color]

        float startRads = -MathHelper.PiOver2;   // left of fan
        float endRads = MathHelper.PiOver2;      // right of fan
        var origPos = Chest.ChestPosition + new Vector3(0, 20, 0);

        var diff = (endRads - startRads) / (numKeys - 1);

        for (int i = 0; i < numKeys; i++) {
            var keyPart = GameHandler.Particles.MakeParticle(origPos, ModelGlobals.Key.Asset, tex);

            float k = (numKeys == 1) ? 0.5f : (float)i / (numKeys - 1);
            // helps calculate the goal position
            float angle = MathHelper.Lerp(startRads, endRads, k);

            var destination = new Vector3(50, 50, 0).RotateXY(angle + MathHelper.PiOver4);
            destination = destination.RotateXZ(-MathHelper.PiOver4);
            // destination = destination.RotateXZ(radLerp);

            keyPart.Alpha = 1f;
            // maybe this is retarded
            keyPart.Scale = Vector3.One * 40;
            keyPart.HasAdditiveBlending = false;
            keyPart.Pitch = MathHelper.PiOver2;
            keyPart.Color = Color.White;
            keyPart.FaceTowardsMe = true;

            float t = 0;

            float phaseOffset = Client.ClientRandom.NextFloat(-100_000, 100_000);

            float moveSpeed = 0.02f;

            keyPart.UniqueBehavior = (p) => {
                // gives time for the camera to transition
                if (p.LifeTime < 80) return;

                if (MainMenuUI.MenuState == MainMenuUI.UIState.Cosmetics) {
                    t += moveSpeed * RuntimeData.DeltaTime;

                    if (t > 1) t = 1;
                }
                else {
                    if (t <= 0) p.Destroy();

                    t -= moveSpeed * 2.5f * RuntimeData.DeltaTime;

                    if (t < 0) t = 0;
                }
                float ease = Easings.GetEasingBehavior(EasingFunction.InOutSine, t);

                p.Roll = ease * MathHelper.TwoPi * 3 + MathHelper.PiOver2;

                float randX = Client.ClientRandom.NextFloat(-10, 20);
                float randY = Client.ClientRandom.NextFloat(-5, 5);

                float rand = Client.ClientRandom.NextFloat(0, 40);

                // framerate independence, baby!
                if (rand < RuntimeData.DeltaTime) {
                    GameHandler.Particles.MakeShineSpot(p.Position + new Vector3(randX, randY, 0),
                        Color.White, Client.ClientRandom.NextFloat(0.3f, 0.5f));
                }

                // if (InputUtils.KeyJustPressed(Microsoft.Xna.Framework.Input.Keys.X)) p.Destroy();
                float ySin = (MathF.Sin(RuntimeData.RunTime / 50 + phaseOffset)) * 0.5f * 5;
                p.Position = origPos + (destination * ease) + new Vector3(0, ySin, 0);
            };
        }
    }

    // non-important shit
    public static void LeaveMenu() {
        dispPart?.Destroy();
    }
    public static void RenderCrates() {
        DebugManager.DrawBoundingBox(_clickSpot, Color.White, CameraGlobals.GameView, CameraGlobals.GameProjection);
        Chest?.Render();
    }
}
