using System.Collections.Generic;

namespace TanksRebirth.Localization;
public class LocalizedString(Dictionary<LangCode, string> langToString)
{
    private Dictionary<LangCode, string> _langCodeToString = langToString;

    public bool AddLocalization(LangCode langCode, string outputString) {
        if (_langCodeToString.ContainsKey(langCode))
            return false;
        _langCodeToString.Add(langCode, outputString);
        return true;
    }

    public string? GetLocalizedString(LangCode langCode) => _langCodeToString.ContainsKey(langCode) ? _langCodeToString[langCode] : null;

    public static implicit operator string(LocalizedString localizedString) => localizedString.GetLocalizedString(TankGame.GameLanguage.ActiveLang)!;
}
