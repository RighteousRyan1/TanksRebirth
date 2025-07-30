using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.UI.MainMenu;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent;

#pragma warning disable
public class GameShaders
{
    public static Effect MouseShader { get; private set; }
    public static Effect GaussianBlurShader { get; private set; }
    public static Effect LanternShader { get; private set; }
    public static Effect AnimatedRainbow { get; private set; }

    public static float BlurFactor = 0.0075f;

    public static void Initialize() {
        GaussianBlurShader = GameResources.GetGameResource<Effect>("Assets/shaders/gaussian_blur");
        MouseShader = GameResources.GetGameResource<Effect>("Assets/shaders/mouse");
        LanternShader = GameResources.GetGameResource<Effect>("Assets/shaders/lantern");
        AnimatedRainbow = GameResources.GetGameResource<Effect>("Assets/shaders/rainbow_grad_anim");
    }
    //static float val = 1f;
    public static void UpdateShaders() {
        AnimatedRainbow.Parameters["oTime"]?.SetValue((float)TankGame.LastGameTime.TotalGameTime.TotalSeconds);
        AnimatedRainbow.Parameters["oStrength"].SetValue(0.5f);
        AnimatedRainbow.Parameters["oAngle"].SetValue(RuntimeData.RunTime * 0.1f);
        AnimatedRainbow.Parameters["oSpeed"].SetValue(0.5f);
        AnimatedRainbow.Parameters["oMinLum"].SetValue(0.1f);

        MouseShader.Parameters["oGlobalTime"].SetValue((float)TankGame.LastGameTime.TotalGameTime.TotalSeconds);
        var value = PlayerID.PlayerTankColors[NetPlay.GetMyClientId()];
        MouseShader.Parameters["oColor"].SetValue(value.ToVector3());
        /*MouseRenderer.HsvToRgb(TankGame.GameUpdateTime % 255 / 255f * 360, 1, 1).ToVector3());*/
        MouseShader.Parameters["oSpeed"].SetValue(15f);
        MouseShader.Parameters["oSpacing"].SetValue(10f);
        // MouseShader.Parameters["oRotation"].SetValue(MathHelper.Pi);

        GaussianBlurShader.Parameters["oResolution"].SetValue(Vector2.One);
        GaussianBlurShader.Parameters["oBlurFactor"].SetValue(BlurFactor);
        GaussianBlurShader.Parameters["oEnabledBlur"].SetValue(MainMenuUI.IsActive);

        /*if (Input.CurrentKeySnapshot.IsKeyDown(Keys.Up))
            val += 0.01f;
        else if (Input.CurrentKeySnapshot.IsKeyDown(Keys.Down))
            val -= 0.01f;*/


        LanternShader.Parameters["oTime"]?.SetValue((float)TankGame.LastGameTime.TotalGameTime.TotalSeconds);
        //TestShader.Parameters["oBend"]?.SetValue(val);
        //TestShader.Parameters["oDistortionFactor"].SetValue(MouseUtils.MousePosition.X / WindowUtils.WindowWidth);
        if (Difficulties.Types["LanternMode"]) {
            var activeTanks = GameHandler.AllPlayerTanks.Where(x => x is not null && !x.IsDestroyed).ToArray();

            if (activeTanks.Length == 0 || MainMenuUI.IsActive) {
                LanternShader.Parameters["oLanternCount"]?.SetValue(0);
                return;
            }

            var lanternPositions = new Vector2[activeTanks.Length];
            var lanternPowers = new float[activeTanks.Length];
            var lanternColors = new Vector3[activeTanks.Length];

            for (int i = 0; i < activeTanks.Length; i++) {
                var tank = activeTanks[i];

                var screenPos = MatrixUtils.ConvertWorldToScreen(
                    Vector3.Zero,
                    Matrix.CreateTranslation(tank.Position.X, 11, tank.Position.Y),
                    CameraGlobals.GameView,
                    CameraGlobals.GameProjection
                ).ToCartesianCoordinates();

                lanternPositions[i] = screenPos;
                lanternPowers[i] = Client.ClientRandom.NextFloat(0.16f, 0.165f);

                lanternColors[i] = new Vector3(1.0f, 0.7f, 0.3f);
            }

            LanternShader.Parameters["oMaxDark"]?.SetValue(0.015f);
            LanternShader.Parameters["oLanternCount"]?.SetValue(activeTanks.Length);
            LanternShader.Parameters["oLanternPositions"]?.SetValue(lanternPositions);
            LanternShader.Parameters["oLanternPowers"]?.SetValue(lanternPowers);
            LanternShader.Parameters["oLanternColors"]?.SetValue(lanternColors);
            LanternShader.Parameters["oDarknessLevel"]?.SetValue(0.1f);
        }
    }
}

