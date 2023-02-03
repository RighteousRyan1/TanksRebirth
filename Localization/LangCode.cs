using System;

namespace TanksRebirth.Localization
{
    public struct LangCode
    {
        public string Language { get; set; }
        public string Country { get; set; }

        public LangCode(string lang, string country) {
            Language = lang;
            Country = country;
        }

        public static LangCode English = new("en", "US"); // done
        public static LangCode Spanish = new("es", "ES"); // done
        public static LangCode French = new("fr", "FR");
        public static LangCode Japanese = new("ja", "JP"); // done
        public static LangCode German = new("de", "DE");
        public static LangCode Russian = new("ru", "RU"); // done
        public static LangCode BrazillianPortuguese = new("pt", "BR");
        public static LangCode Chinese = new("zh", "CN");
        public static LangCode Polish = new("pl", "PL");

        public override string ToString()
            => $"{Language}_{Country}";

        public static LangCode Parse(string lang) {
            var spl = lang.Split('_');

            if (spl.Length > 2 || spl.Length < 2) {
                throw new Exception("Failure to parse " + lang + "into a " + nameof(LangCode) + ".");
            }

            return new(spl[0], spl[1]);
        }
        public static bool TryParse(string lang, out LangCode result) {
            result = default;
            var spl = lang.Split('_');

            if (spl.Length > 2 || spl.Length < 2) {
                return false;
            }
            result = new(spl[0], spl[1]);
            return true;
        }
    }
}