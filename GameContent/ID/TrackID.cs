using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Internals.Common.Framework.Collections;

namespace TanksRebirth.GameContent.ID
{
    public sealed class TrackID
    {
        public const int Standard = 0;
        public const int Thick = 1;

        public static readonly ReflectionDictionary<ShellID, int> Collection = new(MemberType.Fields);
    }
}
