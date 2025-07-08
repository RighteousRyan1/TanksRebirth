using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using TanksRebirth.GameContent.UI;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals;
using TanksRebirth.Net;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.UI.MainMenu;
using TanksRebirth.GameContent.UI.LevelEditor;
using TanksRebirth.Graphics;
using TanksRebirth.GameContent.ID;

namespace TanksRebirth.GameContent;

public static class RebirthMouse
{
    public static Texture2D MouseTexture { get; private set; }

    public static int numDots = 10;

    private static float _sinScale;

    public static bool ShouldRender = true;

    public static float DistUntilPathTrace = 1575f;

    private static Vector2 _oldMouse;

    public static bool DoTrail = true;

    public static Trail? CursorTrail;

    public const int TRAIL_POINTS_BASE = 20;

    public static void Initialize() {
        CursorTrail = new(TankGame.Instance.GraphicsDevice, ColorUtils.ChangeColorBrightness(PlayerID.PlayerTankColors[NetPlay.GetMyClientId()].ToColor(), 0.5f));
    }

    public static void DrawMouse() {
        numDots = 10;
        if (!ShouldRender)
            return;
        if (!MainMenuUI.Active && !GameUI.Paused && !LevelEditorUI.Active) {
            var clientId = NetPlay.CurrentClient is null ? 0 : NetPlay.CurrentClient.Id;
            if (GameHandler.AllPlayerTanks[clientId] is not null) {
                var me = GameHandler.AllPlayerTanks[clientId];
                var tankPos = MatrixUtils.ConvertWorldToScreen(new Vector3(0, 11, 0), me.World, CameraGlobals.GameView, CameraGlobals.GameProjection);

                if (GameUtils.Distance_WiiTanksUnits(tankPos, MouseUtils.MousePosition) >= DistUntilPathTrace.ToResolutionX()) // any scale doesnt matter?
                {
                    var tex = GameResources.GetGameResource<Texture2D>("Assets/textures/misc/mouse_dot");

                    // GameHandler.ClientLog.Write("One Loop:", LogType.Info);
                    for (int i = 0; i < numDots; i++) {
                        var curDrawPos = Vector2.Lerp(tankPos, MouseUtils.MousePosition, (float)i / numDots);// tankPos.DirectionOf(MouseUtils.MousePosition) * i;

                        for (int j = 0; j < 4; j++)
                            TankGame.SpriteRenderer.Draw(tex, curDrawPos, null, Color.White, MathHelper.PiOver2 * j, tex.Size(), new Vector2(0.35f).ToResolution(), default, default);
                    }
                }
            }
        }

        if (DoTrail) {
            CursorTrail.MainColor = ColorUtils.ChangeColorBrightness(PlayerID.PlayerTankColors[NetPlay.GetMyClientId()].ToColor(), 0.5f);
            CursorTrail?.Update(MouseUtils.MousePosition.ToResolution(new(Trail.WIDTH_TRAILS_ENJOY, Trail.HEIGHT_TRAILS_ENJOY)));
            CursorTrail?.Draw();
            /*if (!float.IsInfinity(RuntimeData.DeltaTime)) {
                if (RuntimeData.RunTime % 60 <= RuntimeData.DeltaTime) {
                    var newVal = (int)(TRAIL_POINTS_BASE / RuntimeData.DeltaTime);

                    Console.WriteLine(newVal);
                    if (newVal > 0)
                        CursorTrail.MaxTrailPoints = newVal;
                }
            }*/
        }
        _sinScale = MathF.Sin((float)TankGame.LastGameTime.TotalGameTime.TotalSeconds);

        MouseTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/misc/cursor_1");
        TankGame.SpriteRenderer.Draw(MouseTexture, MouseUtils.MousePosition, null, Color.White, 0f, MouseTexture.Size() / 2, (1f + _sinScale / 16).ToResolution(), default, default);
        _oldMouse = MouseUtils.MousePosition;
    }
}
