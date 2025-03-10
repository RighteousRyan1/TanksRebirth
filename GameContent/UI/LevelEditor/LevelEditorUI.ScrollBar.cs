using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.UI.LevelEditor; 
public static partial class LevelEditorUI {
    public static Dictionary<string, Texture2D> RenderTextures = [];
    private static float _barOffset;
    private static Vector2 _origClick;
    private static float _maxScroll;
    private static List<string> _renderNamesTanks = [];
    private static List<string> _renderNamesBlocks = [];
    private static List<string> _renderNamesPlayers = [];

    public static void DrawTankDescriptionFlavor() {
        var measure = TankGame.TextFont.MeasureString(_curDescription);

        if (_curDescription != null && _curDescription != string.Empty) {
            int padding = 20;
            var orig = new Vector2(0, TextureGlobals.Pixels[Color.White].Size().Y);
            TankGame.SpriteRenderer.Draw(TextureGlobals.Pixels[Color.White],
                new Rectangle((int)(WindowUtils.WindowWidth / 2 - (measure.X / 2 + padding).ToResolutionX()), (int)(WindowUtils.WindowHeight * 0.8f), (int)(measure.X + padding * 2).ToResolutionX(), (int)(measure.Y + 20).ToResolutionY()),
                null,
                Color.White,
                0f,
                orig,
                default,
                0f);
            TankGame.SpriteRenderer.DrawString(TankGame.TextFont, _curDescription, new Vector2(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight * 0.78f), Color.Black, Vector2.One.ToResolution(), 0f, new Vector2(measure.X / 2, measure.Y));
        }
    }
}
