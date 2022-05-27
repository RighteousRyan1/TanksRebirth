using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.Achievements
{
    public interface IAchievement
    {
        void Complete();

        bool[] Requirements { get; set; }
        bool IsComplete { get; set; }
        string Name { get; set; }
    }
}
