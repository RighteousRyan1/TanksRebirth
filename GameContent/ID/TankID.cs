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
    public const int None = 0;
    // this is going to be a while.
    public const int Brown = 1;
    public const int Ash = 2;
    public const int Marine = 3;
    public const int Yellow = 4;
    public const int Pink = 5;
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

    // here separates the master mod tanks from the marble mod tanks

    public const int Granite = 19;
    public const int Bubblegum = 20;
    public const int Water = 21;
    public const int Tiger = 22;
    public const int Crimson = 23;
    public const int Fade = 24;
    public const int Creeper = 25;
    public const int Gamma = 26;
    public const int Marble = 27;

    public const int Cherry = 28;
    public const int Explosive = 29;
    public const int Electro = 30;
    public const int RocketDefender = 31;
    public const int Assassin = 32;
    public const int Commando = 33;

    public static ReflectionDictionary<TankID> Collection { get; internal set; } = new(MemberType.Fields);
}
