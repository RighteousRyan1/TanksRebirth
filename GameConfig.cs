using Microsoft.Xna.Framework.Input;
using TanksRebirth.GameContent.ID;
using TanksRebirth.Graphics;
using TanksRebirth.Graphics.DynamicLighting;
using TanksRebirth.Localization;

namespace TanksRebirth;

public enum WindowKind {
    Windowed,
    Fullscreen,
    FullscreenBorderless
}
public class GameConfig
{
    public float MusicVolume { get; set; } = 0.5f;
    public float EffectsVolume { get; set; } = 1f;
    public float AmbientVolume { get; set; } = 1f;

    #region Graphics Settings
    public int TankFootprintLimit { get; set; } = 10000;
    public bool PerPixelLighting { get; set; } = true;
    public bool Vsync { get; set; } = true;
    public WindowKind WindowKind { get; set; } = WindowKind.Windowed;
    /// <summary>Multisample anti-aliasing for the 3D scene: 0 (off), 2, 4 or 8 samples.</summary>
    public int MSAASamples { get; set; } = 0;
    public bool FadeFootprints { get; set; } = false;

    /// <summary>Frame rate cap while VSync is off. 0 = unlimited.</summary>
    public int TargetFPS { get; set; } = 60;

    /// <summary>Dynamic lighting: on/off, quality, time of day. Edited on the graphics settings page.</summary>
    public LightingSettings Lighting { get; set; } = new();
    #endregion

    #region Controls Settings

    public Keys UpKeybind { get; set; } = Keys.W;
    public Keys LeftKeybind { get; set; } = Keys.A;
    public Keys RightKeybind { get; set; } = Keys.D;
    public Keys DownKeybind { get; set; } = Keys.S;
    public Keys MineKeybind { get; set; } = Keys.Space;
    public int PlayerUsingKeyboard { get; set; } = PlayerID.Blue;

    #endregion

    #region Res Settings

    // Defaults to a 4:3 (480p) resolution if not set.

    public int ResWidth { get; set; } = 640;

    public int ResHeight { get; set; } = 480;

    #endregion

    #region Extra Settings

    /// <summary>Used to be casted to a MapTheme to change the... map's theme.</summary>
    public MapTheme GameTheme { get; set; } = MapTheme.Vanilla;
    public string MapPack { get; set; } = "Vanilla";
    public string TankPack { get; set; } = "Vanilla";
    public string MusicPack { get; set; } = "Vanilla";

    public bool MenuGameplayEnabled { get; set; } = true;

    #endregion

    #region Language

    public LangCode Language { get; set; } = LangCode.English;

    #endregion

    // public MultiplayerInfo MultiplayerInfo { get; set; } = default;
}
public struct MultiplayerInfo
{
    public string Username { get; set; } = "";

    public string LastUsedIp { get; set; } = "";

    public string LastUsedPassword { get; set; } = "";

    public MultiplayerInfo() { }
}