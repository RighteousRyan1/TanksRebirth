using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Enums;
using TanksRebirth.GameContent;
using TanksRebirth.GameContent.ID;
using TanksRebirth.Internals;

namespace TanksRebirth.Achievements;
/// <summary>Strictly for use of the base game's achievements. This contains an <see cref="AchievementRepository"/>.</summary>
public static class VanillaAchievements
{
    public static AchievementRepository Repository { get; } = new();

    private static Texture2D GetAchTex(string name) => GameResources.GetGameResource<Texture2D>($"Assets/textures/ui/achievement/{name}");

    private static readonly Achievement[] _achievements = {
        #region Tank Genocide
        new("Simply a start", "Destroy your first 100 tanks.", GetAchTex("100tanks"), () => TankGame.SaveFile.TotalKills >= 100),
        new("A true warrior", "Destroy 1000 tanks.", GetAchTex("1000tanks"), () => TankGame.SaveFile.TotalKills >= 1000),
        new("Genocide", "Destroy 10,000 tanks total.", GetAchTex("10000tanks"), () => TankGame.SaveFile.TotalKills >= 10000),
        new("There are no equals", "Destroy 100,000 tanks total!", GetAchTex("100000tanks"), () => TankGame.SaveFile.TotalKills >= 100000),

        new("Child Abuse", "Destroy 100 Brown Tanks.", GetAchTex("100brown"), () => TankGame.SaveFile.TankKills[TankID.Brown] >= 100),
        new("Catch them all", "Destroy 100 Ash tanks.", GetAchTex("100ash"), () => TankGame.SaveFile.TankKills[TankID.Ash] >= 100),
        new("Enlisted", "Destroy 100 Marine tanks.", GetAchTex("100marine"), () => TankGame.SaveFile.TankKills[TankID.Marine] >= 100),
        new("Those weren't submarines...", "Destroy 100 Yellow Tanks.", GetAchTex("100yellow"), () => TankGame.SaveFile.TankKills[TankID.Yellow] >= 100),
        new("Bratatatat", "Destroy 100 Pink Tanks.", GetAchTex("100pink"), () => TankGame.SaveFile.TankKills[TankID.Pink] >= 100),
        new("Bratatatat 2", "Destroy 100 Violet Tanks.", GetAchTex("100violet"), () => TankGame.SaveFile.TankKills[TankID.Violet] >= 100),
        new("Outsmarting the Calculator", "Destroy 100 Green Tanks.", GetAchTex("100green"), () => TankGame.SaveFile.TankKills[TankID.Green] >= 100),
        new("Now you see me...", "Destroy 100 White Tanks.", GetAchTex("100white"), () => TankGame.SaveFile.TankKills[TankID.White] >= 100),
        new("Conquering the World", "Destroy 100 Black Tanks.", GetAchTex("100black"), () => TankGame.SaveFile.TankKills[TankID.Black] >= 100),

        new("Lesser Child Abuse", "Destroy 100 Bronze Tanks", GetAchTex("100bronze"), () => TankGame.SaveFile.TankKills[TankID.Bronze] >= 100),
        new("It's no Use!", "Destroy 100 Silver Tanks", GetAchTex("100silver"), () => TankGame.SaveFile.TankKills[TankID.Silver] >= 100),
        new("Gemmin' it up", "Destroy 100 Sapphire Tanks", GetAchTex("100sapphire"), () => TankGame.SaveFile.TankKills[TankID.Sapphire] >= 100),
        new("The Glint in your Eye", "Destroy 100 Ruby Tanks", GetAchTex("100ruby"), () => TankGame.SaveFile.TankKills[TankID.Ruby] >= 100),
        new("Fast and Furious", "Destroy 100 Citrine Tanks", GetAchTex("100citrine"), () => TankGame.SaveFile.TankKills[TankID.Citrine] >= 100),
        new("Purple Pain", "Destroy 100 Amethyst Tanks", GetAchTex("100amethyst"), () => TankGame.SaveFile.TankKills[TankID.Amethyst] >= 100),
        new("Hrmm...", "Destroy 100 Emerald Tanks", GetAchTex("100emerald"), () => TankGame.SaveFile.TankKills[TankID.Emerald] >= 100),
        new("Without a trace", "Destroy 100 Gold Tanks", GetAchTex("100gold"), () => TankGame.SaveFile.TankKills[TankID.Gold] >= 100),
        new("Tough to break", "Destroy 100 Obsidian Tanks", GetAchTex("100obsidian"), () => TankGame.SaveFile.TankKills[TankID.Obsidian] >= 100),

        new("Can't move? Too bad.", "Kill a Green Tank with a mine.", GetAchTex("greenmine")),
        new("Close and Personal", "Kill a black tank within 50 units of it.", GetAchTex("closeblack")),

        new("Will you be mine?", "Kill 100 Tanks with mines.", GetAchTex("100mine"), () => TankGame.SaveFile.MineKills >= 100),
        new("Simple Geometry", "Destroy 100 tanks with bullets that have ricocheted at least once.", GetAchTex("100rico"), () => TankGame.SaveFile.BounceKills >= 100),
        #endregion
        #region Meetups
        new("Playing with fire", "Encounter a Marine tank, a tank with fast, flame-trailed bullets."),
        new("Double Whammy", "Encounter a Green tank, a tank that can precisely calculate double bounces."),
        new("Camouflage", "Encounter a White tank, a tank which can go invisible and sneak up on you."),
        new("Black Attack!", "Encounter a Black tank, a tank which goes fast, shoots fast, and dodges bullets well."),
        #endregion
        #region Self-Deprecation
        new("Doomed to failure", "Destroy yourself with your own mine."),
        new("Just an accident", "Destroy yourself with your own bullet."),
        new("Beyond Saving", "Destroy yourself... 10 times.", GetAchTex("selfsuicide10"), () => TankGame.SaveFile.Suicides >= 10),
        new("I teleported bullets... For 3 days!", "Destroy a tank with a bullet that traveled through a teleporter."),
        #endregion
        #region Misc
        new("See through the dragon's eyes", "Complete a campaign in third-person mode."),
        new("TANKS a lot!", "Be destroyed by one of your teammates."),
        new("I cannon believe you've done this.", "Destroy one of your teammates."),
        #endregion
        #region Secrets
        new("Nice tunes, bro", "This is a hidden achievement!"), // click main menu logo
        #endregion
        #region Time Played
        new("A Good Start", "Play for your first 10 minutes.", GetAchTex("mins10"),
            () => TankGame.CurrentSessionTimer.Elapsed + TankGame.SaveFile.TimePlayed >= TimeSpan.FromMinutes(10)),
        new("Addicted yet?", "Play for an hour of your life.", GetAchTex("mins60"),
            () => TankGame.CurrentSessionTimer.Elapsed + TankGame.SaveFile.TimePlayed >= TimeSpan.FromHours(1)),
        new("Enjoyment Supreme", "Play for 3 hours.", GetAchTex("mins180"),
            () => TankGame.CurrentSessionTimer.Elapsed + TankGame.SaveFile.TimePlayed >= TimeSpan.FromHours(3)),
        new("Entertainment Value", "Play for 5 hours.", GetAchTex("mins300"),
            () => TankGame.CurrentSessionTimer.Elapsed + TankGame.SaveFile.TimePlayed >= TimeSpan.FromHours(5)),
        new("Please go outside", "Play for 10 hours!", GetAchTex("mins600"),
            () => TankGame.CurrentSessionTimer.Elapsed + TankGame.SaveFile.TimePlayed >= TimeSpan.FromHours(10)),
        new("Grass exists, bro", "Play for 100 hours!", /*GetAchTex("mins6000")*/ null, // to be given a texture!
            () => TankGame.CurrentSessionTimer.Elapsed + TankGame.SaveFile.TimePlayed >= TimeSpan.FromHours(100)),
        #endregion
        #region Creation!
        new("The Power of Creation", "Create a campaign."),
        new("What are these?", "Create a mission."),
        #endregion
        #region Blood, Sweat, and Tears
        new("A winner is you", "Complete the Vanilla campaign."),
        new("Is winning this easy?", "Complete 5 custom-made campaigns."),
        new("All your base are belong to us", "Complete 10 custom-made campaigns."),
        #endregion
        // get some new ideas later normie
    };

    public static void InitializeToRepository()
    {
        // If an achievement from _achievemnts already exists in the repository, don't add it again.
        if (Repository.GetAchievements().Intersect(_achievements).Any())
            return;
        
        foreach (var achievement in _achievements)
            Repository.AddAchievement(achievement);
    }
}
