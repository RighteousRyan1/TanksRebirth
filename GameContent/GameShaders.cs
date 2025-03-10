using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals;
using TanksRebirth.Net;
using TanksRebirth.GameContent.UI.MainMenu;
using TanksRebirth.GameContent.Globals;

namespace TanksRebirth.GameContent;

#pragma warning disable
public class GameShaders
{
    public static Effect MouseShader { get; private set; }
    public static Effect GaussianBlurShader { get; private set; }
    public static Effect LanternShader { get; private set; }
    public static Effect AnimatedRainbow { get; private set; }

    public static float BlurFactor = 0.0075f;

    private static bool _lantern;
    public static bool LanternMode {
        get => _lantern;
        set {
            _lantern = value;
            LanternShader.Parameters["oLantern"]?.SetValue(value);
        }
    }

    public static void Initialize() {
        GaussianBlurShader = GameResources.GetGameResource<Effect>("Assets/Shaders/gaussian_blur");
        MouseShader = GameResources.GetGameResource<Effect>("Assets/Shaders/mouse");
        LanternShader = GameResources.GetGameResource<Effect>("Assets/Shaders/lantern");
        AnimatedRainbow = GameResources.GetGameResource<Effect>("Assets/Shaders/rainbow_grad_anim");
    }
    //static float val = 1f;
    public static void UpdateShaders() {
        AnimatedRainbow.Parameters["oTime"]?.SetValue((float)TankGame.LastGameTime.TotalGameTime.TotalSeconds);
        AnimatedRainbow.Parameters["oStrength"].SetValue(0.5f);
        AnimatedRainbow.Parameters["oAngle"].SetValue(0f);
        AnimatedRainbow.Parameters["oSpeed"].SetValue(0.5f);
        AnimatedRainbow.Parameters["oMinLum"].SetValue(0.9f);

        MouseShader.Parameters["oGlobalTime"].SetValue((float)TankGame.LastGameTime.TotalGameTime.TotalSeconds);
        var value = PlayerID.PlayerTankColors[PlayerTank.MyTankType];
        MouseShader.Parameters["oColor"].SetValue(value);
        /*MouseRenderer.HsvToRgb(TankGame.GameUpdateTime % 255 / 255f * 360, 1, 1).ToVector3());*/
        MouseShader.Parameters["oSpeed"].SetValue(-20f);
        MouseShader.Parameters["oSpacing"].SetValue(10f);

        GaussianBlurShader.Parameters["oResolution"].SetValue(Vector2.One);
        GaussianBlurShader.Parameters["oBlurFactor"].SetValue(BlurFactor);
        GaussianBlurShader.Parameters["oEnabledBlur"].SetValue(MainMenuUI.Active);

        /*if (Input.CurrentKeySnapshot.IsKeyDown(Keys.Up))
            val += 0.01f;
        else if (Input.CurrentKeySnapshot.IsKeyDown(Keys.Down))
            val -= 0.01f;*/


        LanternShader.Parameters["oTime"]?.SetValue((float)TankGame.LastGameTime.TotalGameTime.TotalSeconds);
        //TestShader.Parameters["oBend"]?.SetValue(val);
        //TestShader.Parameters["oDistortionFactor"].SetValue(MouseUtils.MousePosition.X / WindowUtils.WindowWidth);

        if (Difficulties.Types["LanternMode"]) {
            var index = NetPlay.GetMyClientId(); //Array.FindIndex(GameHandler.AllPlayerTanks, x => x is not null && !x.Dead);
            var pos = index > -1 && !MainMenuUI.Active ? MatrixUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(GameHandler.AllPlayerTanks[index].Position.X, 11, GameHandler.AllPlayerTanks[index].Position.Y), CameraGlobals.GameView, CameraGlobals.GameProjection).ToCartesianCoordinates() : new Vector2(-1);
            // var val = (float)TankGame.LastGameTime.TotalGameTime.TotalSeconds;
            LanternShader.Parameters["oPower"]?.SetValue(MainMenuUI.Active ? 100f : GameHandler.GameRand.NextFloat(0.195f, 0.20f));
            LanternShader.Parameters["oPosition"]?.SetValue(pos/*MouseUtils.MousePosition.ToCartesianCoordinates()*/);
        }
    }
}

