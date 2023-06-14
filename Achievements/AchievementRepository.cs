using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

        public void UpdateCompletions() {
            Span<IAchievement> achievements = CollectionsMarshal.AsSpan(_achievements);

            ref var achievementSearchSpace = ref MemoryMarshal.GetReference(achievements);
            for (int i = 0; i < _achievements.Count; i++) {
                var achievement = Unsafe.Add(ref achievementSearchSpace, i);
                if (achievement.Requirement is null || achievement.IsComplete) continue; // If the achivement has no requiements or is complete continue;
                var allOk = true;

                // hmm... workaround fix...?
                Span<Func<bool>> achievementsReq = new(new[] { achievement.Requirement });

                ref var achievementReqSearchSpace = ref MemoryMarshal.GetReference(achievementsReq);

                for (var j = 0; j < achievementsReq.Length; j++) {
                    var req = Unsafe.Add(ref achievementReqSearchSpace, j);

                    if (req == null) continue;
                    
                    if (!req.Invoke())
                        allOk = false;
                }
                if (allOk)
                    achievement.Complete();
            }
        }
    }
}
