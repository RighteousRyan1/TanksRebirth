using TanksRebirth.Internals.Common.Framework.Collections;

namespace TanksRebirth.GameContent.ID;

// should this be a dictionary for people to easily add to it?
public sealed class TankID {
    // tanks from the original game

    public const int None = 0;
    public const int Brown = 1;
    public const int Ash = 2;
    public const int Marine = 3;
    public const int Pink = 4;
    public const int Yellow = 5;
    public const int Violet = 6;
    public const int Green = 7;
    public const int White = 8;
    public const int Black = 9;

    // here separates the vanilla tanks from the master mod tanks

    public const int Bronze = 10;
    public const int Silver = 11;
    public const int Sapphire = 12;
    public const int Ruby = 13;
    public const int Citrine = 14;
    public const int Amethyst = 15;
    public const int Emerald = 16;
    public const int Gold = 17;
    public const int Obsidian = 18;

    // ...future tanks will go here

    /* IDEAS:
     * 
     * Constructor tank: builds things
     * Boring tank: plows through things
     * 
     * Tornado tank: creates a tornado that sucks in projectiles (and enemies?)
     * 
     * Rampart tank: creates a shield (or something similar) that blocks projectiles
     * Charging tank: charges at you upon line of sight
     * 
     */

    public static ReflectionDictionary<TankID> Collection { get; internal set; } = new(MemberType.Fields);
}
