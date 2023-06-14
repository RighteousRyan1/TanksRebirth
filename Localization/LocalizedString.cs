using System.Collections.Generic;

namespace TanksRebirth.Localization;
public class LocalizedString {
    private Dictionary<LangCode, string> _langCodeToLocalizedStringConverter;
    public LocalizedString(Dictionary<LangCode, string> langToString) {
        _langCodeToLocalizedStringConverter = langToString;
    }

    public bool AddLocalization(LangCode langCode, string outputString) {
        if (_langCodeToLocalizedStringConverter.ContainsKey(langCode))
            return false;
        _langCodeToLocalizedStringConverter.Add(langCode, outputString);
        return true;
    }

    public string GetLocalizedString(LangCode langCode) => _langCodeToLocalizedStringConverter[langCode];

    public static implicit operator string(LocalizedString localizedString) => localizedString.GetLocalizedString(TankGame.GameLanguage.ActiveLang);
}
