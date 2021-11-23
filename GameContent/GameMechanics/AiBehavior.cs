using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiPlayTanksRemake.GameContent.GameMechanics
{
    public class AiBehavior
    {
        public string Label { get; set; }

        public long totalUpdateCount;

        public bool IsBehaviourModuloOf(long remainder)
        {
            return totalUpdateCount % remainder == 0;
        }
    }
    public static class AiBehaviorExtensions
    {
        public static AiBehavior FromName(this AiBehavior[] arr, string name)
        {
            return arr.First(behavior => behavior.Label == name);
        }
    }
}
