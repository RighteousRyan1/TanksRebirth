using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Internals.Common.Framework.Collections;

namespace TanksRebirth.GameContent.ID
{
    public sealed class ShellID
    {
        public const int Player = 0;
        public const int Standard = 1;
        public const int Rocket = 2;
        public const int TrailedRocket = 3;
        public const int Supressed = 4;
        public const int Explosive = 5;

        public static ReflectionDictionary<ShellID, int> Collection = new(MemberType.Fields);
    }
}
