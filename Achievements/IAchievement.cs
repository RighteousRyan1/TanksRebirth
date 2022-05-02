using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.Achievements
{
    public interface IAchievement
    {
        void Achieve();
        bool[] Requirements { get; set; }

        bool IsAchieved { get; set; }

        string Name { get; set; }
    }
}
