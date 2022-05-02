using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.Achievements
{
    public class AchievementRepository
    {
        private static List<IAchievement> _achievements { get; set; } = new();

        public static void AddAchievement(IAchievement achievement)
        {
            _achievements.Add(achievement);
        }

        public static IEnumerable<IAchievement> GetAchievements()
        {
            return _achievements;
        }

        public static void Save()
        {
            
        }
    }
}
