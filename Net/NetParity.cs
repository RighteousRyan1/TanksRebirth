using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.Net; 

// can't really think of a better thing to name this. just contains stuff that manages Net IDs
public static class NetParity {
    static int _netIdCounter;

    // i think?
    /// <summary>A new network ID, unique to this client.</summary>
    /// <remarks>Should only be used by gameplay elements with a net owner.</remarks>
    public static int NewNetId() {
        // increment the actual counter part of the NetId
        _netIdCounter = (_netIdCounter + 1) & 0xFFFFFF;

        // shift so we can plant the client id in the first 8 bytes, then the counter for the latter 24
        return ((NetPlay.GetMyClientId() + 1) << 24) | _netIdCounter;
    }
}
