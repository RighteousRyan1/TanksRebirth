using Microsoft.Xna.Framework.Input;
using WiiPlayTanksRemake.GameContent;
using WiiPlayTanksRemake.Localization;

namespace WiiPlayTanksRemake
{
    public class GameConfig
    {
        public float MusicVolume { get; set; } = 0.5f;
        public float EffectsVolume { get; set; } = 1f;
        public float AmbientVolume { get; set; } = 1f;

        #region Graphics Settings
        public int TankFootprintLimit { get; set; } = 100000;
        public bool PerPixelLighting { get; set; } = true;
        public bool Vsync { get; set; } = true;
        public bool BorderlessWindow { get; set; } = true;

        public bool MSAA { get; set; } = false;

        #endregion

        #region Controls Settings

        public Keys UpKeybind { get; set; } = Keys.W;

        public Keys LeftKeybind { get; set; } = Keys.A;

        public Keys RightKeybind { get; set; } = Keys.D;

        public Keys DownKeybind { get; set; } = Keys.S;

        public Keys MineKeybind { get; set; } = Keys.Space;

        #endregion

        #region Res Settings

        // Defaults to a 4:3 (480p) resolution if not set.

        public int ResWidth { get; set; } = 640;

        public int ResHeight { get; set; } = 480;

        #endregion

        #region Extra Settings

        /// <summary>Used to be casted to a MapTheme to change the... map's theme.</summary>
        public MapTheme GameTheme { get; set; } = MapTheme.Default;

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
}