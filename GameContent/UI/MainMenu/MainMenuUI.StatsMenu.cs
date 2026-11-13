using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Tanks.AI;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals.UI;

namespace TanksRebirth.GameContent.UI.MainMenu;

#pragma warning disable
public static partial class MainMenuUI {
    // layout, same panel and column positions as the settings window
    internal const float StatsPanelX = 150, StatsPanelY = 30, StatsPanelW = 1620, StatsPanelH = 820;
    internal const float StatsLeftX = 190, StatsRightX = 990, StatsColumnW = 740, StatsInnerRight = 1730;
    internal const float StatsTitleY = 44, StatsTitleH = 54;
    internal const float StatsSectionY = 114, StatsSectionH = 582;
    internal const float StatsInfoY = 716, StatsInfoH = 100;

    static readonly List<UIElement> _statsElements = [];

    // clicks are handled while the UI is updating, so anything that removes elements waits until the next UpdateStatsMenu
    static Action? _pendingStatsAction;

    /// <summary>Builds the stats window from the save file as it is right now.</summary>
    public static void RequestStats() {
        DisposeStatsMenu();

        var language = TankGame.GameLanguage.GameStats;
        var save = TankGame.SaveFile;

        // localize ts
        IReadOnlyList<StatRow> general = [
            new(language.MissionsCompleted, save.MissionsCompleted),
            new(language.CampaignsCompleted, save.CampaignsCompleted),
            new(language.TankKillsTotal, save.TotalKills),
            new(language.Deaths, save.Deaths),
            new(language.Suicides, save.Suicides),
            new(language.TankKillsTotalBullets, save.BulletKills),
            new(language.TankKillsTotalBulletsBounced, save.BounceKills),
            new(language.TankKillsTotalMines, save.MineKills),
            new(language.TimePlayedTotal, save.TimePlayed.TotalHours, "{0:0.0} hrs"),
            new(language.TimePlayedCurrent, TankGame.CurrentSessionTimer.Elapsed.TotalMinutes, "{0:0.0} mins")
        ];

        // only use tanks that have kills attached to them to not spoil anything
        var kills = new List<StatRow>();
        foreach (var kill in save.TankKills) {
            if (kill.Value == 0)
                continue;

            var name = TankID.Collection.GetKey(kill.Key)!.SplitByCamel();
            var color = AITank.TankDestructionColors.TryGetValue(kill.Key, out var found) ? found : Color.Gray;
            kills.Add(new StatRow(name, kill.Value, "{0:0}", color, ColorUtils.WhiteBlack(color)));
        }

        AddStatsElement(new ModifiersFrame(), StatsPanelX, StatsPanelY, StatsPanelW, StatsPanelH);

        const float backW = 150, gap = 12;
        const float backX = StatsInnerRight - backW;
        AddStatsElement(new StatsTitleBar(), StatsLeftX, StatsTitleY, backX - gap - StatsLeftX, StatsTitleH);
        AddStatsElement(new ModsPillButton(() => TankGame.GameLanguage.Menu.Back, () => _pendingStatsAction = LeaveStatsMenu), backX, StatsTitleY, backW, StatsTitleH);

        AddStatsElement(new StatsSection("General Statistics", general, string.Empty), StatsLeftX, StatsSectionY, StatsColumnW, StatsSectionH);
        AddStatsElement(new StatsSection("Combat Efficiency", kills, "No tanks destroyed yet", startDelay: 0.5f), StatsRightX, StatsSectionY, StatsColumnW, StatsSectionH);

        AddStatsElement(new StatsInfoBar(), StatsLeftX, StatsInfoY, StatsInnerRight - StatsLeftX, StatsInfoH);
    }

    public static void UpdateStatsMenu() {
        if (_pendingStatsAction is { } pending) {
            _pendingStatsAction = null;
            pending();
        }

        // the window can be left from elsewhere (ESC, the back button), so tidy up whenever the menu isn't the stats menu
        if (MenuState != UIState.StatsMenu && _statsElements.Count > 0)
            DisposeStatsMenu();
    }

    static void AddStatsElement(UIElement element, float x, float y, float width, float height) {
        element.SetDimensions(() => new Vector2(x, y).ToResolution(), () => new Vector2(width, height).ToResolution());
        _statsElements.Add(element);
    }

    static void LeaveStatsMenu() {
        MenuState = UIState.PrimaryMenu;
        DisposeStatsMenu();
    }

    public static void DisposeStatsMenu() {
        foreach (var element in _statsElements)
            element.Remove();
        _statsElements.Clear();
    }
}
