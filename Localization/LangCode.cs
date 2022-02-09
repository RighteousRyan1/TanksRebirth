
namespace WiiPlayTanksRemake.Localization
{
    public struct LangCode
    {
        public string Language;
        public string Country;

        public LangCode(string lang, string country) {
            Language = lang;
            Country = country;
        }

        public static LangCode EnglishAmerica = new("en", "US");
        public static LangCode SpanishSpain = new("es", "ES");
        public static LangCode Japanese = new("ja", "JP");
        public static LangCode German = new("de", "DE");
        public static LangCode Russian = new("ru", "RU");
        public static LangCode BrazillianPortuguese = new("pt", "BR");
    }
}