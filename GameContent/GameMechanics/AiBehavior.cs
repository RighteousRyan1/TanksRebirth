using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.GameContent.GameMechanics
{
    public class AiBehavior
    {
        public string Label;

        public float Value;

        public bool IsModOf(float remainder)
        {
            if (remainder == 0)
                return false;
            return Value % remainder < RuntimeData.DeltaTime;
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
