using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace TanksRebirth.GameContent.UI.LevelEditor; 
public static partial class LevelEditorUI {
    public static Dictionary<string, Texture2D> RenderTextures = new();
    private static float _barOffset;
    private static Vector2 _origClick;
    private static float _maxScroll;
    private static List<string> _renderNamesTanks = [];
    private static List<string> _renderNamesBlocks = [];
    private static List<string> _renderNamesPlayers = [];
}
