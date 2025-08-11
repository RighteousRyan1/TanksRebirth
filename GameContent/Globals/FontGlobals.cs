using FontStashSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Localization;

namespace TanksRebirth.GameContent.Globals;

#pragma warning disable CS8618, CA2211
public static class FontGlobals {
    public static FontSystem RebirthFontSystem;

    public static SpriteFontBase RebirthFont;
    public static SpriteFontBase RebirthFontLarge;

    public static void LoadFont(LangCode lang) {
        RebirthFontSystem.AddFont(File.ReadAllBytes(@$"Content/Assets/fonts/{lang.Language}_{lang.Country}.ttf"));
    }
}