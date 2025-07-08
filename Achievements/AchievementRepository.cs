using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.GameContent;
using TanksRebirth.GameContent.UI;

namespace TanksRebirth.Achievements;
/// <summary>A collection of <see cref="IAchievement"/>s which can be used for goals or "achievements" for the player.</summary>
public class AchievementRepository
{
    private List<IAchievement> _achievements = new();

    /// <summary>Add an achievement to this <see cref="AchievementRepository"/>.</summary>
    public void AddAchievement(IAchievement achievement)
        => _achievements.Add(achievement);
    /// <summary>Get the <see cref="IAchievement"/>s in this repository as an <see cref="IList{T}"/>.</summary>
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
    /// <summary>
    /// Update the completion of any <see cref="IAchievement"/>s to this given <see cref="AchievementRepository"/>.
    /// </summary>
    /// <param name="popupHelper">Upon completion of any <see cref="IAchievement"/> 
    /// stored in this <see cref="AchievementRepository"/> (make sure that this <see cref="AchievementPopupHandler"/>
    /// was constructed using this <see cref="AchievementRepository"/>), an attempt at queuing a popup will be created.</param>
    public void UpdateCompletions(AchievementPopupHandler? popupHelper = null) {
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
            if (allOk) {
                achievement.Complete();

                if (popupHelper != null) {
                    if (RuntimeData.RunTime > 1f) {
                        if (TankGame.VanillaAchievementPopupHandler.Repo._achievements.Contains(achievement)) {
                            TankGame.VanillaAchievementPopupHandler.SummonOrQueue(TankGame.VanillaAchievementPopupHandler.Repo._achievements.FindIndex(x => x == achievement));
                        }
                    }
                }
            }
        }
    }
}
