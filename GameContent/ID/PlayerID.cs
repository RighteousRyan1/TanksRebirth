using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Internals.Common.Framework.Collections;

namespace TanksRebirth.GameContent.ID
{
    public sealed class PlayerID
    {
        public const int Blue = 0;
        public const int Red = 1;

        public static ReflectionDictionary<PlayerID, int> Collection = new(MemberType.Fields);
    }
}
