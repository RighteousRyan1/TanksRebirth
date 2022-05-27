using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.Achievements
{
    public class Achievement : IAchievement
    {
        public bool[] Requirements { get; set; }

        public bool IsComplete { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        /// <summary>Completes this <see cref="Achievement"/></summary>
        public void Complete()
        {
            IsComplete = true;
        }

        public Achievement(string name, string description, params Func<bool>[] requirements)
        {
            Name = name;

            Description = description;
            
            Requirements = new bool[requirements.Length];

            for (int i = 0; i < requirements.Length; i++)
                Requirements[i] = requirements[i].Invoke();
        }
    }
}
