using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using TanksRebirth.GameContent.UI;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent;

public static class RebirthMouse
{
    public static Texture2D MouseTexture { get; private set; }

    public static int numDots = 10;

    private static float _sinScale;

    public static bool ShouldRender = true;

    public static float DistUntilPathTrace = 1575f;

    private static Vector2 _oldMouse;

    public static bool DoTrail = false;

    public static void DrawMouse() {
        numDots = 10;
        if (!ShouldRender)
            return;
        if (!MainMenu.Active && !GameUI.Paused && !LevelEditor.Active) {
            var clientId = NetPlay.CurrentClient is null ? 0 : NetPlay.CurrentClient.Id;
            if (GameHandler.AllPlayerTanks[clientId] is not null) {
                var me = GameHandler.AllPlayerTanks[clientId];
                var tankPos = MatrixUtils.ConvertWorldToScreen(new Vector3(0, 11, 0), me.World, TankGame.GameView, TankGame.GameProjection);

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
            var p = GameHandler.ParticleSystem.MakeParticle(new Vector3(MouseUtils.MousePosition.X, MouseUtils.MousePosition.Y, 0), TankGame.WhitePixel);
            p.IsIn2DSpace = true;
            var dir = _oldMouse.DirectionOf(MouseUtils.MousePosition).ToResolution();
            p.Rotation2D = dir.ToRotation();
            p.TextureScale = new Vector2(dir.Length() * 1.1f, 20.ToResolutionY());
            p.Origin2D = new(0, TankGame.WhitePixel.Size().Y / 2);
            p.HasAddativeBlending = false;
            p.ToScreenSpace = false;
            p.UniqueBehavior = (pa) => {
                p.Alpha -= 0.06f;
                p.TextureScale -= new Vector2(0.06f);

                if (p.Alpha <= 0)
                    p.Destroy();

                p.Color = Color.SkyBlue;//GameUtils.HsvToRgb(TankGame.GameUpdateTime % 255 / 255f * 360, 1, 1);
            };
        }
        _sinScale = MathF.Sin((float)TankGame.LastGameTime.TotalGameTime.TotalSeconds);

        MouseTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/misc/cursor_1");
        TankGame.SpriteRenderer.Draw(MouseTexture, MouseUtils.MousePosition, null, Color.White, 0f, MouseTexture.Size() / 2, (1f + _sinScale / 16).ToResolution(), default, default);
        _oldMouse = MouseUtils.MousePosition;
    }
}
