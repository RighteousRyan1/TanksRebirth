using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.Net;

// torn between this being a class or a struct. When a player levels up, this has to be updated.
// probably a struct that can be re-assigned.
public struct MPPlayerData {
    public float XPLevel;
    // data for this game only
    public int TotalTanksDestroyed;
    public int TotalDeaths;
    public int PlayerID; // this can be used to track the player's color
    public float Accuracy;
    public float Score; // add score values to each tank?
}
