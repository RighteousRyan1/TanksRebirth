using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.Achievements
{
    public class VanillaAchievements
    {
        private static CompletableAchievement[] _achievements = {
            new("Simply a start", "Destroy your first 100 tanks."),
            new("A true warrior", "Destroy 1000 tanks."),
            new("Genocide", "Destroy 10,000 tanks total."),
            new("There are no equals", "Destroy 100,000 tanks total!"),

            new("Child Abuse", "Destroy 100 Brown Tanks."),
            new("Catch them all", "Destroy 100 Ash tanks."),
            new("Enlisted", "Destroy 100 Marine tanks."),
            new("Those weren't submarines...", "Destroy 100 Yellow Tanks."),
            new("Bratatatat", "Destroy 100 Pink Tanks."),
            new("Outsmarting the Calculator", "Destroy 100 Green Tanks."),
            new("Bratatatat 2", "Destroy 100 Purple Tanks."),
            new("Now you see me...", "Destroy 100 White Tanks."),
            new("Conquering the World", "Destroy 100 Black Tanks."),

            new("Will you be mine?", "Kill 50 Tanks with mines."),
            new("Simple Geometry", "Destroy 100 tanks with bullets that have ricocheted at least once."),

            new("Playing with fire", "Encounter a Marine tank, a tank with fast, flame-trailed bullets."),
            new("Double Whammy", "Encounter a Green tank, a tank that can precisely calculate double bounces."),
            new("Camoflague", "Encounter a White tank, a tank which can go invisible and sneak up on you."),
            new("Black Attack!", "Encounter a Black tank, a tank which goes fast, shoots fast, and dodges bullets well."),

            new("Doomed to failure", "Destroy yourself with your own mine."),
            new("Just an accident", "Destroy yourself with your own bullet."),
            new("Beyond Saving", "Destroy yourself with your own bullet... 10 times."),
            new("Soldier in disguise", "Destroy a tank with a bullet that traveled through a teleporter."),

            new("See through the dragon's eyes", "Complete a campaign in third-person mode.")

            // get some new ideas later normie
        };

        public void InitializeToRepository()
        {
            // If an achievement from _achievemnts already exists in the repository, don't add it again.
            if (AchievementRepository.GetAchievements().Intersect(_achievements).Any())
                return;
            
            foreach (var ach in _achievements)
                AchievementRepository.AddAchievement(ach);
        }
    }
}
