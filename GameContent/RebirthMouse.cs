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

// turn into nonstatic class because local multiplayer prep :)
public class RebirthMouse {
    public const float PATH_TRACE_MIN_DIST = 1575.0f;
    public const int TRAIL_POINTS_BASE = 20;

    static float _scaleOscillation;
    public Trail? CursorTrail;

    public Vector2 Position;
    public float Rotation;

    public Texture2D DotTexture;
    public Texture2D MouseTexture;

    public Color MouseColor;
    public Color TrailColor;

    public Effect _mouseShader;

    // the index of the player in the AllPlayerTanks array. this helps that player tank know what to aim at.
    public int Player;

    public int DotCount { get; set; } = 10;
    public bool ShouldRender { get; set; } = true;
    public bool HasTrail { get; set; } = true;

    public RebirthMouse(Color mouseColor, Color trailColor, int player, int dotCount = 10, bool hasTrail = true) {
        CursorTrail = new(TankGame.Instance.GraphicsDevice, trailColor) {
            StartWidth = 15f
        };
        DotCount = dotCount;
        HasTrail = hasTrail;

        MouseColor = mouseColor;
        TrailColor = trailColor;

        Player = player;

        DotTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/misc/mouse_dot");
        MouseTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/misc/cursor_1");

        _mouseShader = GameResources.GetGameResource<Effect>("Assets/shaders/mouse");

        _mouseShader.Parameters["oSpeed"].SetValue(15f);
        _mouseShader.Parameters["oSpacing"].SetValue(10f);
    }

    // drawn within MouseShader
    public void Draw() {
        if (!ShouldRender)
            return;

        _mouseShader.Parameters["oGlobalTime"].SetValue((float)TankGame.LastGameTime.TotalGameTime.TotalSeconds);
        // maybe just put this in the setter? idk. might make coloring stupidly annoying
        _mouseShader.Parameters["oColor"].SetValue(MouseColor.ToVector3());

        _scaleOscillation = MathF.Sin((float)TankGame.LastGameTime.TotalGameTime.TotalSeconds);
        var scaleReal = 1f + _scaleOscillation / 12;

        TankGame.SpriteRenderer.Begin(blendState: BlendState.AlphaBlend, effect: _mouseShader, rasterizerState: RenderGlobals.DefaultRasterizer);

        if (!MainMenuUI.IsActive && !GameUI.Paused && !LevelEditorUI.IsActive) {
            var playerId = Client.IsConnected() ? NetPlay.GetMyClientId() : Player;
            if (GameHandler.AllPlayerTanks[playerId] is not null) {
                var me = GameHandler.AllPlayerTanks[playerId];
                var tankPos = MatrixUtils.ConvertWorldToScreen(
                    new Vector3(0, 11, 0), me.DrawParams.World, CameraGlobals.GameView, CameraGlobals.GameProjection);

                // any scale doesnt matter?
                if (GameUtils.TanksDistance(tankPos, Position) >= PATH_TRACE_MIN_DIST.ToResolutionX()) {
                    // GameHandler.ClientLog.Write("One Loop:", LogType.Info);
                    for (int i = 1; i < DotCount; i++) {
                        var curDrawPos = Vector2.Lerp(tankPos, Position, (float)i / DotCount);

                        TankGame.SpriteRenderer.Draw(DotTexture, curDrawPos, null, Color.White, 0f, Anchor.Center.GetAnchor(DotTexture.Size()), new Vector2(0.35f).ToResolution(), default, default);
                    }
                }
            }
        }

        if (HasTrail) {
            CursorTrail!.StartWidth = 10f.ToResolutionF() + _scaleOscillation;

            CursorTrail?.Update(Position);
            CursorTrail?.Draw();
            /*if (!float.IsInfinity(RuntimeData.DeltaTime)) {
                if (RuntimeData.RunTime % 60 <= RuntimeData.DeltaTime) {
                    var newVal = (int)(TRAIL_POINTS_BASE / RuntimeData.DeltaTime);

                    Console.WriteLine(newVal);
                    if (newVal > 0) {
                        CursorTrail.MaxTrailPoints = newVal;
                    }
                }
            }*/
        }

        TankGame.SpriteRenderer.Draw(MouseTexture, Position, null, Color.White, Rotation, MouseTexture.Size() / 2, scaleReal.ToResolution(), default, default);

        TankGame.SpriteRenderer.End();
    }
}