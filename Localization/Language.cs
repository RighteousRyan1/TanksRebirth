
using System.IO;
using System.Text.Json;
using WiiPlayTanksRemake.Internals.Common.IO;

namespace WiiPlayTanksRemake.Localization
{
    // TODO: once cuno pr merged, change text values appropriately
    /// <summary>Localization to load a <see cref="Language"/> from a .json entry. Defaults to English if no <see cref="LangCode"/> is loaded.</summary>
    public class Language
    {
        public string Mission { get; set; } = "Mission";
        public string Resume { get; set; } = "Resume";
        public string StartOver { get; set; } = "Start Over";
        public string Options { get; set; } = "Options";
        public string Quit { get; set; } = "Quit";

        public string Volume { get; set; } = "Volume";
        public string Graphics { get; set; } = "Graphics";
        public string Controls { get; set; } = "Controls";

        public string MusicVolume { get; set; } = "Music Volume";
        public string EffectsVolume { get; set; } = "Effects Volume";
        public string AmbientVolume { get; set; } = "Ambient Volume";

        public string PerPxLight { get; set; } = "Per-Pixel Lighting";
        public string PerPxLightDesc { get; set; } = "Whether or not to draw lighting on each individual pixel";
        public string VSync { get; set; } = "Vertical Sync";
        public string VSyncDesc { get; set; } = "Whether or not to render one full frame cycle per second";
        public string BorderlessWindow { get; set; } = "Borderless Window";
        public string BorderlessWindowDesc { get; set; } = "Whether or not to run the game window borderless";
        public string Resolution { get; set; } = "Resolution";
        public string ResolutionDesc { get; set; } = "Changes the resolution of the game";

        public string PressAKey { get; set; } = "Press a key";

        public string Back { get; set; } = "Back";

        public static void LoadLang(ref Language lang, LangCode code)
        {
            // for example, it would be sane to have en_US or es_SP or jp_JA
            var path = Path.Combine(Path.Combine("Localization", $"{code}.json"));
            JsonHandler handler = new(lang, path);

            var newLang = handler.DeserializeAndSet<Language>();

            lang = newLang;

            GameContent.GameHandler.ClientLog.Write($"Loading language '{code}'... [ " + path + " ]", Internals.LogType.Debug);
        }
    }
}