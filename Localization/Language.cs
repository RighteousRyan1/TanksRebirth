
using System.IO;
using System.Text.Json;
using TanksRebirth.Internals.Common.IO;

namespace TanksRebirth.Localization
{
    /// <summary>Localization to load a <see cref="Language"/> from a .json entry. Defaults to English if no <see cref="LangCode"/> is loaded.</summary>
    public class Language
    {
        public string Mission { get; set; }
        public string Resume { get; set; }
        public string StartOver { get; set; }
        public string Options { get; set; }
        public string Quit { get; set; }

        public string Volume { get; set; }
        public string Graphics { get; set; }
        public string Controls { get; set; }

        public string MusicVolume { get; set; }
        public string EffectsVolume { get; set; }
        public string AmbientVolume { get; set; }

        public string PerPxLight { get; set; }
        public string PerPxLightDesc { get; set; }
        public string VSync { get; set; }
        public string VSyncDesc { get; set; }
        public string BorderlessWindow { get; set; }
        public string BorderlessWindowDesc { get; set; }
        public string Resolution { get; set; }
        public string ResolutionDesc { get; set; }

        public string PressAKey { get; set; }

        public string Back { get; set; }


        #region Main Menu

        public string Play { get; set; }

        public string SinglePlayer { get; set; }

        public string LevelEditor { get; set; }

        public string Multiplayer { get; set; }

        public string ConnectToServer { get; set; }

        public string CreateServer { get; set; }

        public string Difficulties { get; set; }

        #endregion

        public static void LoadLang(ref Language lang, LangCode code)
        {
            // for example, it would be sane to have en_US or es_SP or jp_JA
            try
            {
                var path = Path.Combine(Path.Combine("Localization", $"{code}.json"));
                JsonHandler<Language> handler = new(lang, path);

                var newLang = handler.DeserializeAndSet();

                lang = newLang;

                GameContent.GameHandler.ClientLog.Write($"Loading language '{code}'... [ " + path + " ]", Internals.LogType.Debug);
            }
            catch
            {
                GameContent.GameHandler.ClientLog.Write($"Loading language '{code}'... Could not find localization file! Using default language 'en_US' instead.", Internals.LogType.Debug);
                var path = Path.Combine(Path.Combine("Localization", $"en_US.json"));
                JsonHandler<Language> handler = new(lang, path);
                var newLang = handler.DeserializeAndSet();

                lang = newLang;
                return;
            }
        }
    }
}