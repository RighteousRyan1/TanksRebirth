using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.ModSupport;
using TanksRebirth.Internals.Common.GameUI;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.UI.MainMenu; 

// TODO: make this fully functional next!
public partial class MainMenuUI {
    // button to press to enter the mods menu
    public static UITextButton ModsMenuButton;
    public static UITextButton ModsMenuLeave;
    public static UITextButton ReloadMods;

    static Dictionary<string, Texture2D> _icons = [];
    static Dictionary<string, UITextButton> _buttons = [];
    static Dictionary<string, Color> _averageColors = [];
    public static void InitModsMenu(SpriteFontBase font) {
        ModsMenuButton = new(TankGame.GameLanguage.Mods, font, Color.WhiteSmoke) {
            IsVisible = false,
        };
        ModsMenuButton.SetDimensions(() => new Vector2(WindowUtils.WindowWidth - 250.ToResolutionX(), 10.ToResolutionY()), () => new Vector2(240, 50).ToResolution());
        ModsMenuButton.OnLeftClick = (a) => {
            MenuState = UIState.ModsMenu;

            SetModsMenuButtonsVisiblity(true);

            InitModMenuGraphics(TankGame.Instance.GraphicsDevice);
        };

        ModsMenuLeave = new(TankGame.GameLanguage.Back, font, Color.WhiteSmoke) {
            IsVisible = false,
        };
        ModsMenuLeave.SetDimensions(() => new Vector2(WindowUtils.WindowWidth - 250.ToResolutionX(), 10.ToResolutionY()), () => new Vector2(240, 50).ToResolution());
        ModsMenuLeave.OnLeftClick = (a) => {
            MenuState = UIState.PrimaryMenu;

            SetModsMenuButtonsVisiblity(false);

            DisposeModMenuGraphics();
        };

        ReloadMods = new(TankGame.GameLanguage.ReloadMods, font, Color.WhiteSmoke) {
            IsVisible = false,
        };
        ReloadMods.SetDimensions(() => new Vector2(WindowUtils.WindowWidth - 250.ToResolutionX(), 70.ToResolutionY()), () => new Vector2(240, 50).ToResolution());
        ReloadMods.OnLeftClick = (a) => {
            ModLoader.LoadMods();
        };
    }
    public static void InitModMenuGraphics(GraphicsDevice device) {
        float uiStartX = 50f;
        float uiStartY = 50f;

        float uiScaleX = 400f;
        float uiScaleY = 100f;

        for (int i = 0; i < ModLoader.modNames.Count; i++) {
            var dir = Path.Combine(ModLoader.ModsPath, ModLoader.modNames[i]);
            var modInternalName = new DirectoryInfo(dir).Name;
            var iCapture = i;

            var modInfoJson = Path.Combine(dir, "mod_info.json");
            var json = File.ReadAllText(modInfoJson);
            ModInfo info = JsonSerializer.Deserialize<ModInfo>(json);

            var iconPath = Path.Combine(dir, "mod_icon.png");
            var iconExists = File.Exists(iconPath);

            if (iconExists)
                _icons.Add(dir, Texture2D.FromFile(device, iconPath));
            else
                _icons.Add(dir, TextureGlobals.Pixels[Color.Magenta]);

            _averageColors.Add(dir, ColorUtils.GetAverageColor(_icons[dir]));

            var btn = new UITextButton(string.Empty, FontGlobals.RebirthFont, Color.AliceBlue) {
                HoverColor = Color.CadetBlue
            };

            var yOffset = 125 * i;
            btn.SetDimensions(() => new Vector2(uiStartX, uiStartY + yOffset).ToResolution(), () => new Vector2(uiScaleX, uiScaleY).ToResolution());
            btn.UniqueDraw = (a, sb) => {
                // to prevent status changing while mods are loading
                btn.IsVisible = !ModLoader.IsLoadingMods;
                var curButton = _buttons[dir];
                var curTex = _icons[dir];

                sb.Draw(curTex, new Rectangle((int)curButton.Position.X + 5, (int)curButton.Position.Y + 5, (int)90.ToResolutionX(), (int)btn.Size.Y - 10), Color.White);

                var borderColor = ColorUtils.ChangeColorBrightness(_averageColors[dir], -0.5f);
                var textColor = _averageColors[dir];

                var enabled = ModLoader.modEnablement[iCapture];

                DrawUtils.DrawStringWithBorderAndShadow(sb, btn.Font, btn.Position + new Vector2(100, 0).ToResolution(), Vector2.One, info.DisplayName,
                    textColor, borderColor, new Vector2(0.75f).ToResolution(), 1f, Anchor.TopLeft, shadowDistScale: 0.5f, borderThickness: 0.5f);

                DrawUtils.DrawStringWithBorder(sb, btn.Font, info.BriefDescription, btn.Position + new Vector2(100, 25).ToResolution(),
                    textColor, borderColor, new Vector2(0.6f).ToResolution(), 0, Anchor.TopLeft, borderThickness: 0.5f);
                var status = TankGame.GameLanguage.Status + ": " + (enabled? TankGame.GameLanguage.Enabled : TankGame.GameLanguage.Disabled);
                var statusColor = enabled ? Color.Lime : Color.Red;
                DrawUtils.DrawStringWithBorder(sb, btn.Font, status, btn.Position + new Vector2(100, 75).ToResolution(),
                    statusColor, ColorUtils.ChangeColorBrightness(statusColor, -0.5f), new Vector2(0.6f).ToResolution(), 0, Anchor.TopLeft, borderThickness: 0.5f);
            };
            btn.OnLeftClick = (a) => {
                ModLoader.modEnablement[iCapture] = !ModLoader.modEnablement[iCapture];
            };

            _buttons.Add(dir, btn);
        }
    }

    public static void DisposeModMenuGraphics() {
        foreach (var icon in _icons) {
            icon.Value?.Dispose();
        }
        _icons.Clear();

        foreach (var button in _buttons) {
            button.Value.Remove();
        }
        _buttons.Clear();
        for (int i = 0; i < _icons.Count; i++) {
            var elem = _icons.ElementAt(i);
            elem.Value.Dispose();
        }
        _icons.Clear();
        _averageColors.Clear();
    }

    public static void SetModsMenuButtonsVisiblity(bool visible) {
        ModsMenuLeave.IsVisible = visible;
        ReloadMods.IsVisible = visible;
    }
}
