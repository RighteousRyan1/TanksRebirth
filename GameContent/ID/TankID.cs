using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Internals.Common.Framework.Collections;

namespace TanksRebirth.GameContent.ID;

// should this be a dictionary for people to easily add to it?
public sealed class TankID
{
    public const int None = -1;
    // this is going to be a while.
    public const int Brown = 0;
    public const int Ash = 1;
    public const int Marine = 2;
    public const int Yellow = 3;
    public const int Pink = 4;
    public const int Violet = 5;
    public const int Green = 6;
    public const int White = 7;
    public const int Black = 8;

    // here separates the vanilla tanks from the master mod tanks

    public const int Bronze = 9;
    public const int Silver = 10;
    public const int Sapphire = 11;
    public const int Ruby = 12;
    public const int Citrine = 13;
    public const int Amethyst = 14;
    public const int Emerald = 15;
    public const int Gold = 16;
    public const int Obsidian = 17;

    // here separates the master mod tanks from the marble mod tanks

    public const int Granite = 18;
    public const int Bubblegum = 19;
    public const int Water = 20;
    public const int Tiger = 21;
    public const int Crimson = 22;
    public const int Fade = 23;
    public const int Creeper = 24;
    public const int Gamma = 25;
    public const int Marble = 26;

    public const int Cherry = 27;
    public const int Explosive = 28;
    public const int Electro = 29;
    public const int RocketDefender = 30;
    public const int Assassin = 31;
    public const int Commando = 32;

    public static ReflectionDictionary<TankID> Collection { get; internal set; } = new(MemberType.Fields);
}
