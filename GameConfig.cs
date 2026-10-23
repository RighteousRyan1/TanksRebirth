using Microsoft.Xna.Framework.Input;
using TanksRebirth.GameContent.ID;
using TanksRebirth.Internals.Common;
using TanksRebirth.Graphics;
using TanksRebirth.Graphics.DynamicLighting;
using TanksRebirth.Localization;

namespace TanksRebirth;

public enum WindowKind {
    Windowed,
    Fullscreen,
    FullscreenBorderless
}
public class GameConfig {
    public struct AudioSettings() {
        public float MusicVolume { get; set; } = 0.5f;
        public float EffectsVolume { get; set; } = 1f;
        public float AmbientVolume { get; set; } = 1f;
    }
    public struct GraphicsSettings() {
        public bool PerPixelLighting { get; set; } = true;
        public int MSAASamples { get; set; } = 0;
        public bool RainyWindows { get; set; } = true;
        public float RainStrength { get; set; } = 1f;
        public bool TankBlobShadows { get; set; } = true;
        public bool MenuBlur { get; set; } = true;
    }
    public struct ControlSettings() {
        public Keys UpKeybind { get; set; } = Keys.W;
        public Keys LeftKeybind { get; set; } = Keys.A;
        public Keys RightKeybind { get; set; } = Keys.D;
        public Keys DownKeybind { get; set; } = Keys.S;
        public Keys MineKeybind { get; set; } = Keys.Space;
        // a binding uses its mouse button instead of its key when the button isn't None
        public MouseInput UpMouseBind { get; set; } = MouseInput.None;
        public MouseInput LeftMouseBind { get; set; } = MouseInput.None;
        public MouseInput RightMouseBind { get; set; } = MouseInput.None;
        public MouseInput DownMouseBind { get; set; } = MouseInput.None;
        public MouseInput MineMouseBind { get; set; } = MouseInput.None;
        public int PlayerUsingKeyboard { get; set; } = PlayerID.Blue;
    }
    public struct DisplaySettings() {
        public int TargetFPS { get; set; } = 60;
        public int ResWidth { get; set; } = 640;
        public int ResHeight { get; set; } = 480;
        public bool Vsync { get; set; } = true;
        public WindowKind WindowKind { get; set; } = WindowKind.Windowed;
    }
    public struct GameplaySettings() {
        public bool MenuGameplayEnabled { get; set; } = true;
        public bool ShowTeamVisuals { get; set; } = false;
        public int TankFootprintLimit { get; set; } = 50_000;
        public int FirstPersonFOV { get; set; } = 90;
        public bool FadeFootprints { get; set; } = false;
    }
    public struct ThemeSettings() {
        public MapTheme GameTheme { get; set; } = MapTheme.Vanilla;
        public string MapPack { get; set; } = "Vanilla";
        public string TankPack { get; set; } = "Vanilla";
        public string MusicPack { get; set; } = "Vanilla";
    }

    // actual saved stuff

    public AudioSettings Audio = new();
    public GraphicsSettings Graphics = new();
    public ControlSettings Control = new();
    public DisplaySettings Display = new();
    public GameplaySettings Gameplay = new();
    public ThemeSettings Theme = new();
    public DynLightSettings Lighting = new();

    // idk what category this should go under
    public LangCode Language = LangCode.English;


    // public MultiplayerInfo MultiplayerInfo { get; set; } = default;
}

// you know... this will get saved at some point.
public struct MultiplayerInfo {
    public string Username { get; set; } = "";

    public string LastUsedIp { get; set; } = "";

    public string LastUsedPassword { get; set; } = "";

    public MultiplayerInfo() { }
}