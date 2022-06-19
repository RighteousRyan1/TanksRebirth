using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.Achievements
{
    public class AchievementRepository
    {
        private List<IAchievement> _achievements = new();

        public void AddAchievement(IAchievement achievement)
            => _achievements.Add(achievement);

        public IList<IAchievement> GetAchievements() 
            => _achievements;

        // not really sure if these work. I hope they do.
        public void Save(BinaryWriter writer)
        {
            for (int i = 0; i < _achievements.Count; i++)
                writer.Write(_achievements[i].IsComplete);
        }

        public void Load(BinaryReader reader)
        {
            for (int i = 0; i < _achievements.Count; i++)
                if (reader.ReadBoolean())
                    _achievements[i].Complete();
        }

        public void UpdateCompletions()
        {
            for (int i = 0; i < _achievements.Count; i++)
            {
                var achievement = _achievements[i];
                if (achievement.Requirements.Length > 0)
                {
                    if (_achievements[i].Requirements.All(req => req.Invoke()))
                    {
                        _achievements[i].Complete();
                    }
                }
            }
        }
    }
}
