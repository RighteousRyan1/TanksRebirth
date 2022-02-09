
using System.IO;
using WiiPlayTanksRemake.Internals.Common.IO;

namespace WiiPlayTanksRemake.Localization
{
    // TODO: once cuno pr merged, change text values appropriately
    public struct Language
    {
        public string Resume;
        public string StartOver;
        public string Options;
        public string Quit;

        public string Volume;
        public string Graphics;
        public string Controls;

        public string MusicVolume;
        public string EffectsVolume;
        public string AmbientVolume;

        public string PerPxLight;
        public string VSync;
        public string BorderlessWindow;
        public string Resolution;

        public string Back;
        public void LoadLang(LangCode profile)
        {
            // for example, it would be sane to have en_US or es_SP or jp_JA
            JsonHandler handler = new(this, Path.Combine(TankGame.SaveDirectory, $"{profile.Language}_{profile.Country}"));

            handler.DeserializeAndSet<Language>();
        }
    }
}