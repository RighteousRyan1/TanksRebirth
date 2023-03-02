using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.GameContent.ID;
using TanksRebirth.Internals.Common.Framework;

namespace TanksRebirth.GameContent.Systems.PingSystem;

public static class PingMenu {

    // TODO: localize
    public static Dictionary<int, string> PingsIdToName = new() {
        [PingID.Generic] = "General",
        [PingID.StayHere] = "Stay Here",
        [PingID.WatchHere] = "Watch Here",
        [PingID.AvoidHere] = "Avoid Here",
        [PingID.GoHere] = "Go Here",
        [PingID.FocusHere] = "Focus Here",
        [PingID.GroupHere] = "Group Here"
    };

    // i dont think a radial would be optimal. Scroll wheel would even be better
    //public static Radial RadialMenu;

    private static int _curPingId;
    
    // todo: finish impl
}
