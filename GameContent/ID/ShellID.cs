using TanksRebirth.Internals.Common.Framework.Collections;

namespace TanksRebirth.GameContent.ID;

public sealed class ShellID
{
    public const int Player = 0;
    public const int Standard = 1;
    public const int Rocket = 2;
    public const int TrailedRocket = 3;
    public const int Supressed = 4;
    public const int Explosive = 5;

    /* IDEAS:
     * Clustering shell: explodes into multiple smaller shells (after X amount of time or distance?)
     * Grenade...? self explanatory
     * Torpedo: digs underground and approaches enemies from below, pops up and explodes
     * Blazing shell: leaves a trail of fire behind it which damages enemies
     * 
     */

    public static ReflectionDictionary<ShellID> Collection { get; internal set; } = new(MemberType.Fields);
}
