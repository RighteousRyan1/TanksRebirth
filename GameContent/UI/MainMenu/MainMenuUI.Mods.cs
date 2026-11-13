using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TanksRebirth.GameContent.ModSupport;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.GameUI;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals.UI;

namespace TanksRebirth.GameContent.UI.MainMenu;

public partial class MainMenuUI {
    // button to press to enter the mods menu
    public static UITextButton ModsMenuButton;

    // layout
    internal const float ModsPanelX = 40, ModsPanelY = 30, ModsPanelW = 860, ModsPanelH = 1020;
    internal const float ModsLeftX = 72, ModsRightX = 868;
    internal const float ModsTitleY = 46, ModsTitleH = 54;
    internal const float ModsListX = 60, ModsListY = 116, ModsListW = 820, ModsListH = 740;
    internal const float ModsInfoY = 890, ModsInfoH = 128;

    /// <summary>Every folder in the Mods folder, in the order they are listed in the menu.</summary>
    internal static readonly List<ModMenuEntry> ModEntries = [];
    /// <summary>The entry the mouse is over, -1 for n/a.</summary>
    internal static int ModsHovered = -1;

    static readonly List<UIElement> _modsMenuElements = [];
    static ModsList? _modsList;

    // clicks are handled while the UI is updating, so anything that removes elements waits until the next UpdateModsMenu
    static Action? _pendingModsAction;
    static int _previousScrollValue;

    public static void InitModsMenu(SpriteFontBase font) {
        ModsMenuButton = new(TankGame.GameLanguage.Misc.Mods, font, Color.WhiteSmoke) {
            IsVisible = false,
        };
        ModsMenuButton.SetDimensions(() => new Vector2(WindowUtils.WindowWidth - 250.ToResolutionX(), 10.ToResolutionY()), () => new Vector2(240, 50).ToResolution());
        ModsMenuButton.OnLeftClick = (a) => {
            MenuState = UIState.ModsMenu;
            InitModMenuGraphics(TankGame.Instance.GraphicsDevice);
        };
    }

    public static void UpdateModsMenu() {
        if (_pendingModsAction is { } pending) {
            _pendingModsAction = null;
            pending();
            if (MenuState != UIState.ModsMenu)
                return;
        }

        var wheel = Mouse.GetState().ScrollWheelValue;
        var scrollDelta = wheel - _previousScrollValue;
        _previousScrollValue = wheel;

        // the loading screen is drawn over everything while mods load
        var busy = ModLoader.IsLoadingMods;
        foreach (var element in _modsMenuElements)
            element.IsVisible = !busy;

        if (busy || _modsList is null)
            return;

        _modsList.ScrollBy(-scrollDelta * 0.5f); // sensitivity
        _modsList.Tick();
    }

    public static void InitModMenuGraphics(GraphicsDevice device) {
        DisposeModMenuGraphics();
        _previousScrollValue = Mouse.GetState().ScrollWheelValue;

        foreach (var modName in ModLoader.modNames)
            ModEntries.Add(CreateModEntry(device, modName));

        AddModsElement(new ModifiersFrame(), ModsPanelX, ModsPanelY, ModsPanelW, ModsPanelH);

        const float backW = 150, reloadW = 230, gap = 12;
        const float backX = ModsRightX - backW;
        const float reloadX = backX - gap - reloadW;

        AddModsElement(new ModsTitleBar(), ModsLeftX, ModsTitleY, reloadX - gap - ModsLeftX, ModsTitleH);
        AddModsElement(new ModsPillButton(() => TankGame.GameLanguage.Misc.ReloadMods, () => _pendingModsAction = ReloadModsFromMenu), reloadX, ModsTitleY, reloadW, ModsTitleH);
        AddModsElement(new ModsPillButton(() => TankGame.GameLanguage.Menu.Back, () => _pendingModsAction = LeaveModsMenu), backX, ModsTitleY, backW, ModsTitleH);

        _modsList = new ModsList();
        AddModsElement(_modsList, ModsListX, ModsListY, ModsListW, ModsListH);
        AddModsElement(new ModsInfoBar(), ModsLeftX, ModsInfoY, ModsRightX - ModsLeftX, ModsInfoH);
    }

    static void AddModsElement(UIElement element, float x, float y, float width, float height) {
        element.SetDimensions(() => new Vector2(x, y).ToResolution(), () => new Vector2(width, height).ToResolution());
        element.IsVisible = !ModLoader.IsLoadingMods;
        _modsMenuElements.Add(element);
    }

    static ModMenuEntry CreateModEntry(GraphicsDevice device, string modName) {
        var dir = Path.Combine(ModLoader.ModsPath, modName);
        var loadable = ModLoader.HasModProject(modName);

        // folders without a mod_info.json still show up, under their folder name
        var info = default(ModInfo);
        var modInfoJson = Path.Combine(dir, "mod_info.json");
        if (File.Exists(modInfoJson)) {
            try {
                info = JsonSerializer.Deserialize<ModInfo>(File.ReadAllText(modInfoJson));
            } catch (Exception e) {
                TankGame.ClientLog.Write($"Could not read mod_info.json of '{modName}': {e.Message}", RebirthLogLevel.Warn);
            }
        }

        var displayName = string.IsNullOrWhiteSpace(info.DisplayName) ? modName : info.DisplayName;
        var description = info.BriefDescription ?? string.Empty;
        if (string.IsNullOrEmpty(description) && !loadable)
            description = "No .csproj file found, so this folder is skipped when loading mods.";

        Texture2D? icon = null;
        var accent = Color.Gold;
        var iconPath = Path.Combine(dir, "mod_icon.png");
        if (File.Exists(iconPath)) {
            icon = Texture2D.FromFile(device, iconPath);
            accent = ColorUtils.GetAverageColor(icon) with { A = 255 };

            // a near black icon would leave the stripe invisible
            if (Math.Max(accent.R, Math.Max(accent.G, accent.B)) < 60)
                accent = Color.Gold;
        }

        return new ModMenuEntry(modName, displayName, description, info.Version ?? string.Empty, icon, accent, loadable);
    }

    static void ReloadModsFromMenu() {
        ModLoader.LoadMods();

        // LoadMods lists the folders again, so show what it found
        if (MenuState == UIState.ModsMenu)
            InitModMenuGraphics(TankGame.Instance.GraphicsDevice);
    }

    static void LeaveModsMenu() {
        MenuState = UIState.PrimaryMenu;
        DisposeModMenuGraphics();
    }

    public static void DisposeModMenuGraphics() {
        foreach (var entry in ModEntries)
            entry.Icon?.Dispose();
        ModEntries.Clear();
        ModsHovered = -1;

        foreach (var element in _modsMenuElements)
            element.Remove();
        _modsMenuElements.Clear();
        _modsList = null;
    }
}
