using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.Achievements
{
    public class CompletableAchievement : IAchievement
    {
        public bool[] Requirements { get; set; }

        public bool IsAchieved { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public bool _achievedOld;

        public void Achieve()
        {

        }

        public CompletableAchievement(string name, string description, params bool[] requirements)
        {
            Name = name;
            Requirements = requirements;
        }
    }
}
