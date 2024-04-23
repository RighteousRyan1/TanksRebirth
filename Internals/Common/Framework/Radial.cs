using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.Internals.Common.Framework;

// todo: finish when needed.
/// <summary>Construct a radial that can be used for visuals.</summary>
public class Radial {
    private uint _divs;
    public uint Divisions {
        get => _divs;
        set {
            _divs = value;

            RadiansPerSlice = MathHelper.TwoPi / Divisions;
            DegreesPerSlice = 360 / Divisions;
        }
    }
    public Circle ContainingCircle { get; private set; }
    public float RadiansPerSlice { get; private set; }
    public float DegreesPerSlice { get; private set; }

    public Radial(uint divisions, Circle containingCircle) {
        Divisions = divisions;
        ContainingCircle = containingCircle;
    }
}
