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

    public static List<LangCode> LoadedFontLangs = [];

    public static void LoadFont(LangCode lang) {
        if (LoadedFontLangs.Contains(lang)) return;

        LoadedFontLangs.Add(lang);
        RebirthFontSystem.AddFont(File.ReadAllBytes(@$"Content/Assets/fonts/{lang.Language}_{lang.Country}.ttf"));
    }
}