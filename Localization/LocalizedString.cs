using System.Collections.Generic;

namespace TanksRebirth.Localization;
public class LocalizedString {
    readonly Dictionary<LangCode, string> _langCodeToString;

    public string this[LangCode code] {
        get => _langCodeToString[code];
        set => _langCodeToString[code] = value;
    }

    public LocalizedString() {
        _langCodeToString = [];
    }

    public bool TryAdd(LangCode langCode, string outputString) {
        if (_langCodeToString.ContainsKey(langCode))
            return false;

        _langCodeToString.Add(langCode, outputString);
        return true;
    }

    public static implicit operator string(LocalizedString localizedString) => localizedString[TankGame.GameLanguage.ActiveLang]!;
}