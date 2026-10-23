using TanksRebirth.GameContent.Tanks;
using TanksRebirth.GameContent.UI.MainMenu;
using TanksRebirth.Graphics;

namespace TanksRebirth.GameContent.UI;

/// <summary>The Gameplay page of the settings window (<see cref="SettingsUI.Gameplay"/>): tank tracks, menu gameplay and team colors.</summary>
public static class GameplayUI {
    public static SettingRow FadeTracksBtn = null!;
    public static SettingRow TrackLimitBtn = null!;
    public static SettingRow MenuGameplayBtn = null!;
    public static SettingRow TeamVisualsBtn = null!;

    /// <summary>Whether the Gameplay page is on screen. Clearing it closes the settings window.</summary>
    public static bool BatchVisible { get; set; }

    static SettingsPage Page => SettingsUI.Gameplay;

    public static void Initialize() {
        Page.Clear();

        var lang = TankGame.GameLanguage;
        const float x = SettingsUI.CenterX;

        Page.Header("Gameplay", x);

        FadeTracksBtn = Page.Row(x, SettingsUI.RowY(0), lang.Settings.FadeTracks ?? "Fading Tank Tracks",
            lang.Settings.FadeTracksDesc ?? "Tank tracks slowly fade away. Saves performance in long missions.",
            () => GraphicsUI.OnOff(TankGame.Settings.FadeFootprints),
            _ => TankFootprint.ShouldTracksFade = TankGame.Settings.FadeFootprints = !TankGame.Settings.FadeFootprints);

        TrackLimitBtn = Page.Row(x, SettingsUI.RowY(1), "Tank Track Limit",
            "How many tank tracks can stay on the ground before the oldest disappear.",
            () => $"{TankGame.Settings.TankFootprintLimit:N0}",
            dir => GraphicsSettings.SetFootprintLimit(GraphicsUI.Cycle(GraphicsSettings.FootprintLimitOptions,
                GraphicsUI.NearestOf(GraphicsSettings.FootprintLimitOptions, TankGame.Settings.TankFootprintLimit), dir)));

        MenuGameplayBtn = Page.Row(x, SettingsUI.RowY(2), lang.Settings.MenuGameplay ?? "Menu Gameplay",
            lang.Settings.MenuGameplayDesc ?? "Tanks battle in the background of the main menu.",
            () => GraphicsUI.OnOff(TankGame.Settings.MenuGameplayEnabled),
            _ => {
                TankGame.Settings.MenuGameplayEnabled = !TankGame.Settings.MenuGameplayEnabled;
                if (!MainMenuUI.IsActive)
                    return;
                foreach (var tank in GameHandler.AllTanks)
                    tank?.Remove(true);
            });

        TeamVisualsBtn = Page.Row(x, SettingsUI.RowY(3), "Team Colors",
            "Tints each tank's wooden trim with its team color, so you can tell the teams apart.",
            () => GraphicsUI.OnOff(TankGame.Settings.ShowTeamVisuals),
            _ => Tank.ShowTeamVisuals = TankGame.Settings.ShowTeamVisuals = !TankGame.Settings.ShowTeamVisuals);

        Page.Button(x, SettingsUI.RowY(5), SettingsUI.ColumnW, "Reset to Defaults",
            "Puts every option on this page back to its default.", () => {
                var defaults = new GameConfig();
                GraphicsSettings.SetFootprintLimit(defaults.TankFootprintLimit);
                TankFootprint.ShouldTracksFade = TankGame.Settings.FadeFootprints = defaults.FadeFootprints;
                if (TankGame.Settings.MenuGameplayEnabled != defaults.MenuGameplayEnabled)
                    MenuGameplayBtn.Step(1);
                Tank.ShowTeamVisuals = TankGame.Settings.ShowTeamVisuals = defaults.ShowTeamVisuals;
            });
    }
}
