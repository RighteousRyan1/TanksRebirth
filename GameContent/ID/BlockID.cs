using TanksRebirth.Internals.Common.Framework.Collections;

namespace TanksRebirth.GameContent.ID
{
    public class BlockID
    {
        public const int Wood = 0;
        public const int Cork = 1;
        public const int Hole = 2;
        public const int Teleporter = 3;

        public static ReflectionDictionary<BlockID, int> Collection = new(MemberType.Fields);
    }
}
