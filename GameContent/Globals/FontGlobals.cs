using FontStashSharp;
using System.Collections.Generic;
using System.IO;
using TanksRebirth.Localization;

namespace TanksRebirth.GameContent.Globals;

#pragma warning disable CS8618, CA2211
public static class FontGlobals {
    public static FontSystem RebirthFontSystem;

    public static SpriteFontBase RebirthFont;
    public static SpriteFontBase RebirthFontLarge;

    readonly static List<LangCode> _loadedFontLangs = [];

    readonly static List<string> _loadedFontPaths = [];

    public static void LoadLocalizedFont(LangCode lang) {
        if (_loadedFontLangs.Contains(lang)) return;

        _loadedFontLangs.Add(lang);
        RebirthFontSystem.AddFont(File.ReadAllBytes(@$"Content/Assets/fonts/{lang.Language}_{lang.Country}.ttf"));
    }

    public static void LoadFontDirect(FontSystem fs, string fontPath) {
        if (_loadedFontPaths.Contains(fontPath)) {
            return;
        }

        _loadedFontPaths.Add(fontPath);

        fs.AddFont(File.ReadAllBytes(fontPath));
    }
}