using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Data;
using TanksRebirth.Achievements;
using TanksRebirth.Internals.Common.GameUI;

namespace TanksRebirth.GameContent.UI;

public static class AchievementsUI {
    public static List<Achievement> VanillaAchievementsToList = new();

    internal static void GetVanillaAchievementsToList() {
        var vAchievements = VanillaAchievements.Repository.GetAchievements();

        foreach (var ach in vAchievements) {
            if (ach.Requirement is not null)
                VanillaAchievementsToList.Add((ach as Achievement)!);
        }
    }

    public static List<UIImage> AchBtns = new();

    internal static void InitBtns() {
        // find out why positions are in the two-thousands for some reason...
        var curX = 300f;
        var curY = 300f;
        var dims = 64;
        var padding = 16f;
        for (int i = 0; i < VanillaAchievementsToList.Count; i++) {
            var btn = new UIImage(VanillaAchievementsToList[i].Texture ?? Achievement.MysteryTexture, 1f) {
                IsVisible = true,
            };
            btn.SetDimensions(() => new Vector2(curX, curY), () => new Vector2(dims));

            curX += dims + padding;
            curY += dims + padding;

            AchBtns.Add(btn);
        }
    }
}
