using TanksRebirth.GameContent.Globals;

namespace TanksRebirth.GameContent.UI;

/// <summary>The Audio page of the settings window (<see cref="SettingsUI.Audio"/>): music, sound effect and ambient volume.</summary>
/// <remarks>The sliders read and write <see cref="TankGame.Settings"/> directly, so setting <c>Value</c> from code (like the
/// <c>snd_*</c> commands do) updates both the setting and the slider.</remarks>
public static class VolumeUI {
    public static SliderRow MusicVolume = null!;
    public static SliderRow EffectsVolume = null!;
    public static SliderRow AmbientVolume = null!;

    /// <summary>Whether the Audio page is on screen. Clearing it closes the settings window.</summary>
    public static bool BatchVisible { get; set; }

    static SettingsPage Page => SettingsUI.Audio;

    public static void Initialize() {
        Page.Clear();

        var lang = TankGame.GameLanguage;
        const float x = SettingsUI.CenterX;

        Page.Header("Volume", x);

        MusicVolume = Page.Slider(x, SettingsUI.RowY(0), lang.Settings.MusicVolume ?? "Music",
            "Volume of the menu, level editor and mission music.",
            () => TankGame.Settings.MusicVolume, v => TankGame.Settings.MusicVolume = v, new GameConfig().MusicVolume);

        EffectsVolume = Page.Slider(x, SettingsUI.RowY(1), lang.Settings.EffectsVolume ?? "Sound Effects",
            "Volume of shots, explosions, mines, tanks and menu sounds.",
            () => TankGame.Settings.EffectsVolume, v => TankGame.Settings.EffectsVolume = v, new GameConfig().EffectsVolume);

        AmbientVolume = Page.Slider(x, SettingsUI.RowY(2), lang.Settings.AmbientVolume ?? "Ambient",
            "Volume of the weather: rain, thunder and the snowy wind.",
            () => TankGame.Settings.AmbientVolume, v => TankGame.Settings.AmbientVolume = v, new GameConfig().AmbientVolume);

        Page.Button(x, SettingsUI.RowY(4), SettingsUI.ColumnW, "Reset to Defaults",
            "Puts all three volumes back to their defaults.", () => {
                var defaults = new GameConfig();
                MusicVolume.Value = defaults.MusicVolume;
                EffectsVolume.Value = defaults.EffectsVolume;
                AmbientVolume.Value = defaults.AmbientVolume;
            });
    }

    /// <summary>Opens the settings window on this page.</summary>
    public static void ShowAll() => SettingsUI.Open(SettingsUI.Audio);

    /// <summary>Closes the settings window if it's on this page.</summary>
    public static void HideAll() {
        if (SettingsUI.IsOpen && SettingsUI.Current == SettingsUI.Audio)
            SettingsUI.Close();
    }
}
