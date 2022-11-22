using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.Localization
{
    // unlocalized for now. will localize later.
    public static class LocalizationRandoms
    {
        // only called when webserver is failed to be accessed.

        public static readonly string[] RandomMotds =
        {
            "I heard you took an arrow to the knee",
            "Ryan has awesome hair!",
            "Colonel, I'm trying to sneak around...",
            "Ryan is mad... And has two eyes!",
            "Better than Unity's particle system!",
            "Also try Wii Play: Tanks!",
            "Black tank has returned!",
            "Green tanks are NOT calculators, I promise",
            "Resolution issues since the '60s!",
            "Stevie please fix the game",
            "Are you crazy?! Are you out of your mind?!",
            "Thanks BigKitty1011 for the amazing crown!",
            "Thanks BigKitty1011 for literally all other models outside of the vanilla game...",
            "Doesn't include gambling!",
            "Be sure not to erase your data unless there is an important update!",
            "Do not look at your memory usage...",
            "Also try Tanks: The Crusades!",
            "Ryan is the king of originality",
            "Lacking a lighting engine!",
            "Includes accurate physics!"
        };
        public static string GetRandomMotd() => RandomUtils.PickRandom(RandomMotds);
    }
}
