using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Internals.Common.Framework.Collections;

namespace TanksRebirth.GameContent.ID
{
    // should this be a dictionary for people to easily add to it?
    public sealed class TankID
    {
        // this is going to be a while.
        public const int None = 0;
        public const int Random = 1;
        public const int Brown = 2;
        public const int Ash = 3;
        public const int Marine = 4;
        public const int Yellow = 5;
        public const int Pink = 6;
        public const int Violet = 7;
        public const int Green = 8;
        public const int White = 9;
        public const int Black = 10;

        // here separates the vanilla tanks from the master mod tanks

        public const int Bronze = 11;
        public const int Silver = 12;
        public const int Sapphire = 13;
        public const int Citrine = 14;
        public const int Ruby = 15;
        public const int Amethyst = 16;
        public const int Emerald = 17;
        public const int Gold = 18;
        public const int Obsidian = 19;

        // here separates the master mod tanks from the advanced mod tanks

        public const int Granite = 20;
        public const int Bubblegum = 21;
        public const int Water = 22;
        public const int Tiger = 23;
        public const int Crimson = 24;
        public const int Fade = 25;
        public const int Creeper = 26;
        public const int Gamma = 27;
        public const int Marble = 28;

        public const int Cherry = 29;
        public const int Explosive = 30;
        public const int Electro = 31;
        public const int RocketDefender = 32;
        public const int Assassin = 33;
        public const int Commando = 34;

        public static ReflectionDictionary<TankID, int> Collection = new(MemberType.Fields);
    }
}
