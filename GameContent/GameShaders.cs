using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.UI;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent;

#pragma warning disable
public class GameShaders
{
    public static Effect MouseShader { get; private set; }
    public static Effect GaussianBlurShader { get; private set; }

    public static Effect LanternShader { get; private set; }

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
        GaussianBlurShader = GameResources.GetGameResource<Effect>("Assets/Shaders/GaussianBlur");
        MouseShader = GameResources.GetGameResource<Effect>("Assets/Shaders/MouseShader");
        LanternShader = GameResources.GetGameResource<Effect>("Assets/Shaders/testshader");
    }
    //static float val = 1f;
    public static void UpdateShaders() {
        MouseShader.Parameters["oGlobalTime"].SetValue((float)TankGame.LastGameTime.TotalGameTime.TotalSeconds);
        var value = PlayerID.PlayerTankColors[PlayerTank.MyTankType];
        MouseShader.Parameters["oColor"].SetValue(value);
        /*MouseRenderer.HsvToRgb(TankGame.GameUpdateTime % 255 / 255f * 360, 1, 1).ToVector3());*/
        MouseShader.Parameters["oSpeed"].SetValue(-20f);
        MouseShader.Parameters["oSpacing"].SetValue(10f);

        GaussianBlurShader.Parameters["oResolution"].SetValue(Vector2.One);
        GaussianBlurShader.Parameters["oBlurFactor"].SetValue(BlurFactor);
        GaussianBlurShader.Parameters["oEnabledBlur"].SetValue(MainMenu.Active);

        /*if (Input.CurrentKeySnapshot.IsKeyDown(Keys.Up))
            val += 0.01f;
        else if (Input.CurrentKeySnapshot.IsKeyDown(Keys.Down))
            val -= 0.01f;*/


        LanternShader.Parameters["oTime"]?.SetValue((float)TankGame.LastGameTime.TotalGameTime.TotalSeconds);
        //TestShader.Parameters["oBend"]?.SetValue(val);
        //TestShader.Parameters["oDistortionFactor"].SetValue(MouseUtils.MousePosition.X / WindowUtils.WindowWidth);

        if (Difficulties.Types["LanternMode"]) {
            var index = NetPlay.GetMyClientId(); //Array.FindIndex(GameHandler.AllPlayerTanks, x => x is not null && !x.Dead);
            var pos = index > -1 && !MainMenu.Active ? MatrixUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(GameHandler.AllPlayerTanks[index].Position.X, 11, GameHandler.AllPlayerTanks[index].Position.Y), TankGame.GameView, TankGame.GameProjection).ToCartesianCoordinates() : new Vector2(-1);
            // var val = (float)TankGame.LastGameTime.TotalGameTime.TotalSeconds;
            LanternShader.Parameters["oPower"]?.SetValue(MainMenu.Active ? 100f : GameHandler.GameRand.NextFloat(0.195f, 0.20f));
            LanternShader.Parameters["oPosition"]?.SetValue(pos/*MouseUtils.MousePosition.ToCartesianCoordinates()*/);
        }
    }
}

