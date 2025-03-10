using System;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Graphics;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.UI.MainMenu;

public static partial class MainMenuUI {
    public static void OpenAudio() {
        TankMusicSystem.StopAll();
        Theme = GetAppropriateMusic();
        Theme.Play();
    }
    public static void UpdateMusic() {
        VolumeMultiplier = SteamworksUtils.IsOverlayActive ? 0.25f : 1f;
        if (_musicFading) {
            if (Theme.Volume > 0)
                Theme.Volume -= 0.0075f;
        }
        else if (Active)
            Theme.Volume = TankGame.Settings.MusicVolume * 0.1f * VolumeMultiplier;
    }
    public static OggMusic GetAppropriateMusic() {
        OggMusic music = GameScene.Theme switch {
            MapTheme.Vanilla => new OggMusic("Main Menu Theme", "Content/Assets/music/mainmenu/theme.ogg", 1f),
            MapTheme.Christmas => new OggMusic("Main Menu Theme", "Content/Assets/music/mainmenu/theme_christmas.ogg", 1f),
            _ => throw new Exception("Invalid game theme for menu music.")
        };
        return music;
    }
}
