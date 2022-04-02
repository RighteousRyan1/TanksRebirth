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
    }
}