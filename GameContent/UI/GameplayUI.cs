using System.Linq;
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
    public static SettingRow FOVBtn = null!;

    /// <summary>Whether the Gameplay page is on screen. Clearing it closes the settings window.</summary>
    public static bool BatchVisible { get; set; }

    static SettingsPage Page => SettingsUI.Gameplay;

    // funny ahh way to get every 5 
    static readonly int[] _fovs = [.. Enumerable.Range(0, (GraphicsSettings.MaxFOV - GraphicsSettings.MinFOV) / 5 + 1).Select(i => GraphicsSettings.MinFOV + i * 5)];

    public static void Initialize() {
        Page.Clear();

        var lang = TankGame.GameLanguage;
        const float x = SettingsUI.CenterX;

        Page.Header("Gameplay", x);

        FadeTracksBtn = Page.Row(x, SettingsUI.RowY(0), lang.Settings.FadeTracks ?? "Fading Tank Tracks",
            lang.Settings.FadeTracksDesc ?? "Tank tracks slowly fade away.",
            () => GameUI.OnOff(TankGame.Settings.Gameplay.FadeFootprints),
            _ => TankFootprint.ShouldTracksFade = TankGame.Settings.Gameplay.FadeFootprints = !TankGame.Settings.Gameplay.FadeFootprints);

        TrackLimitBtn = Page.Row(x, SettingsUI.RowY(1), "Tank Track Limit",
            "How many tank tracks can stay on the ground before the oldest disappear.",
            () => $"{TankGame.Settings.Gameplay.TankFootprintLimit:N0}",
            dir => GraphicsSettings.SetFootprintLimit(GameUI.Cycle(GraphicsSettings.FootprintLimitOptions,
                GameUI.NearestOf(GraphicsSettings.FootprintLimitOptions, TankGame.Settings.Gameplay.TankFootprintLimit), dir)));

        MenuGameplayBtn = Page.Row(x, SettingsUI.RowY(2), lang.Settings.MenuGameplay ?? "Menu Gameplay",
            lang.Settings.MenuGameplayDesc ?? "Tanks battle in the background of the main menu.",
            () => GameUI.OnOff(TankGame.Settings.Gameplay.MenuGameplayEnabled),
            _ => {
                TankGame.Settings.Gameplay.MenuGameplayEnabled = !TankGame.Settings.Gameplay.MenuGameplayEnabled;
                if (!MainMenuUI.IsActive)
                    return;
                foreach (var tank in GameHandler.AllTanks)
                    tank?.Remove(true);
            });

        TeamVisualsBtn = Page.Row(x, SettingsUI.RowY(3), "Team Colors",
            "Tints each tank's wooden trim with its team color, so you can tell the teams apart.",
            () => GameUI.OnOff(TankGame.Settings.Gameplay.ShowTeamVisuals),
            _ => Tank.ShowTeamVisuals = TankGame.Settings.Gameplay.ShowTeamVisuals = !TankGame.Settings.Gameplay.ShowTeamVisuals);

        FOVBtn = Page.Row(x, SettingsUI.RowY(4), "First-Person FOV",
            $"How wide the first-person camera sees, in degrees ({GraphicsSettings.MinFOV} to {GraphicsSettings.MaxFOV}).",
            () => TankGame.Settings.Gameplay.FirstPersonFOV.ToString(),
            dir => TankGame.Settings.Gameplay.FirstPersonFOV = GameUI.Cycle(_fovs, GameUI.NearestOf(_fovs, TankGame.Settings.Gameplay.FirstPersonFOV), dir));

        Page.Button(x, SettingsUI.RowY(5), SettingsUI.ColumnW, "Reset to Defaults",
            "Puts every option on this page back to its default.", () => {
                var defaults = new GameConfig();
                GraphicsSettings.SetFootprintLimit(defaults.Gameplay.TankFootprintLimit);
                TankFootprint.ShouldTracksFade = TankGame.Settings.Gameplay.FadeFootprints = defaults.Gameplay.FadeFootprints;
                if (TankGame.Settings.Gameplay.MenuGameplayEnabled != defaults.Gameplay.MenuGameplayEnabled)
                    MenuGameplayBtn.Step(1);
                Tank.ShowTeamVisuals = TankGame.Settings.Gameplay.ShowTeamVisuals = defaults.Gameplay.ShowTeamVisuals;
            });
    }
}
