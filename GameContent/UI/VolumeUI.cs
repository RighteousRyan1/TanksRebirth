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

        Page.Header(lang.Menu.Volume, x);

        MusicVolume = Page.Slider(x, SettingsUI.RowY(0), lang.Settings.MusicVolume ?? "Music",
            lang.Settings.MusicVolumeDesc,
            () => TankGame.Settings.Audio.MusicVolume, v => TankGame.Settings.Audio.MusicVolume = v, new GameConfig().Audio.MusicVolume);

        EffectsVolume = Page.Slider(x, SettingsUI.RowY(1), lang.Settings.EffectsVolume ?? "Sound Effects",
            lang.Settings.EffectsVolumeDesc,
            () => TankGame.Settings.Audio.EffectsVolume, v => TankGame.Settings.Audio.EffectsVolume = v, new GameConfig().Audio.EffectsVolume);

        AmbientVolume = Page.Slider(x, SettingsUI.RowY(2), lang.Settings.AmbientVolume ?? "Ambient",
            lang.Settings.AmbientVolumeDesc,
            () => TankGame.Settings.Audio.AmbientVolume, v => TankGame.Settings.Audio.AmbientVolume = v, new GameConfig().Audio.AmbientVolume);

        Page.Button(x, SettingsUI.RowY(4), SettingsUI.ColumnW, lang.Settings.ResetDefaults,
            lang.Settings.ResetVolumesDesc, () => {
                var defaults = new GameConfig();
                MusicVolume.Value = defaults.Audio.MusicVolume;
                EffectsVolume.Value = defaults.Audio.EffectsVolume;
                AmbientVolume.Value = defaults.Audio.AmbientVolume;
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
