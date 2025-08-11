using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.ModSupport;
using TanksRebirth.Internals.Common.GameUI;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.UI.MainMenu; 
public partial class MainMenuUI {
    // button to press to enter the mods menu
    public static UITextButton ModsMenuButton;
    public static UITextButton ModsMenuLeave;
    public static UITextButton ReloadMods;

    static Dictionary<TanksMod, Texture2D> _icons = [];
    static Dictionary<TanksMod, UITextButton> _buttons = [];
    static Dictionary<TanksMod, Color> _averageColors = [];
    public static void InitModsMenu(SpriteFontBase font) {
        ModsMenuButton = new(TankGame.GameLanguage.Mods, font, Color.WhiteSmoke) {
            IsVisible = false,
        };
        ModsMenuButton.SetDimensions(() => new Vector2(WindowUtils.WindowWidth - 250, 10), () => new Vector2(240, 50));
        ModsMenuButton.OnLeftClick = (a) => {
            MenuState = UIState.ModsMenu;

            SetModsMenuButtonsVisiblity(true);

            InitModMenuGraphics(TankGame.Instance.GraphicsDevice);
        };

        ModsMenuLeave = new(TankGame.GameLanguage.Back, font, Color.WhiteSmoke) {
            IsVisible = false,
        };
        ModsMenuLeave.SetDimensions(() => new Vector2(WindowUtils.WindowWidth - 250, 10.ToResolutionY()), () => new Vector2(240, 50).ToResolution());
        ModsMenuLeave.OnLeftClick = (a) => {
            MenuState = UIState.PrimaryMenu;

            SetModsMenuButtonsVisiblity(false);

            DisposeModMenuGraphics();
        };

        ReloadMods = new(TankGame.GameLanguage.ReloadMods, font, Color.WhiteSmoke) {
            IsVisible = false,
        };
        ReloadMods.SetDimensions(() => new Vector2(WindowUtils.WindowWidth - 250, 70.ToResolutionY()), () => new Vector2(240, 50).ToResolution());
        ReloadMods.OnLeftClick = (a) => {
            ModLoader.LoadMods();
        };
    }

    public static void InitModMenuGraphics(GraphicsDevice device) {
        float uiStartX = 50f;
        float uiStartY = 50f;

        float uiScaleX = 400f;
        float uiScaleY = 100f;
        for (int i = 0; i < ModLoader.modDirs.Count; i++) {
            var dir = ModLoader.modDirs.ElementAt(i);

            var mod = dir.Key;
            var iconPath = Path.Combine(dir.Value, "mod_icon.png");
            var iconExists = File.Exists(iconPath);

            if (iconExists)
                _icons.Add(mod, Texture2D.FromFile(device, iconPath));
            else
                _icons.Add(mod, TextureGlobals.Pixels[Color.Magenta]);

            _averageColors.Add(mod, ColorUtils.GetAverageColor(_icons[mod]));

            var btn = new UITextButton(string.Empty, FontGlobals.RebirthFont, Color.AliceBlue) {
                HoverColor = Color.CadetBlue
            };

            var yOffset = 125 * i;
            btn.SetDimensions(() => new Vector2(uiStartX, uiStartY + yOffset).ToResolution(), () => new Vector2(uiScaleX, uiScaleY).ToResolution());
            btn.UniqueDraw = (a, sb) => {
                var curButton = _buttons[mod];
                var curTex = _icons[mod];

                sb.Draw(curTex, new Rectangle((int)curButton.Position.X + 5, (int)curButton.Position.Y + 5, (int)90.ToResolutionX(), (int)btn.Size.Y - 10), Color.White);

                var borderColor = ColorUtils.ChangeColorBrightness(_averageColors[mod], -0.5f);
                var textColor = _averageColors[mod];

                DrawUtils.DrawStringWithBorderAndShadow(sb, btn.Font, btn.Position + new Vector2(100, 0).ToResolution(), Vector2.One, mod.ModInfo.DisplayName,
                    textColor, borderColor, new Vector2(0.75f).ToResolution(), 1f, Anchor.TopLeft, shadowDistScale: 0.5f, borderThickness: 0.5f);

                DrawUtils.DrawStringWithBorder(sb, btn.Font, mod.ModInfo.BriefDescription, btn.Position + new Vector2(100, 25).ToResolution(),
                    textColor, borderColor, new Vector2(0.6f).ToResolution(), 0, Anchor.TopLeft, borderThickness: 0.5f);
                var status = TankGame.GameLanguage.Status + ": " + (ModLoader.ModsEnabled[mod.InternalName] ? TankGame.GameLanguage.Enabled : TankGame.GameLanguage.Disabled);
                var statusColor = ModLoader.ModsEnabled[mod.InternalName] ? Color.Lime : Color.Red;
                DrawUtils.DrawStringWithBorder(sb, btn.Font, status, btn.Position + new Vector2(100, 75).ToResolution(),
                    statusColor, ColorUtils.ChangeColorBrightness(statusColor, -0.5f), new Vector2(0.6f).ToResolution(), 0, Anchor.TopLeft, borderThickness: 0.5f);
            };
            btn.OnLeftClick = (a) => {
                ModLoader.ModsEnabled[mod.InternalName] = !ModLoader.ModsEnabled[mod.InternalName];
            };

            _buttons.Add(mod, btn);
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
        _averageColors.Clear();
    }

    public static void SetModsMenuButtonsVisiblity(bool visible) {
        ModsMenuLeave.IsVisible = visible;
        ReloadMods.IsVisible = visible;
    }
}
