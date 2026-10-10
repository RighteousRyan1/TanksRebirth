using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.GameUI;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;
using TanksRebirth.Internals.UI;

namespace TanksRebirth.GameContent.UI.MainMenu;

#pragma warning disable

public static partial class MainMenuUI {
    static bool _diffButtonsInitialized;

    /// <summary>Every element of the modifiers window (frame and all pages).</summary>
    public static List<UIElement> AllDifficultyButtons = [];
    /// <summary>Every modifier row, on every page.</summary>
    public static List<ModifierRow> AllModifierRows = [];

    /// <summary>Whether the modifiers window is on screen.</summary>
    public static bool ModifiersOpen { get; private set; }
    /// <summary>The page on screen (0 based).</summary>
    public static int ModifiersPage { get; private set; }
    public static int ModifiersPageCount => Math.Max(1, _modPages.Count);

    // layout...
    internal const float ModPanelX = 150, ModPanelY = 30, ModPanelW = 1620, ModPanelH = 820;
    internal const float ModLeftX = 190, ModColumnW = 493, ModColumnGap = 30;
    internal const float ModTitleY = 44, ModTitleH = 54;
    internal const float ModFirstSlotY = 114, ModSlotStep = 54, ModRowH = 48;
    internal const float ModDescY = 716, ModDescH = 116;
    internal const int ModColumns = 3, ModSlotsPerColumn = 11;

    static float ModColumnX(int column) => ModLeftX + column * (ModColumnW + ModColumnGap);
    static float ModSlotY(int slot) => ModFirstSlotY + slot * ModSlotStep;
    static float ModSpanWidth(int columns) => columns * ModColumnW + (columns - 1) * ModColumnGap;

    static UIPanel _modPanel;
    static readonly List<UIElement> _modFrame = [];
    static readonly List<UIElement> _modPagers = [];
    static readonly List<List<UIElement>> _modPages = [];
    static int _builtRegistryVersion = -1;

    static uint _modsOpenedAt;
    static int _sentValuesVersion = -1;
    static int _sentPeerCount = -1;

    /// <summary>Ignores the click that opened the window or switched the page (it lands on the same frame the rows appear).</summary>
    internal static bool ModifiersInputReady => RuntimeData.UpdateCount - _modsOpenedAt > 5;

    /// <summary>Only the host (or a single player) can change modifiers.</summary>
    internal static bool CanEditModifiers => !Client.IsConnected() || Client.IsHost();

    // TODO: UI Layers. This is fucking ugly.
    internal static void SetDifficultiesButtonsVisibility(bool visible) {
        if (visible && !ModifiersOpen)
            _modsOpenedAt = RuntimeData.UpdateCount;
        ModifiersOpen = visible;

        foreach (var element in _modFrame)
            element.IsVisible = visible;
        foreach (var element in _modPagers)
            element.IsVisible = visible && _modPages.Count > 1;
        for (int i = 0; i < _modPages.Count; i++)
            foreach (var element in _modPages[i])
                element.IsVisible = visible && i == ModifiersPage;
    }

    public static void UpdateDifficulties() {
        if (_builtRegistryVersion != Modifiers.RegistryVersion)
            BuildModifiersWindow();

        if (IsActive && Client.IsConnected() && Client.IsHost()) {
            var peers = Server.NetManager?.ConnectedPeersCount ?? 0;
            if (Modifiers.ValuesVersion != _sentValuesVersion || peers != _sentPeerCount) {
                _sentValuesVersion = Modifiers.ValuesVersion;
                _sentPeerCount = peers;
                Client.SendDiffiulties();
            }
        }

        if (MenuState != UIState.Modifiers) return;

        if (_modPages.Count > 1 && UITextInput.currentActiveBox == -1 && !ChatSystem.ActiveHandle) {
            if (InputUtils.KeyJustPressed(Keys.Q)) {
                CycleModifiersPage(-1);
                SettingsUI.PlayTick();
            }
            else if (InputUtils.KeyJustPressed(Keys.E)) {
                CycleModifiersPage(1);
                SettingsUI.PlayTick();
            }
        }
    }

    /// <summary>The next (1) or previous (-1) page of the modifiers window, wrapping around.</summary>
    public static void CycleModifiersPage(int direction) {
        if (_modPages.Count < 2)
            return;
        ModifiersPage = ((ModifiersPage + direction) % _modPages.Count + _modPages.Count) % _modPages.Count;
        _modsOpenedAt = RuntimeData.UpdateCount;
        SetDifficultiesButtonsVisibility(ModifiersOpen);
    }

    /// <summary>Turns every modifier off.</summary>
    public static void ResetAllModifiers() => Modifiers.ResetAll();

    // initialization
    static void InitializeDifficultyButtons() {
        _diffButtonsInitialized = true;
        BuildModifiersWindow();
    }

    static void ArrangeDifficultyButtons() {
        if (!_diffButtonsInitialized || _builtRegistryVersion != Modifiers.RegistryVersion)
            InitializeDifficultyButtons();
    }

    static void BuildModifiersWindow() {
        foreach (var element in AllDifficultyButtons)
            element.Remove();
        AllDifficultyButtons.Clear();
        AllModifierRows.Clear();
        _modFrame.Clear();
        _modPagers.Clear();
        _modPages.Clear();
        _builtRegistryVersion = Modifiers.RegistryVersion;

        _modPanel = new UIPanel((_, _) => { }) {
            BackgroundColor = Color.Black * 0.55f,
            IgnoreMouseInteractions = true,
        };
        AddModElement(_modFrame, _modPanel, ModPanelX, ModPanelY, ModPanelW, ModPanelH);

        var right = ModPanelX + ModPanelW - 40;
        const float resetW = 240, arrowW = 110, pageLabelW = 150, gap = 12;
        var resetX = right - resetW;
        var nextX = resetX - gap * 2 - arrowW;
        var labelX = nextX - pageLabelW;
        var previousX = labelX - arrowW;

        AddModElement(_modFrame, new ModifiersTitleBar(), ModLeftX, ModTitleY, previousX - gap - ModLeftX, ModTitleH);
        AddModElement(_modFrame, new ModifiersResetButton(), resetX, ModTitleY, resetW, ModTitleH);
        AddModElement(_modFrame, new ModifiersDescriptionBar(), ModLeftX, ModDescY, right - ModLeftX, ModDescH);
        AddModElement(_modPagers, new ModifiersPageButton(-1), previousX, ModTitleY, arrowW, ModTitleH);
        AddModElement(_modPagers, new ModifiersPageLabel(), labelX, ModTitleY, pageLabelW, ModTitleH);
        AddModElement(_modPagers, new ModifiersPageButton(1), nextX, ModTitleY, arrowW, ModTitleH);

        foreach (var page in PackSections(CollectSections())) {
            var elements = new List<UIElement>();
            foreach (var placed in page) {
                var x = ModColumnX(placed.Column);
                AddModElement(elements, new ModifiersHeader(placed.Section.Title, placed.Section.Rows), x, ModSlotY(placed.Slot),
                    ModSpanWidth(placed.Span), ModRowH);

                var rows = placed.Section.Rows;
                for (int i = 0; i < rows.Count; i++) {
                    var column = placed.Column + i / placed.RowsPerColumn;
                    var slot = placed.Slot + 1 + i % placed.RowsPerColumn;
                    AddModElement(elements, rows[i], ModColumnX(column), ModSlotY(slot), ModColumnW, ModRowH);
                    AllModifierRows.Add(rows[i]);
                }
            }
            _modPages.Add(elements);
        }

        ModifiersPage = Math.Clamp(ModifiersPage, 0, ModifiersPageCount - 1);
        SetDifficultiesButtonsVisibility(ModifiersOpen);
    }

    static void AddModElement(List<UIElement> group, UIElement element, float x, float y, float width, float height) {
        element.SetDimensions(() => new Vector2(x, y).ToResolution(), () => new Vector2(width, height).ToResolution());
        element.IsVisible = false;
        group.Add(element);
        AllDifficultyButtons.Add(element);
    }

    sealed record ModSection(string Title, List<ModifierRow> Rows);
    sealed record PlacedSection(ModSection Section, int Column, int Span, int Slot, int RowsPerColumn);

    /// <summary>One section per category, in category order. A section too big for a whole page is split into parts.</summary>
    static List<ModSection> CollectSections() {
        const int maxRowsPerSection = ModColumns * (ModSlotsPerColumn - 1);
        var sections = new List<ModSection>();

        foreach (var category in Modifiers.Categories) {
            var rows = new List<ModifierRow>();
            foreach (var definition in Modifiers.Definitions)
                if (definition.Category == category)
                    rows.Add(new ModifierRow(definition));
            if (rows.Count == 0)
                continue;

            var parts = (rows.Count + maxRowsPerSection - 1) / maxRowsPerSection;
            for (int part = 0; part < parts; part++) {
                var slice = rows.GetRange(part * maxRowsPerSection, Math.Min(maxRowsPerSection, rows.Count - part * maxRowsPerSection));
                sections.Add(new ModSection(parts > 1 ? $"{category} ({part + 1}/{parts})" : category, slice));
            }
        }
        return sections;
    }

    // places sections on pages. Each section is a header plus its rows split evenly over one or more side-by-side columns
    static List<List<PlacedSection>> PackSections(List<ModSection> sections) {
        var pages = new List<List<PlacedSection>>();
        var page = new List<PlacedSection>();
        var fill = new int[ModColumns];

        foreach (var section in sections) {
            var placed = TryPlace(section, fill);
            if (placed is null) {
                pages.Add(page);
                page = [];
                Array.Clear(fill);
                placed = TryPlace(section, fill)!;
            }
            page.Add(placed);
            for (int c = placed.Column; c < placed.Column + placed.Span; c++)
                fill[c] = placed.Slot + 1 + placed.RowsPerColumn;
        }
        if (page.Count > 0 || pages.Count == 0)
            pages.Add(page);
        return pages;
    }

    static PlacedSection? TryPlace(ModSection section, int[] fill) {
        PlacedSection? best = null;
        for (int span = 1; span <= ModColumns; span++) {
            var rowsPerColumn = (section.Rows.Count + span - 1) / span;
            var height = 1 + rowsPerColumn;
            if (height > ModSlotsPerColumn)
                continue;
            for (int start = 0; start + span <= ModColumns; start++) {
                var top = 0;
                for (int c = start; c < start + span; c++)
                    top = Math.Max(top, fill[c]);
                if (top + height > ModSlotsPerColumn)
                    continue;
                if (best is null || top < best.Slot)
                    best = new PlacedSection(section, start, span, top, rowsPerColumn);
            }
        }
        return best;
    }
}
