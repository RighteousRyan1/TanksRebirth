using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Internals.Common.Framework;

namespace TanksRebirth.GameContent.Systems.PingSystem;

public static class PingMenu {

    // TODO: localize
    public static Dictionary<int, string> PingsIdToName = new() {
        [0] = "General",
        [1] = "Stay Here",
        [2] = "Watch Here",
        [3] = "Avoid Here",
        [4] = "Go Here",
        [5] = "Focus Here",
        [6] = "Group Here"
    };

    // i dont think a radial would be optimal. Scroll wheel would even be better
    //public static Radial RadialMenu;

    private static int _curPingId;
    
    // todo: finish impl
}
