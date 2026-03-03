using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Localization;

namespace TanksRebirth.GameContent.UI.MainMenu;

#pragma warning disable
public static class CreditsHandler {
    static string _fmt;
    static IEnumerable<string> _lines;

    static float _yOffset;

    public static void Load(Language lang) {
        var text = File.ReadAllText("Localization/credits.txt");
        _fmt = string.Format(text, 
            lang.Credits.Credits, lang.Credits.DevelopedBy, 
            lang.Credits.Programming, lang.Credits.GraphicsDesign, 
            lang.Credits.Contributors, lang.Credits.SpecialThanks, lang.Credits.Shoutouts);

        // now they can be drawn propery
        // excludes comments
        _lines = _fmt.Replace("\r", string.Empty)
            .Split('\n')
            .Where(l => !l.StartsWith("//"));

        _yOffset = WindowUtils.WindowHeight;
    }

    public static void Unload() {
        _fmt = null;
        _lines = null;
        _yOffset = 0;
    }

    public static void Draw(SpriteBatch sb, SpriteFontBase font, float originX) {
        float currentY = _yOffset;
        float baseSpacing = 75.0f;

        float totalHeight = 0f;

        foreach (var line in _lines) {
            if (string.IsNullOrWhiteSpace(line)) {
                // padding
                currentY += baseSpacing * 0.5f;
                totalHeight += baseSpacing * 0.5f;
                continue;
            }

            var textEff = line[0];
            float scale = 0.75f;
            Color mainColor = Color.White;
            Color borderColor = Color.Black;
            bool hasPrefix = false;

            switch (textEff) {
                case '$': // v. big
                    hasPrefix = true;

                    scale = 1.6f;
                    break;
                case '#': // big
                    hasPrefix = true;

                    scale = 1.2f;
                    break;
                case '@': // small
                    hasPrefix = true;

                    scale = 0.50f;
                    break;

                case '~': // rainbow
                    hasPrefix = true;

                    mainColor = ColorUtils.DiscoPartyColor;
                    break;

                case '.': // border rainbow
                    hasPrefix = true;

                    borderColor = ColorUtils.DiscoPartyColor;
                    break;

                case '*': // v. big border rainbow
                    hasPrefix = true;

                    scale = 2f;
                    borderColor = ColorUtils.DiscoPartyColor;
                    break;
                case ';': // big border rainbow
                    hasPrefix = true;

                    scale = 1.2f;
                    borderColor = ColorUtils.DiscoPartyColor;
                    break;
            }

            string textToDraw = hasPrefix ? line[1..] : line;
            Vector2 position = new(originX, currentY);
            Vector2 scaling = new(scale);

            DrawUtils.DrawStringWithBorder(sb, font, textToDraw, position, mainColor, borderColor, scaling, 0f, Anchor.Center, borderThickness: scale);

            currentY += baseSpacing * scale;
            totalHeight += baseSpacing * scale;
        }
        _yOffset -= 0.8f * RuntimeData.DeltaTime;

        float stopPoint = -totalHeight + (TankGame.Instance.GraphicsDevice.Viewport.Height / 2f);

        float stopYOff = WindowUtils.WindowHeight / 2;
        if (_yOffset < stopPoint + stopYOff) {
            _yOffset = stopPoint + stopYOff;
        }
    }
}
