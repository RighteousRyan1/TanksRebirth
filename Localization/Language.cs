
using System.IO;
using WiiPlayTanksRemake.Internals.Common.IO;

namespace WiiPlayTanksRemake.Localization
{
    // TODO: once cuno pr merged, change text values appropriately
    /// <summary>Localization to load a <see cref="Language"/> from a .json entry. Defaults to English if no <see cref="LangCode"/> is loaded.</summary>
    public class Language
    {
        public string Resume = "Resume";
        public string StartOver = "Start Over";
        public string Options = "Options";
        public string Quit = "Quit";

        public string Volume = "Volume";
        public string Graphics = "Graphics";
        public string Controls = "Controls";

        public string MusicVolume = "Music Volume";
        public string EffectsVolume = "Effects Volume";
        public string AmbientVolume = "Ambient Volume";

        public string PerPxLight = "Per-Pixel Lighting";
        public string VSync = "Vertical Sync";
        public string BorderlessWindow = "Borderless Window";
        public string Resolution = "Resolution";

        public string Back = "Back";
        public void LoadLang(LangCode profile)
        {
            // for example, it would be sane to have en_US or es_SP or jp_JA
            JsonHandler handler = new(this, Path.Combine(TankGame.SaveDirectory, $"{profile.Language}_{profile.Country}"));

            handler.DeserializeAndSet<Language>();
        }
    }
}