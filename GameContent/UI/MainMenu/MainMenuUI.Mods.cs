using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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

public partial class MainMenuUI {
    // button to press to enter the mods menu
    public static UITextButton ModsMenuButton;
    public static UITextButton ModsMenuLeave;
    public static UITextButton ReloadMods;

    static List<Texture2D> _icons = [];
    static List<UITextButton> _buttons = [];
    static List<Color> _averageColors = [];

    // scrolling stuff
    private static float _currentScroll = 0f;
    private static float _targetScroll = 0f;
    private static int _previousScrollValue;

    // layout config
    private const float ENTRY_HEIGHT = 125f;
    private const float LIST_TOP_MARGIN = 150f;
    private const float LIST_BOTTOM_MARGIN = 50f;
    private static Rectangle _listViewRect;

    public static void InitModsMenu(SpriteFontBase font) {
        ModsMenuButton = new(TankGame.GameLanguage.Misc.Mods, font, Color.WhiteSmoke) {
            IsVisible = false,
        };
        ModsMenuButton.SetDimensions(() => new Vector2(WindowUtils.WindowWidth - 250.ToResolutionX(), 10.ToResolutionY()), () => new Vector2(240, 50).ToResolution());
        ModsMenuButton.OnLeftClick = (a) => {
            MenuState = UIState.ModsMenu;
            SetModsMenuButtonsVisiblity(true);
            InitModMenuGraphics(TankGame.Instance.GraphicsDevice);
        };

        ModsMenuLeave = new(TankGame.GameLanguage.Menu.Back, font, Color.WhiteSmoke) {
            IsVisible = false,
        };
        ModsMenuLeave.SetDimensions(() => new Vector2(WindowUtils.WindowWidth - 250.ToResolutionX(), 10.ToResolutionY()), () => new Vector2(240, 50).ToResolution());
        ModsMenuLeave.OnLeftClick = (a) => {
            MenuState = UIState.PrimaryMenu;
            SetModsMenuButtonsVisiblity(false);
            DisposeModMenuGraphics();
        };

        ReloadMods = new(TankGame.GameLanguage.Misc.ReloadMods, font, Color.WhiteSmoke) {
            IsVisible = false,
        };
        ReloadMods.SetDimensions(() => new Vector2(WindowUtils.WindowWidth - 250.ToResolutionX(), 70.ToResolutionY()), () => new Vector2(240, 50).ToResolution());
        ReloadMods.OnLeftClick = (a) => {
            ModLoader.LoadMods();
        };
    }

    public static void UpdateModsMenu() {
        // scroll input
        var mouseState = Mouse.GetState();
        int scrollDelta = mouseState.ScrollWheelValue - _previousScrollValue;
        _previousScrollValue = mouseState.ScrollWheelValue;

        if (ModLoader.IsLoadingMods) return;

        // view area
        float screenHeight = WindowUtils.WindowHeight;
        float viewHeight = screenHeight - LIST_TOP_MARGIN.ToResolutionY() - LIST_BOTTOM_MARGIN.ToResolutionY();
        _listViewRect = new Rectangle(0, (int)LIST_TOP_MARGIN.ToResolutionY(), WindowUtils.WindowWidth, (int)viewHeight);

        // scroll target
        float totalContentHeight = _buttons.Count * ENTRY_HEIGHT.ToResolutionY();
        float maxScroll = Math.Max(0, totalContentHeight - viewHeight);

        _targetScroll -= scrollDelta * 0.5f; // Sensitivity
        _targetScroll = MathHelper.Clamp(_targetScroll, 0, maxScroll);

        // scroll interp
        _currentScroll = MathUtils.SoftStep(_currentScroll, _targetScroll, 0.2f);

        // buttons / visibility
        for (int i = 0; i < _buttons.Count; i++) {
            var btn = _buttons[i];

            // supposed draw pos
            float btnY = (LIST_TOP_MARGIN.ToResolutionY() + (i * ENTRY_HEIGHT.ToResolutionY())) - _currentScroll;
            float btnHeight = 100f.ToResolutionY();

            // maybe just use scissors?
            bool isVisible = (btnY + btnHeight > _listViewRect.Top) && (btnY < _listViewRect.Bottom);

            btn.IsVisible = isVisible;
        }
    }

    public static void InitModMenuGraphics(GraphicsDevice device) {
        float uiStartX = 50f;
        float uiScaleX = 400f;
        float uiScaleY = 100f;

        // resets scroll
        _currentScroll = 0;
        _targetScroll = 0;
        _previousScrollValue = Mouse.GetState().ScrollWheelValue;

        for (int i = 0; i < ModLoader.modNames.Count; i++) {
            var dir = Path.Combine(ModLoader.ModsPath, ModLoader.modNames[i]);
            var modInternalName = new DirectoryInfo(dir).Name;
            var iCapture = i;

            var modInfoJson = Path.Combine(dir, "mod_info.json");
            ModInfo info = default;

            if (File.Exists(modInfoJson)) {
                var json = File.ReadAllText(modInfoJson);
                info = JsonSerializer.Deserialize<ModInfo>(json);
            }
            else {
                info = new ModInfo();
            }

            var iconPath = Path.Combine(dir, "mod_icon.png");
            var iconExists = File.Exists(iconPath);

            if (iconExists)
                _icons.Add(Texture2D.FromFile(device, iconPath));
            else
                _icons.Add(TextureGlobals.Pixels[Color.Magenta]);

            _averageColors.Add(ColorUtils.GetAverageColor(_icons[i]));

            var btn = new UITextButton(string.Empty, FontGlobals.RebirthFont, Color.AliceBlue) {
                HoverColor = Color.CadetBlue
            };

            // positioning
            btn.SetDimensions(
                () => {
                    float yPos = LIST_TOP_MARGIN.ToResolutionY() + (iCapture * ENTRY_HEIGHT.ToResolutionY()) - _currentScroll;
                    return new Vector2(uiStartX.ToResolutionX(), yPos);
                },
                () => new Vector2(uiScaleX, uiScaleY).ToResolution()
            );

            btn.UniqueDraw = (a, sb) => {
                if (ModLoader.IsLoadingMods || iCapture >= _buttons.Count) return;

                var curButton = _buttons[iCapture];
                var curTex = _icons[iCapture];

                // icon
                sb.Draw(curTex, new Rectangle((int)curButton.Position.X + 5, (int)curButton.Position.Y + 5, (int)90.ToResolutionX(), (int)btn.Size.Y - 10), Color.White);

                var borderColor = ColorUtils.ChangeColorBrightness(_averageColors[iCapture], -0.5f);
                var textColor = _averageColors[iCapture];
                var enabled = ModLoader.modEnablement[iCapture];

                // name
                DrawUtils.DrawStringWithBorderAndShadow(sb, btn.Font, btn.Position + new Vector2(100, 0).ToResolution(), Vector2.One, info.DisplayName,
                    textColor, borderColor, new Vector2(0.75f).ToResolution(), 1f, Anchor.TopLeft, shadowDistScale: 0.5f, borderThickness: 0.5f);

                // desc
                DrawUtils.DrawStringWithBorder(sb, btn.Font, info.BriefDescription, btn.Position + new Vector2(100, 25).ToResolution(),
                    textColor, borderColor, new Vector2(0.6f).ToResolution(), 0, Anchor.TopLeft, borderThickness: 0.5f);

                // Draw Status
                var status = TankGame.GameLanguage.Basic.Status + ": " + TankGame.GameLanguage.GetEnablement(enabled);
                var statusColor = enabled ? Color.Lime : Color.Red;
                DrawUtils.DrawStringWithBorder(sb, btn.Font, status, btn.Position + new Vector2(100, 75).ToResolution(),
                    statusColor, ColorUtils.ChangeColorBrightness(statusColor, -0.5f), new Vector2(0.6f).ToResolution(), 0, Anchor.TopLeft, borderThickness: 0.5f);
            };

            btn.OnLeftClick = (a) => {
                ModLoader.modEnablement[iCapture] = !ModLoader.modEnablement[iCapture];
            };

            _buttons.Add(btn);
        }
    }

    public static void DrawModMenu(SpriteBatch sb) {
        if (ModLoader.IsLoadingMods) return;

        // bg panel
        var panelRect = new Rectangle(
            (int)25.ToResolutionX(),
            (int)_listViewRect.Top - 10,
            (int)(450.ToResolutionX()),
            (int)_listViewRect.Height + 20
        );

        sb.Draw(TextureGlobals.Pixels[Color.White], panelRect, new Color(20, 20, 25) * 0.85f);
        DrawUtils.DrawBoxWithOutline(sb, panelRect, Color.Transparent, Color.Gray, 2);

        // scrollbar, if applicable
        float totalHeight = _buttons.Count * ENTRY_HEIGHT.ToResolutionY();
        float viewHeight = _listViewRect.Height;

        if (totalHeight > viewHeight) {
            float scrollRatio = _currentScroll / (totalHeight - viewHeight);
            float barHeight = (viewHeight / totalHeight) * viewHeight;
            float barY = _listViewRect.Top + (scrollRatio * (viewHeight - barHeight));

            var trackRect = new Rectangle(panelRect.Right - 15, (int)_listViewRect.Top, 10, (int)viewHeight);
            var thumbRect = new Rectangle(panelRect.Right - 15, (int)barY, 10, (int)Math.Max(20, barHeight));

            sb.Draw(TextureGlobals.Pixels[Color.White], trackRect, new Color(10, 10, 10) * 0.5f);
            sb.Draw(TextureGlobals.Pixels[Color.White], thumbRect, Color.Goldenrod);
        }
    }

    public static void DisposeModMenuGraphics() {
        foreach (var icon in _icons) icon?.Dispose();
        _icons.Clear();
        foreach (var button in _buttons) button.Remove();
        _buttons.Clear();
        _averageColors.Clear();
    }

    public static void SetModsMenuButtonsVisiblity(bool visible) {
        ModsMenuLeave.IsVisible = visible;
        ReloadMods.IsVisible = visible;
    }
}